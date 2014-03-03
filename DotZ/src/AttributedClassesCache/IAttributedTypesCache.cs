using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace DotZ.AttributedClassesCache
{
  public interface IAttributedTypesCache
  {
    [CanBeNull]
    AttributedTypesTrieNode GetTrie<TClass>();

    [CanBeNull]
    IClass FindClass(AttributedTypesTrieNode.AttributedTypesTrieData data);

    ITypeDeclaration FindDeclaration(AttributedTypesTrieNode.AttributedTypesTrieData data);

    IEnumerable<ITypeElement> GetAttrubutedTypeElements([NotNull] IClass attributeClass);
  }
}