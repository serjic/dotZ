using DotZ.Zones.Utils;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace DotZ.Zones.Inspections
{
  [ElementProblemAnalyzer(new[] { typeof(IUserDeclaredTypeUsage) },
    HighlightingTypes = new[] { typeof(UndefinedAxisError) })]
  public class UndefinedZone : ElementProblemAnalyzer<IUserDeclaredTypeUsage>
  {
    protected override void Run(IUserDeclaredTypeUsage typeUsage, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      var iRequire = typeUsage.TypeName.Reference.Resolve().DeclaredElement as IInterface;
      if (iRequire == null)
        return;

      if (!ZoneConstants.IsIRequire(iRequire)) return;

      foreach (var typeArgument in typeUsage.TypeName.TypeArguments)
      {
        var declaredType = typeArgument as IDeclaredType;
        if (declaredType != null)
        {
          var typeElement = declaredType.GetTypeElement();
          if (typeElement != null)
          {
            if (!ZoneConstants.IsZoneDefinitionClass(typeElement))
            {
              consumer.AddHighlighting(new UndefinedAxisError(typeUsage, typeElement.GetClrName().FullName,
                ZoneConstants.ZoneDefinitionAttributeNameFull), typeUsage.GetContainingFile());
            }
          }
        }
      }
    }
  }
}