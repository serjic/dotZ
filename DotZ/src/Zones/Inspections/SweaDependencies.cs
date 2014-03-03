using System.Collections.Generic;
using DotZ.AttributedClassesCache;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ExternalProcessStorage.SolutionAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Dependencies;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;

namespace DotZ.Zones.Inspections
{
  [SolutionComponent]
  public class SweaDependencies : IFileImageContributor
  {
    private readonly AttributedTypesCache myCache;

    public SweaDependencies(AttributedTypesCache cache)
    {
      myCache = cache;
    }

    public IEnumerable<KeyValuePair<Dependency, Hash>> SolutionDependencies(ISolution solution)
    {
      return EmptyList<KeyValuePair<Dependency, Hash>>.InstanceList;
    }

    public IEnumerable<KeyValuePair<Dependency, Hash>> ModuleDependencies(IPsiModule module)
    {
      return EmptyList<KeyValuePair<Dependency, Hash>>.InstanceList;
    }

    public IEnumerable<KeyValuePair<Dependency, Hash>> FileDependencies(IProjectFile projectFile)
    {
      var node = AttributedTypesTrieCollection.Build(projectFile.ToSourceFile(), myCache.Builder);      
      return node.GetDependencies(myCache);
    }
  }
}