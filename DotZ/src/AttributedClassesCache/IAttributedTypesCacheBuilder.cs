using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace DotZ.AttributedClassesCache
{
  public interface IAttributedTypesCacheBuilder
  {
    void BuildFile(IFile file, AttributedTypesTrieCollection trieRoot);
    IClass FindClass(IFile file, int offset);
    ITypeDeclaration FindDeclaration(IFile file, int offset);
  }
}