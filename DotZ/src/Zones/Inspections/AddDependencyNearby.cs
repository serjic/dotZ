using System.Collections.Generic;
using System.Linq;
using System.Zones;
using DotZ.Zones.Marks;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Intentions.Extensibility.Menu;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace DotZ.Zones.Inspections
{
  internal class AddDependencyNearby : IQuickFix
  {
    private readonly IncorrectReferenceError myError;

    public AddDependencyNearby(IncorrectReferenceError error)
    {
      myError = error;
    }

    public IEnumerable<IntentionAction> CreateBulbItems()
    {
      var node = myError.Node;
      
      var ownerNamespace = node.GetContainingNode<ICSharpNamespaceDeclaration>();
      if (ownerNamespace == null)
        return null;

      var nsElement = ownerNamespace.DeclaredElement;

      var service = node.GetPsiServices().GetComponent<MarksService>();

      return service.CollectAnnotatedClasses<ZoneAttribute>(nsElement).
        SelectMany(markerClass => new MyAddToExistingZoneMark(markerClass, myError).ToQuickFixAction()).
        Concat(new MyAddNewMark(myError).ToQuickFixAction());
    }

    public bool IsAvailable(IUserDataHolder cache)
    {
      return true;
    }
  }
}