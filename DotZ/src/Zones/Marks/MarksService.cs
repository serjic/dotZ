using System;
using System.Collections;
using System.Collections.Generic;
using System.Zones;
using DotZ.AttributedClassesCache;
using DotZ.Zones.Utils;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.ExternalProcessStorage.SolutionAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Dependencies;
using JetBrains.Util;

namespace DotZ.Zones.Marks
{
  [SolutionComponent]
  public class MarksService : InvalidatingPsiCache
  {
    private readonly AttributedTypesCache myCache;
    private readonly DependencyStore myDependencyStore;
    private readonly Dictionary<INamespace, IMark> myMarks = new Dictionary<INamespace, IMark>();

    private readonly IList<IClass> myIndexedClasses = new List<IClass>();
    private readonly Dictionary<IClass, int> myClassToIndicies = new Dictionary<IClass, int>();
    public IProperty<bool> Enabled { get; private set; }

    public MarksService(AttributedTypesCache cache, DependencyStore dependencyStore, ISettingsStore settingsStore, Lifetime lifetime)
    {
      myCache = cache;
      myDependencyStore = dependencyStore;
      Enabled = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide).GetValueProperty<bool>(lifetime, settingsStore.Schema.GetScalarEntry((ZoneSettings settings) => settings.CheckZones), null);
    }

    public IMark GetMark(INamespace ns)
    {
      lock (this)
      {
        IMark ret;
        if (myMarks.TryGetValue(ns, out ret))
          return ret;
        ret = CalculateMark(ns);
        myMarks[ns] = ret;
        return ret;
      }
    }

    private IMark CalculateMark(INamespace ns)
    {
      var bits = new BitArray(300);
      foreach (var cls in GetAttributedClasses<ZoneAttribute>(ns, myDependencyStore))
      {
        var index = GetIndex(cls);
        bits.Set(index, true);
      }
      return new Mark(bits);
    }

    private int GetIndex(IClass cls)
    {
      lock (this)
      {
        int ret;
        if (myClassToIndicies.TryGetValue(cls, out ret))
          return ret;
        ret = myIndexedClasses.Count;
        myIndexedClasses.Add(cls);
        myClassToIndicies.Add(cls, ret);
        return ret;
      }
    }

    protected override void InvalidateOnPhysicalChange()
    {
      lock (this)
      {
        myMarks.Clear();
        myIndexedClasses.Clear();
        myClassToIndicies.Clear();
      }
    }

    public IEnumerable<IClass> CollectAnnotatedClasses<TClass>(INamespace ns)
    {
      var trie = myCache.GetTrie<TClass>();
      var names = ns.QualifiedName.Split('.');
      foreach (var node in trie.Explore(names))
      {
        foreach (var annotation in node.Annotations)
        {
          var annotationClass = myCache.FindClass(annotation);
          if (annotationClass != null)
            yield return annotationClass;
        }
      }
    }

    public IEnumerable<IClass> EnumerateClasses(IMark mark)
    {
      foreach (var index in mark.EnumerateIndicies())
      {
        lock (this)
          yield return myIndexedClasses[index];
      }
    }

    public static IEnumerable<IClass> EnumerableMarkClasses([NotNull] IClass annotationClass, bool recursive)
    {
      if (annotationClass == null) 
        throw new ArgumentNullException("annotationClass");

      var ret = new LocalHashSet<IClass>();
      var queue = new Queue<IClass>();
      queue.Enqueue(annotationClass);
      while (!queue.IsEmpty())
      {
        var el = queue.Dequeue();
        foreach (var superType in el.GetSuperTypes())
        {
          var typeElement = superType.GetTypeElement();
          if (typeElement != null)
          {
            if (ZoneConstants.IsIRequire(typeElement as IInterface))
            {
              var substitution = superType.GetSubstitution();
              foreach (var parameter in substitution.Domain)
              {
                var zoneType = substitution[parameter] as IDeclaredType;
                if (zoneType != null)
                {
                  var zoneClass = zoneType.GetTypeElement() as IClass;
                  if (zoneClass != null)
                  {
                    if (ret.Add(zoneClass))
                    {
                      if (recursive)
                        queue.Enqueue(zoneClass);
                    }
                  }
                }
              }
            }
          }
        }
      }
      return ret;
    }

    public IEnumerable<IClass> GetAttributedClasses<TClass>(INamespace ns, DependencyStore dependencyStore)
    {
      var trie = myCache.GetTrie<TClass>();
      if (trie == null)
        yield break;

      var names = ns.QualifiedName.Split('.');

      uint dependency = 0;
      foreach (var name in names)
      {
        // store dependency on every owner namespace
        dependency += (uint)name.GetHashCode();
        dependencyStore.AddDependency(new Dependency(dependency));
      }

      foreach (var node in trie.Explore(names))
      {
        foreach (var annotation in node.Annotations)
        {
          var annotationClass = myCache.FindClass(annotation);
          if (annotationClass != null)
            foreach (var @class in EnumerableMarkClasses(annotationClass, true))
              yield return @class;
        }
      }
    }

  }
}