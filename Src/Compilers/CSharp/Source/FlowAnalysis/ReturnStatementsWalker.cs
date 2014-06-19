using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A region analysis walker that records returns out of the region.  It works by processing the set of pending jumps
    /// at the end of the region, which will contain all return statements within the region.
    /// </summary>
    class ReturnStatementsWalker : AbstractRegionControlFlowAnalysis
    {
        private readonly ArrayBuilder<StatementSyntax> returnStatements = ArrayBuilder<StatementSyntax>.GetInstance();

        internal static IEnumerable<StatementSyntax> Analyze(Compilation compilation, MethodSymbol sourceMethod, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
        {
            var walker = new ReturnStatementsWalker(compilation, sourceMethod, node, firstInRegion, lastInRegion);
            try
            {
                bool badRegion = false;
                walker.Analyze(ref badRegion);
                return badRegion ? Enumerable.Empty<StatementSyntax>() : walker.returnStatements.ToArray();
            }
            finally
            {
                walker.Free();
            }
        }

        protected override void Free()
        {
            returnStatements.Free();
            base.Free();
        }

        new void Analyze(ref bool badRegion)
        {
            Scan(ref badRegion);
        }

        internal ReturnStatementsWalker(Compilation compilation, MethodSymbol sourceMethod, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, sourceMethod, node, firstInRegion, lastInRegion)
        {
        }

        protected override void LeaveRegion()
        {
            foreach (var pending in PendingBranches)
            {
                if (pending.Branch != null && IsReturn(pending.Branch.Syntax) && RegionContains(pending.Branch.Syntax.Span))
                    returnStatements.Add(pending.Branch.Syntax as StatementSyntax);
            }

            base.LeaveRegion();
        }

        private bool IsReturn(SyntaxNode syntax)
        {
            if (syntax == null) return false;
            switch (syntax.Kind)
            {
                case SyntaxKind.ReturnStatement: return true;
                case SyntaxKind.YieldBreakStatement: return true;
                default: return false;
            }
        }
    }
}
