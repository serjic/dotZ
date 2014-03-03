using System.Collections.Generic;
using System.IO;
using JetBrains.ReSharper.ExternalProcessStorage.SolutionAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Dependencies;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace DotZ.AttributedClassesCache
{
  public class AttributedTypesTrieCollection
  {
    public CompactMap<string, AttributedTypesTrieNode> Roots { get; private set; }

    public AttributedTypesTrieCollection()
    {
      Roots = new CompactMap<string, AttributedTypesTrieNode>();
    }

    public static AttributedTypesTrieCollection Build(IPsiSourceFile sourceFile, AttributedTypesCacheBuilder builder)
    {
      var roots = new AttributedTypesTrieCollection();
      foreach (var file in sourceFile.EnumerateDominantPsiFiles())
        builder.BuildFile(file, roots);

      return roots;
    }

    public void AddAnnotation(string attributeName, TreeOffset offset, IEnumerable<string> ownerNamspaceNames, IPsiSourceFile sourceFile)
    {
      if (attributeName.EndsWith("Attribute"))
        attributeName = attributeName.Substring(attributeName.Length - 9);
      var node = EnsureNode(attributeName);
      node.AddAnnotation(offset, ownerNamspaceNames, sourceFile);
    }

    private AttributedTypesTrieNode EnsureNode(string attributeName)
    {
      AttributedTypesTrieNode node;
      if (!Roots.TryGetValue(attributeName, out node))
      {
        node = new AttributedTypesTrieNode();
        Roots.Add(attributeName, node);
      }
      return node;
    }

    public void Write(BinaryWriter writer, IPersistentIndexManager persistentIndexManager)
    {
      writer.Write(Roots.Count);
      foreach (var pair in Roots)
      {
        var name = pair.Key;
        writer.Write(name);
        var trieNode = pair.Value;
        trieNode.Write(writer, persistentIndexManager);
      }
    }
    public void Read(BinaryReader reader, IPersistentIndexManager persistentIndexManager)
    {
      var num = reader.ReadInt32();
      for (int i = 0; i < num; i++)
      {
        var name = reader.ReadString();
        var trie = AttributedTypesTrieNode.Read(reader, persistentIndexManager) ?? new AttributedTypesTrieNode();
        // direct add to roots here... no collisions by name is guaranted
        Roots.Add(name, trie);
      }
    }

    public bool Merge(AttributedTypesTrieCollection part, OneToSetMap<IPsiSourceFile, AttributedTypesTrieNode> sourceFileToNodes)
    {
      bool ret = false;
      foreach (var pair in part.Roots)
      {
        var node = EnsureNode(pair.Key);
        ret = node.Merge(pair.Value, sourceFileToNodes) || ret;
      }      
      return ret;
    }

    public void Dump(StreamWriter writer)
    {
      foreach (var pair in Roots)
      {
        writer.Write("Attribute: {0}", pair.Key);
        pair.Value.Dump(writer);
      }
    }

    public IEnumerable<KeyValuePair<Dependency, Hash>> GetDependencies(AttributedTypesCache cache)
    {
      foreach (var pair in Roots)
        foreach (var dependency in pair.Value.GetDependencies(cache))
          yield return dependency;
    }

  }
}