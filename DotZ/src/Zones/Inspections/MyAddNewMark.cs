using System;
using DotZ.Zones.Utils;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace DotZ.Zones.Inspections
{
  internal class MyAddNewMark : BulbActionBase
  {
    private readonly  IncorrectReferenceError myError;
    private readonly string myShortName;

    public MyAddNewMark(IncorrectReferenceError error)
    {
      myError = error;
      myShortName = error.Cls.ShortName;
    }

    public override string Text
    {
      get { return string.Format("Add dependency on '{0}' to a new marker class", myShortName); }
    }

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
      var node = myError.Node;

      var ownerNamespace = node.GetContainingNode<ICSharpNamespaceDeclaration>();
      if (ownerNamespace == null)
        return null;

      var ownerClassLike = node.GetContainingNode<ICSharpTypeDeclaration>();
      while (ownerClassLike != null)
      {
        var tmp = ownerClassLike.GetContainingNode<ICSharpTypeDeclaration>();
        if (tmp == null)
          break;
        ownerClassLike = tmp;
      }

      if (ownerClassLike != null)
      {
        var services = solution.GetPsiServices();
        var module = node.GetPsiModule();
        var symbolScope = services.Symbols.GetSymbolScope(module, node.GetSourceFile().ResolveContext, true, true);
        var typeElement = symbolScope.GetTypeElementByCLRName(ZoneConstants.ZoneAttributeNameFull);

        var factory = CSharpElementFactory.GetInstance(module);
        var member = (ICSharpTypeDeclaration)factory.CreateTypeMemberDeclaration(
          "[$0]public class ZoneMarker : IRequire<$1> {}",
          (object)typeElement ?? ZoneConstants.ZoneAttributeNameFull,
          myError.Cls
          );

        ownerNamespace.AddTypeDeclarationBefore(member, ownerClassLike);
      }

      return control =>
      {

      };
    }

  }
}