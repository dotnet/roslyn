namespace Roslyn.Compilers.CSharp
{
    internal sealed partial class LocalRewriter
    {

        public override BoundNode VisitDelegateCall(BoundDelegateCall node)
        {
            return this.VisitCall(node);
        }

    }
}
