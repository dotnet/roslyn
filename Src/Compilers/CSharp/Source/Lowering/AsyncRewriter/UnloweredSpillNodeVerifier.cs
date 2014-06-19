using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class UnloweredSpillNodeVerifier
    {
        /// <summary>
        /// BoundSpill* nodes introduced during initial async lowering should be rewritten away during
        /// stack spilling. Spill nodes that are left in the bound tree will cause codgen to fail.
        /// </summary>
        [Conditional("DEBUG")]
        internal static void Verify(BoundNode node)
        {
            new UnloweredSpillNodeVisitor().Visit(node);
        }

        private class UnloweredSpillNodeVisitor : BoundTreeWalker
        {
            public override BoundNode VisitSpillSequence(BoundSpillSequence node)
            {
                throw Contract.Unreachable;
            }

            public override BoundNode VisitSpillBlock(BoundSpillBlock node)
            {
                throw Contract.Unreachable;
            }

            public override BoundNode VisitSpillTemp(BoundSpillTemp node)
            {
                throw Contract.Unreachable;
            }
        }
    }
}