using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace DotZ.AttributedClassesCache
{
  [Language(typeof(CSharpLanguage))]
  public class CSharpAttributedTypesCacheBuilder : IAttributedTypesCacheBuilder
  {
    public readonly HashSet<string> ForbiddenNames = new HashSet<string>();
    private readonly int myCutLength = "Attribute".Length;

    public CSharpAttributedTypesCacheBuilder()
    {
      AddForbidden<NotNullAttribute>();
      AddForbidden<CanBeNullAttribute>();
      AddForbidden<SerializableAttribute>();
      AddForbidden<CompilerGeneratedAttribute>();
    }

    private void AddForbidden<T>()
    {
      var name = (typeof (T).Name);
      ForbiddenNames.Add(name);
      ForbiddenNames.Add(name.Substring(name.Length - myCutLength));
    }

    public void BuildFile(IFile file, AttributedTypesTrieCollection trieRoot)
    {
      file.ProcessDescendants(new MyProcessor(this, trieRoot));
    }

    public IClass FindClass(IFile file, int offset)
    {
      var declarations = file.FindNodesAt<IClassLikeDeclaration>(new TreeTextRange(new TreeOffset(offset)));
      foreach (var declaration in declarations)
      {
        var ret = declaration.DeclaredElement as IClass;
        if (ret != null)
        {
          return ret;
        }
      }
      return null;
    }

    public ITypeDeclaration FindDeclaration(IFile file, int offset)
    {
      var declarations = file.FindNodesAt<IClassLikeDeclaration>(new TreeTextRange(new TreeOffset(offset)));
      foreach (var declaration in declarations)
        return declaration;
      return null;
    }


  }

  public class MyProcessor : IRecursiveElementProcessor
  {
    private readonly CSharpAttributedTypesCacheBuilder myBuilder;
    private readonly AttributedTypesTrieCollection myTrieRoot;

    public MyProcessor(CSharpAttributedTypesCacheBuilder builder, AttributedTypesTrieCollection trieRoot)
    {
      myBuilder = builder;
      myTrieRoot = trieRoot;
    }

    private bool NeedProcessAttribute(IAttribute attribute)
    {
      return attribute.Name != null && !myBuilder.ForbiddenNames.Contains(attribute.Name.ShortName);
    }


    public bool InteriorShouldBeProcessed(ITreeNode element)
    {
      return !(element is IClassLikeDeclaration);
    }

    public bool ProcessingIsFinished
    {
      get
      {
        return false;
      }
    }

    public void ProcessBeforeInterior(ITreeNode element)
    {
      var classDeclaration = element as IClassDeclaration;
      if (classDeclaration != null)
      {
        foreach (var attribute in classDeclaration.Attributes)
        {
          if (NeedProcessAttribute(attribute))
          {
            var shortName = attribute.Name.ShortName;
            var ownerNamspaceNames = GetOwnerNamesList(classDeclaration).Reverse();
            myTrieRoot.AddAnnotation(shortName, classDeclaration.GetTreeStartOffset(), ownerNamspaceNames, element.GetSourceFile());
          }
        }
      }
    }

    private static IEnumerable<string> GetOwnerNamesList(IClassDeclaration classDeclaration)
    {
      var ownerNamespace = classDeclaration.GetContainingNamespaceDeclaration();
      while (ownerNamespace != null)
      {
        yield return ownerNamespace.ShortName;
        var namespaceQualification = ownerNamespace.NamespaceQualification;
        if (namespaceQualification != null)
        {
          var qualifier = namespaceQualification.Qualifier;
          while (qualifier != null)
          {
            yield return qualifier.ShortName;
            qualifier = qualifier.Qualifier;
          }
        }
        ownerNamespace = ownerNamespace.GetContainingNamespaceDeclaration();
      }
    }

    public void ProcessAfterInterior(ITreeNode element)
    {
    }
  }
}