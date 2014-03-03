using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.VB;

namespace DotZ.AttributedClassesCache
{
  [Language(typeof(VBLanguage))]
  public class VBIAttributedTypesCacheBuilder : IAttributedTypesCacheBuilder
  {
    public void BuildFile(IFile file, AttributedTypesTrieCollection trieRoot)
    {
    }

    public IClass FindClass(IFile file, int offset)
    {
      return null;
    }

    public ITypeDeclaration FindDeclaration(IFile file, int offset)
    {
      return null;
    }
  }
}