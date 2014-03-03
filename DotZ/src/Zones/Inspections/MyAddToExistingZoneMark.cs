using System;
using System.Linq;
using DotZ.Zones.Utils;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.TextControl;

namespace DotZ.Zones.Inspections
{
  internal class MyAddToExistingZoneMark : BulbActionBase
  {
    private readonly IClass myMarkClass;
    private readonly IncorrectReferenceError myError;
    private readonly string myClrTypeName;

    public MyAddToExistingZoneMark(IClass markClass, IncorrectReferenceError error)
    {
      myMarkClass = markClass;
      myError = error;
      myClrTypeName = markClass.GetClrName().FullName;
    }

    public override string Text
    {
      get { return string.Format("Add requirement of '{0}' to '{1}'", myError.Cls.ShortName, myClrTypeName); }
    }

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
      var ownerClassLike = myMarkClass.GetDeclarations().FirstOrDefault() as IClassDeclaration;
      if (ownerClassLike != null)
      {
        var zoneType = TypeFactory.CreateType(myError.Cls);
        var services = solution.GetPsiServices();
        var symbolScope = services.Symbols.GetSymbolScope(myMarkClass.Module, myMarkClass.ResolveContext, true, true);
        var iRequire = symbolScope.GetTypeElementByCLRName(ZoneConstants.RequireNameFull);
        
        var interfaceType = iRequire == null ? 
          TypeFactory.CreateTypeByCLRName(ZoneConstants.RequireNameFull, myMarkClass.Module, myMarkClass.ResolveContext) : 
          TypeFactory.CreateType(iRequire, new IType[] {zoneType,});

        ownerClassLike.AddSuperInterface(interfaceType, false);
      }

      return control =>
      {
      };
    }

  }
}