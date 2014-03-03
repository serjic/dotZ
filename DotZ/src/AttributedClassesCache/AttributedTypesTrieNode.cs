using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.ExternalProcessStorage.SolutionAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Dependencies;
using JetBrains.Util;

namespace DotZ.AttributedClassesCache
{
  public class AttributedTypesTrieNode
  {
    public struct AttributedTypesTrieData
    {
      public IPsiSourceFile SourceFile;
      public int Offset;
    }

    private Dictionary<string, AttributedTypesTrieNode> myChildren;
    private IList<AttributedTypesTrieData> myAnnotations;

    public IList<AttributedTypesTrieData> Annotations
    {
      get
      {
        return myAnnotations ?? EmptyList<AttributedTypesTrieData>.InstanceList;
      }
    }

    public Dictionary<string, AttributedTypesTrieNode> Children
    {
      get
      {
        return myChildren ?? (myChildren = new Dictionary<string, AttributedTypesTrieNode>());
      }
    }

    public void Dump(TextWriter writer, string indent = "")
    {
      if (myAnnotations != null && myAnnotations.Count > 0)
      {
        writer.Write("[");
        bool first = true;
        foreach (var annotation in myAnnotations)
        {
          if (!first)
            writer.Write(", ");
          writer.Write("{0} at {1}", annotation.SourceFile, annotation.Offset);
          first = false;
        }
        writer.Write("]");
      }
      foreach (var child in Children)
      {
        writer.WriteLine();
        writer.Write(indent);
        writer.Write(child.Key);
        writer.Write(" ");
        child.Value.Dump(writer, indent + "  ");
      }
    }

    public static AttributedTypesTrieNode Read(BinaryReader reader, IPersistentIndexManager persistentIndexManager)
    {
      var root = new AttributedTypesTrieNode();
      if (!root.ReadThis(reader, persistentIndexManager))
        return null;
      return root;
    }

    public bool ReadThis(BinaryReader reader, IPersistentIndexManager persistentIndexManager)
    {
      // optimize on load.
      var hasChildren = false;
      var childrenCount = reader.ReadInt32();
      if (childrenCount == 0)
      {
        myChildren = null;
      }
      else
      {
        myChildren = new Dictionary<string, AttributedTypesTrieNode>(childrenCount);
        for (int i = 0; i < childrenCount; i++)
        {
          var name = reader.ReadString();
          var child = Read(reader, persistentIndexManager);
          if (child != null)
          {
            hasChildren = true;
            myChildren.Add(name, child);
          }
        }
      }
      var annotationsCount = reader.ReadInt32();
      if (annotationsCount == 0)
        return hasChildren;
      myAnnotations = new List<AttributedTypesTrieData>(annotationsCount);
      for (int i = 0; i < annotationsCount; i++)
      {
        var persistentId = reader.ReadInt32();
        long timestamp = reader.ReadInt64();
        var offset = reader.ReadInt32();
        var file = persistentIndexManager.GetSourceFileById(persistentId);

        if (file != null && file.IsValid() && file.LastWriteTimeUtc.Ticks == timestamp)
          myAnnotations.Add(new AttributedTypesTrieData { SourceFile = file, Offset = offset });
      }
      return true;
    }

    public void Write(BinaryWriter writer, IPersistentIndexManager persistentIndexManager)
    {
      if (myChildren != null)
      {
        writer.Write(myChildren.Count);
        foreach (var child in myChildren)
        {
          writer.Write(child.Key);
          child.Value.Write(writer, persistentIndexManager);
        }
      }
      else
      {
        writer.Write(0);
      }

      if (myAnnotations != null)
      {
        writer.Write(myAnnotations.Count);
        foreach (var annotation in myAnnotations)
        {
          writer.Write(persistentIndexManager.GetIdBySourceFile(annotation.SourceFile));
          writer.Write(annotation.SourceFile.LastWriteTimeUtc.Ticks);
          writer.Write(annotation.Offset);
        }
      }
      else
      {
        writer.Write(0);
      }
    }

    public void AddAnnotation(TreeOffset offset, IEnumerable<string> owners, IPsiSourceFile sourceFile)
    {
      var node = this;
      foreach (var owner in owners)
        node = node.AddNamespaceNode(owner);
      node.AddAnnotation(sourceFile, offset);
    }

    private void AddAnnotation(IPsiSourceFile sourceFile, TreeOffset offset)
    {
      if (myAnnotations == null)
        myAnnotations = new List<AttributedTypesTrieData>(1);
      myAnnotations.Add(new AttributedTypesTrieData{SourceFile = sourceFile, Offset = offset.Offset});
    }

    private AttributedTypesTrieNode AddNamespaceNode(string namespaceShortName)
    {
      AttributedTypesTrieNode ret;
      if (Children.TryGetValue(namespaceShortName, out ret))
        return ret;
      ret = new AttributedTypesTrieNode();
      Children.Add(namespaceShortName, ret);
      return ret;
    }

    public void Clear(IPsiSourceFile sourceFile)
    {
      Assertion.Assert(myAnnotations != null, "myAnnotations != null");
      myAnnotations = myAnnotations.Where(x => x.SourceFile != sourceFile).ToList();
      if (myAnnotations.Count == 0)
      {
        myAnnotations = null;
      } 
      // cleanup empty subtries?
    }

    /// <summary>
    /// return true when some data was added
    /// </summary>
    public bool Merge(AttributedTypesTrieNode builtPart, OneToSetMap<IPsiSourceFile, AttributedTypesTrieNode> sourceFileToNodes)
    {
      var ret = false;

      if (builtPart.myAnnotations != null && builtPart.myAnnotations.Count > 0)
      {
        ret = true;
        foreach (var data in builtPart.myAnnotations)
          sourceFileToNodes.Add(data.SourceFile, this);
      }

      if (myAnnotations == null && builtPart.myAnnotations != null)
      {
        myAnnotations = builtPart.myAnnotations;
      }
      else if (myAnnotations != null && builtPart.myAnnotations != null)
      {
        myAnnotations = myAnnotations.Concat(builtPart.myAnnotations).ToList();
      }

      if (builtPart.myChildren != null)
      {
        foreach (var child in builtPart.myChildren)
        {
          AttributedTypesTrieNode ownChild;
          var childTrie = child.Value;
          if (Children.TryGetValue(child.Key, out ownChild))
            ret = ownChild.Merge(childTrie, sourceFileToNodes) || ret;
          else
          {
            Children[child.Key] = childTrie;
            ret = true;
            UpdateIndex(childTrie, sourceFileToNodes);
          }
        }
      }
      return ret;
    }

    private void UpdateIndex(AttributedTypesTrieNode node, OneToSetMap<IPsiSourceFile, AttributedTypesTrieNode> sourceFileToNodes)
    {
      foreach (var annotationData in node.Annotations)
        sourceFileToNodes.Add(annotationData.SourceFile, node);
      foreach (var childNode in node.Children.Values)
        UpdateIndex(childNode, sourceFileToNodes);
    }

    [CanBeNull]
    public AttributedTypesTrieNode GetChild(string name)
    {
      AttributedTypesTrieNode ret;
      if (myChildren == null)
          return null;
      if (myChildren.TryGetValue(name, out ret))
        return ret;
      return null;
    }

    public IEnumerable<KeyValuePair<Dependency, Hash>> GetDependencies(AttributedTypesCache cache)
    {
      var ret = new List<KeyValuePair<Dependency, Hash>>();
      GetDependencies(ret, 0, cache);
      return ret;
    }

    private void GetDependencies(List<KeyValuePair<Dependency, Hash>> ret, uint dependency, AttributedTypesCache cache)
    {
      var hash = 0;
      if (myAnnotations != null)
      {
        foreach (var annotation in myAnnotations)
        {
          var cls = cache.FindDeclaration(annotation);
          Assertion.Assert(cls != null, "cls != null");
          var hashableDeclaration = cls as IHashableDeclaration;
          Assertion.Assert(hashableDeclaration != null, "hashableDeclaration != null");
          var calcHash = hashableDeclaration.CalcHash();
          foreach (var declaration in cls.MemberDeclarations)
          {
            var memberDeclaration = declaration as IHashableDeclaration;
            if (memberDeclaration != null)
              calcHash += memberDeclaration.CalcHash();
          }
          hash += calcHash.GetHashCode();
        }

        if (hash != 0)
          ret.Add(KeyValuePair.Of(new Dependency(dependency), new Hash(hash)));

      }
      if (myChildren != null)
      {
        foreach (var child in myChildren)
          child.Value.GetDependencies(ret, dependency + (uint)child.Key.GetHashCode(), cache);
      }
    }
  }
}