using DotZ.Zones.Marks;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace DotZ.Zones.Inspections
{
  [ElementProblemAnalyzer(new[] { typeof(IReferenceName) },
    HighlightingTypes = new [] { typeof(IncorrectReferenceError) })]
  public class UsingsCheckerReferenceName : UsingsCheckerBase<IReferenceName>
  {
    public UsingsCheckerReferenceName(MarksService marks) : base(marks)
    {
    }

    protected override ITypeElement Resolve(IReferenceName element)
    {
      return element.Reference.Resolve().DeclaredElement as ITypeElement;
    }
  }
}