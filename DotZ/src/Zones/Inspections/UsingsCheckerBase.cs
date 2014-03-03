using System.Linq;
using DotZ.Zones.Marks;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace DotZ.Zones.Inspections
{
  public class UsingsCheckerBase<T> : ElementProblemAnalyzer<T> where T : ICSharpTreeNode
  {
    private readonly MarksService myMarks;
    private readonly IProperty<bool> myIsEnabled;

    public UsingsCheckerBase(MarksService marks)
    {
      myMarks = marks;
      myIsEnabled = marks.Enabled;
    }

    protected override void Run(T element, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
    {
      if (!myIsEnabled.Value)
        return;

      var ownerNamespace = element.GetContainingNamespaceDeclaration();
      if (ownerNamespace != null)
      {
        var ownerNs = ownerNamespace.DeclaredElement;
        var declaredElement = Resolve(element);
        if (declaredElement != null)
        {
          var referencedNs = declaredElement.GetContainingNamespace();
          var ownerMark = myMarks.GetMark(ownerNs);
          var referencedMark = myMarks.GetMark(referencedNs);
          var notSpecifiedClass = myMarks.EnumerateClasses(referencedMark.Substract(ownerMark)).FirstOrDefault();
          if (notSpecifiedClass != null)
          {
            consumer.AddHighlighting(new IncorrectReferenceError(element, notSpecifiedClass, string.Format("Dependency on {0} is not satisfied in the containing namespace", notSpecifiedClass.ShortName)), element.GetContainingNode<IFile>());
          }
        }
      }
    }

    protected virtual ITypeElement Resolve(T element)
    {
      return null;
    }
  }
}