namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A pending branch.  There are created for a return, break, continue, or goto statement.  The
    /// idea is that we don't know if the branch will eventually reach its destination because of an
    /// intervening finally block that cannot complete normally.  So we store them up and handle them
    /// as we complete processing each construct.  At the end of a block, if there are any pending
    /// branches to a label in that block we process the branch.  Otherwise we relay it up to the
    /// enclosing construct as a pending branch of the enclosing construct.
    /// </summary>
    internal class PendingBranch
    {
        public readonly BoundStatement Branch;
        public FlowAnalysisLocalState State;

        public PendingBranch(BoundStatement branch, FlowAnalysisLocalState state)
        {
            this.Branch = branch;
            this.State = state;
        }
    }
}