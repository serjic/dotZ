using System;
using System.Collections.Generic;
using System.Diagnostics;
using DotZ.AttributedClassesCache;
using DotZ.Zones.Marks;
using DotZ.Zones.Utils;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.ContextActions;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Intentions.Extensibility.Menu;
using JetBrains.ReSharper.Psi.Services;
using JetBrains.TextControl;
using JetBrains.Util;

namespace DotZ.Zones.Actions
{
  [ContextAction(Group = CommonContextActions.GroupID, Name = ShowMarkedNamespacesCache, Description = "Show marked namespaces cache", Priority = 1)]
  public class DumpZonesCacheAction : BulbActionBase, IContextAction
  {

    private const string ShowMarkedNamespacesCache = "Show Zones Cache";
    private readonly IContextActionDataProvider myProvider;

    public DumpZonesCacheAction(ICSharpContextActionDataProvider provider)
    {
      myProvider = provider;
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
      if (!myProvider.Solution.GetComponent<MarksService>().Enabled.Value)
        return false;
      bool x;
      var declaredElements = TextControlToPsi.GetDeclaredElements(myProvider.Solution, myProvider.Document, myProvider.CaretOffset, out x);

      foreach (var declaredElement in declaredElements)
      {
        if (declaredElement.ShortName == ZoneConstants.ZoneAttributeName)
        {
          return true;
        }
      }
      return false;
    }

    public override string Text
    {
      get { return ShowMarkedNamespacesCache; }
    }

    protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
      return control =>
      {
        var file = FileSystemDefinition.CreateTemporaryFile(extensionWithDot:".txt");
        file.WriteTextStream(writer =>
        {
          solution.GetComponent<AttributedTypesCache>().Dump(writer);
        });
        Process.Start(file.FullPath);
      };
    }

    public IEnumerable<IntentionAction> CreateBulbItems()
    {
      return this.ToContextAction();
    }
  }
}