using DotZ.Zones.Marks;
using JetBrains.ReSharper.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace DotZ.Zones.Inspections
{
  [ElementProblemAnalyzer(new[] { typeof(IReferenceExpression) },
    HighlightingTypes = new [] { typeof(IncorrectReferenceError) })]
  public class UsingsCheckerReferenceExpression : UsingsCheckerBase<IReferenceExpression>
  {
    public UsingsCheckerReferenceExpression(MarksService marks) : base(marks)
    {
    }

    protected override ITypeElement Resolve(IReferenceExpression element)
    {
      return element.Reference.Resolve().DeclaredElement as ITypeElement;
    }
  }
}