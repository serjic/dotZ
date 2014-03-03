using System.Collections.Generic;
using System.Linq;
using DotZ.Zones.Marks;
using DotZ.Zones.Utils;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace DotZ.Zones.Inspections
{
  [ElementProblemAnalyzer(new[] { typeof(IClassDeclaration) },
    HighlightingTypes = new[] { typeof(RedundantDependencySpecificationError) })]
  public class RedundantDependencyChecker : ElementProblemAnalyzer<IClassDeclaration>
  {
    private readonly MarksService myMarks;
    private readonly IPsiServices myServices;
    private readonly IProperty<bool> myIsEnabled;

    public RedundantDependencyChecker(MarksService marks, IPsiServices services)
    {
      myMarks = marks;
      myServices = services;
      myIsEnabled = services.Solution.GetComponent<MarksService>().Enabled;
    }

    protected override void Run(IClassDeclaration classDeclaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      if (!myIsEnabled.Value)
        return;

      var typeElement = classDeclaration.DeclaredElement;
      if (typeElement == null || !ZoneConstants.IsZoneClass(typeElement))
        return;

      var mark = myMarks.GetMark(typeElement.GetContainingNamespace());
      var classes = myMarks.EnumerateClasses(mark).ToList();
      
      // find all base zones
      var baseZones = new HashSet<IClass>();
      foreach (var cls in classes)
        foreach (var baseZone in MarksService.EnumerableMarkClasses(cls, true))
          if (!baseZone.Equals(cls))
            baseZones.Add(baseZone);

      foreach (var typeUsage in classDeclaration.SuperTypeUsageNodes.OfType<IUserDeclaredTypeUsage>())
      {
        IDeclaredType superType = CSharpTypeFactory.CreateDeclaredType(typeUsage);
        var superTypeElement = superType.GetTypeElement();
        if (superTypeElement != null)
        {
          if (ZoneConstants.IsIRequire(superTypeElement as IInterface))
          {
            var substitution = superType.GetSubstitution();
            foreach (var parameter in substitution.Domain)
            {
              var zoneType = substitution[parameter] as IDeclaredType;
              if (zoneType != null)
              {
                var zoneClass = zoneType.GetTypeElement() as IClass;
                if (zoneClass != null && baseZones.Contains(zoneClass))
                {
                  consumer.AddHighlighting(new RedundantDependencySpecificationError(typeUsage), classDeclaration.GetContainingNode<IFile>());
                }
              }
            }
          }
        }

      }
    }
  }
}