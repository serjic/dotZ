using DotZ.Zones.Marks;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace DotZ.Zones.Inspections
{
  [ElementProblemAnalyzer(new[] { typeof(ICSharpNamespaceDeclaration) },
    HighlightingTypes = new[] { typeof(IncorrectNamespaceNestingError) })]
  public class NamespaceNestingChecker : ElementProblemAnalyzer<ICSharpNamespaceDeclaration>
  {
    private readonly MarksService myMarks;
    private readonly IPsiServices myServices;
    private readonly IProperty<bool> myIsEnabled;

    public NamespaceNestingChecker(MarksService marks, IPsiServices services)
    {
      myMarks = marks;
      myServices = services;
      myIsEnabled = services.Solution.GetComponent<MarksService>().Enabled;
    }

    protected override void Run(ICSharpNamespaceDeclaration declaration, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      if (!myIsEnabled.Value)
       return;
/*
      var ownerProject = declaration.GetProject();
      if (ownerProject == null)
        return;
      var buildSettings = ownerProject.ProjectProperties.BuildSettings as IManagedProjectBuildSettings;
      if (buildSettings == null)
        return;
      var defaultNamespace = buildSettings.DefaultNamespace;
      var ns = declaration.DeclaredElement;
      if (ns == null)
        return;
      var nsName = ns.QualifiedName;
      if (!nsName.StartsWith(defaultNamespace) || nsName.Length > defaultNamespace.Length && nsName[defaultNamespace.Length] != '.')
      {
        consumer.AddHighlighting(new IncorrectNamespaceNestingError(declaration,declaration. NamespaceKeyword, defaultNamespace), declaration.GetContainingFile());
      }
      // once ns is OK - check containing folders for ZoneMarkers...
      var projectFile = declaration.GetSourceFile().ToProjectFile();
      if (projectFile == null)
        return;
      var folder = projectFile.ParentFolder;
      while (folder != null)
      {
        if (folder.GetSubItems().OfType<IProjectFile>().Any(x => x.Name == "ZoneMarker.cs"))
        {
          defaultNamespace = folder.CalculateExpectedNamespace(CSharpLanguage.Instance);
          if (!nsName.StartsWith(defaultNamespace) || nsName.Length > defaultNamespace.Length && nsName[defaultNamespace.Length] != '.')
          {
            consumer.AddHighlighting(new IncorrectNamespaceNestingError(declaration, declaration.NamespaceKeyword, defaultNamespace), declaration.GetContainingFile());
          }
        }
        folder = folder.ParentFolder;
      }
 */
    }
  }
}