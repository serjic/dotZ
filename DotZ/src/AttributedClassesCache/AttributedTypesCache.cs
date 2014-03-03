using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.DataFlow;
using JetBrains.DocumentManagers.impl;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Dependencies;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.Psi.VB;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.Logging;

namespace DotZ.AttributedClassesCache
{
  [PsiComponent]
  public class AttributedTypesCache : ICache, IAttributedTypesCache
  {
    private readonly OneToSetMap<IPsiSourceFile, AttributedTypesTrieNode> mySourceFileToNodes = new OneToSetMap<IPsiSourceFile, AttributedTypesTrieNode>();
    private AttributedTypesTrieCollection myRoot = new AttributedTypesTrieCollection();

    private readonly JetHashSet<IPsiSourceFile> myDirtyFiles = new JetHashSet<IPsiSourceFile>();
    private JetHashSet<IPsiSourceFile> myFilesWithoutAnnotations = new JetHashSet<IPsiSourceFile>();

    private readonly IShellLocks myShellLocks;
    private readonly IPersistentIndexManager myPersistentIndexManager;
    public AttributedTypesCacheBuilder Builder { get; private set; }

    private readonly FileSystemPath myCacheFile;
    private readonly IShellLocks myLocks;
    private readonly IPsiFiles myPsiFiles;
    private const string IMAGE_FILE = "MarkedFoldersCache3.bin";

    public AttributedTypesCache(Lifetime lifetime, IDependencyStore dependencyStore, IShellLocks shellLocks, IPsiConfiguration psiConfiguration, IPersistentIndexManager persistentIndexManager, CommonIdentifierIntern identifierIntern, AttributedTypesCacheBuilder builder, ISolution solution, IShellLocks locks, IPsiFiles psiFiles)
    {
      myShellLocks = shellLocks;
      myPersistentIndexManager = persistentIndexManager;
      Builder = builder;
      myLocks = locks;
      myPsiFiles = psiFiles;

      var solutionCacheFolder = psiConfiguration.SolutionCachesConfiguration.GetSolutionCacheFolder(solution);
      myCacheFile = solutionCacheFolder.Combine(IMAGE_FILE);

    }

    private static bool Accepts(IPsiSourceFile sourceFile)
    {
      foreach (var language in sourceFile.GetDominantLanguages())
      {
        if (language.Is<CSharpLanguage>())
          return true;
        if (language.Is<VBLanguage>())
          return true;
      }
      return false;
    }

    public object Load(IProgressIndicator progress, bool enablePersistence)
    {
      if (!enablePersistence)
        return null;

      Stream stream;
      FileSystemPath path = myCacheFile;
      try
      {
        stream = path.ExistsFile ? path.OpenFileForReadingExclusive() : null; // Exists check: avoid unnecessary exceptions
      }
      catch (IOException)
      {
        stream = null;
      }
      catch (UnauthorizedAccessException)
      {
        stream = null;
      }

      if (stream == null)
        return null;

      try
      {
        using (var reader = new BinaryReader(stream))
          return LoadTrie(reader, progress);
      }
      catch (SerializationError e)
      {
        Logger.LogExceptionSilently(e);
      }
      catch (IOException e)
      {
        LogIoException(e);
      }
      catch (FormatException e)
      {
        LogIoException(e);
      }

      return null;
    }

    private void SaveSourceCache(BinaryWriter writer)
    {
      writer.Write(myFilesWithoutAnnotations.Count);
      foreach (var file in myFilesWithoutAnnotations)
      {
        int id = myPersistentIndexManager.GetIdBySourceFile(file);
        writer.Write(id);
        writer.Write(file.LastWriteTimeUtc.Ticks);
      }

      myRoot.Write(writer, myPersistentIndexManager);

    }

    private Pair<AttributedTypesTrieCollection, JetHashSet<IPsiSourceFile>> LoadTrie(BinaryReader reader, IProgressIndicator progress)
    {
      var otherFilesCount = reader.ReadInt32();
      var otherFiles = new JetHashSet<IPsiSourceFile>();
      for (int i = 0; i < otherFilesCount; i++)
      {
        int persistentId = reader.ReadInt32();
        var timestamp = reader.ReadInt64();
        var file = myPersistentIndexManager.GetSourceFileById(persistentId);
        if (file != null && file.IsValid() && file.LastWriteTimeUtc.Ticks == timestamp)
          otherFiles.Add(file);
      }

      var map = new AttributedTypesTrieCollection();
      map.Read(reader, myPersistentIndexManager);
      return Pair.Of(map, otherFiles);
    }


    public void MergeLoaded(object data)
    {
      var node = (Pair<AttributedTypesTrieCollection, JetHashSet<IPsiSourceFile>>)data;
      if (node.First == null || node.Second == null)
        return;

      myLocks.AssertWriteAccessAllowed();

      //optimization here: replace roots because it constains no data yet.
      myRoot = node.First;
      myFilesWithoutAnnotations = node.Second;
      
      // add new nodes to file -> node index. 
      UpdateIndexAfterLoad(myRoot.Roots);

      myPsiFiles.PsiChanged(null, PsiChangedElementType.ContentsChanged);
    }

    /// <summary>
    /// Returns of some data was added to the index
    /// </summary>
    private void UpdateIndexAfterLoad(CompactMap<string, AttributedTypesTrieNode> node)
    {
      foreach (var pair in node)
      {
        var trieNode = pair.Value;

        UpdateIndexAfterLoad(trieNode);
      }
    }

    private void UpdateIndexAfterLoad(AttributedTypesTrieNode trieNode)
    {
      foreach (var annotationData in trieNode.Annotations)
        mySourceFileToNodes.Add(annotationData.SourceFile, trieNode);

      foreach (var childNode in trieNode.Children.Values)
        UpdateIndexAfterLoad(childNode);
    }

    public void Save(IProgressIndicator progress, bool enablePersistence)
    {
      if (!enablePersistence)
        return;

      try
      {
        myCacheFile.Directory.CreateDirectory();

        //
        var stream = myCacheFile.OpenFileForWritingExclusive();
        using (var writer = new BinaryWriter(stream))
          SaveSourceCache(writer);

      }
      catch (IOException e)
      {
        LogIoException(e);
      }
      catch (UnauthorizedAccessException e)
      {
        LogIoException(e);
      }
    }

    private static void LogIoException(Exception e)
    {
      Logger.LogExceptionSilently(e);
    }


    public void MarkAsDirty(IPsiSourceFile sourceFile)
    { 
      if (Accepts(sourceFile))
        myDirtyFiles.Add(sourceFile);
    }

    public bool HasDirtyFiles
    {
      get { return !myDirtyFiles.IsEmpty(); }
    }

    public bool UpToDate(IPsiSourceFile sourceFile)
    {
      myShellLocks.AssertReadAccessAllowed();
      if (!Accepts(sourceFile))
        return true;
      return !myDirtyFiles.Contains(sourceFile) && (myFilesWithoutAnnotations.Contains(sourceFile) || mySourceFileToNodes.ContainsKey(sourceFile));
    }

    public object Build(IPsiSourceFile sourceFile, bool isStartup)
    {
      return Build(sourceFile);
    }

    private AttributedTypesTrieCollection Build(IPsiSourceFile sourceFile)
    {
      return AttributedTypesTrieCollection.Build(sourceFile, Builder);
    }

    public void Merge(IPsiSourceFile sourceFile, object builtPart)
    {
      myShellLocks.AssertWriteAccessAllowed();

      // remove old annotations from all tries
      foreach (var node in mySourceFileToNodes[sourceFile])
        node.Clear(sourceFile);

      mySourceFileToNodes.RemoveKey(sourceFile);
      myFilesWithoutAnnotations.Remove(sourceFile);

      // add built annotations.
      var part = (AttributedTypesTrieCollection) builtPart;

      if (!myRoot.Merge(part, mySourceFileToNodes))
        myFilesWithoutAnnotations.Add(sourceFile);

      myDirtyFiles.Remove(sourceFile);
    }

    public void Drop(IPsiSourceFile sourceFile)
    {
      myShellLocks.AssertWriteAccessAllowed();

      // remove old annotations from the trie...
      foreach (var node in mySourceFileToNodes[sourceFile])
        node.Clear(sourceFile);

      myFilesWithoutAnnotations.Remove(sourceFile);
      mySourceFileToNodes.RemoveKey(sourceFile);
    }

    public void OnPsiChange(ITreeNode elementContainingChanges, PsiChangedElementType type)
    {
      if (elementContainingChanges == null)
        return;
      myShellLocks.AssertWriteAccessAllowed();
      var projectFile = elementContainingChanges.GetSourceFile();
      if (projectFile != null && Accepts(projectFile))
        myDirtyFiles.Add(projectFile);
    }

    public void OnDocumentChange(IPsiSourceFile sourceFile, ProjectFileDocumentCopyChange args)
    {
      myShellLocks.AssertWriteAccessAllowed();
      if (Accepts(sourceFile))
        myDirtyFiles.Add(sourceFile);
    }

    public void SyncUpdate(bool underTransaction)
    {
      myShellLocks.AssertReadAccessAllowed();

      if (myDirtyFiles.Count > 0)
      {
        foreach (var sourceFile in new List<IPsiSourceFile>(myDirtyFiles))
        {
          if (sourceFile.IsValid())
            using (WriteLockCookie.Create())
            {
              var ret = Build(sourceFile);
              ((ICache) this).Merge(sourceFile, ret);
            }
          else
            myDirtyFiles.Remove(sourceFile);
        }
      }
    }

    public void Dump(StreamWriter writer)
    {
      myRoot.Dump(writer);
    }

    public IEnumerable<ITypeElement> GetAttrubutedTypeElements([NotNull] IClass attributeClass)
    {
      var clrTypeName = attributeClass.GetClrName();
      if (attributeClass == null) throw new ArgumentNullException("attributeClass");
      var trie = GetTrie(attributeClass.ShortName);
      if (trie == null)
        yield break;
      foreach (var node in trie.Enumerate())
      {
        foreach (var data in node.Annotations)
        {
          var declaration = FindDeclaration(data);
          if (declaration != null)
          {
            var declaredElement = declaration.DeclaredElement;
            if (declaredElement != null)
            {
              if (declaredElement.HasAttributeInstance(clrTypeName, false))
                yield return declaredElement;
            }
          }
        }
      }
    }


    [CanBeNull]
    public AttributedTypesTrieNode GetTrie<TClass>()
    {
      var name = (typeof(TClass)).Name;
      return GetTrie(name);
    }

    [CanBeNull]
    private AttributedTypesTrieNode GetTrie(string name)
    {
      if (name.EndsWith("Attribute"))
        name = name.Substring(0, name.Length - 9);
      AttributedTypesTrieNode ret;
      myRoot.Roots.TryGetValue(name, out ret);
      return ret;
    }

    [CanBeNull]
    public IClass FindClass(AttributedTypesTrieNode.AttributedTypesTrieData data)
    {
      foreach (var file in data.SourceFile.EnumerateDominantPsiFiles())
      {
        var ret = Builder.FindClass(file, data.Offset);
        if (ret != null)
          return ret;
      }
      return null;
    }

    public ITypeDeclaration FindDeclaration(AttributedTypesTrieNode.AttributedTypesTrieData data)
    {
      foreach (var file in data.SourceFile.EnumerateDominantPsiFiles())
      {
        var ret = Builder.FindDeclaration(file, data.Offset);
        if (ret != null)
          return ret;
      }
      return null;
    }
  }
}