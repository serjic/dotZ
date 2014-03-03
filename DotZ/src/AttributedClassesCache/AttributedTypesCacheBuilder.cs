using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace DotZ.AttributedClassesCache
{
  [PsiComponent]
  public class AttributedTypesCacheBuilder
  {
    private readonly LanguageManager myLanguages;

    public AttributedTypesCacheBuilder(LanguageManager languages)
    {
      myLanguages = languages;
    }

    public void BuildFile(IFile file, AttributedTypesTrieCollection collection)
    {
      file.GetPsiServices();
      var service = myLanguages.TryGetService<IAttributedTypesCacheBuilder>(file.Language);
      if (service != null)
        service.BuildFile(file, collection);
    }

    public IClass FindClass(IFile file, int offset)
    {
      file.GetPsiServices();
      var service = myLanguages.TryGetService<IAttributedTypesCacheBuilder>(file.Language);
      if (service != null)
        return service.FindClass(file, offset);
      return null;
    }

    public ITypeDeclaration FindDeclaration(IFile file, int offset)
    {
      file.GetPsiServices();
      var service = myLanguages.TryGetService<IAttributedTypesCacheBuilder>(file.Language);
      if (service != null)
        return service.FindDeclaration(file, offset);
      return null;
    }
  }
}