// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class AbstractRegionControlFlowPass : ControlFlowPass
    {
        internal AbstractRegionControlFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        public override BoundNode Visit(BoundNode node)
        {
            VisitAlways(node);
            return null;
        }

        // Control flow analysis does not normally scan the body of a lambda, but region analysis does.
        public override BoundNode VisitLambda(BoundLambda node)
        {
            var oldPending = SavePending(); // We do not support branches *into* a lambda.
            LocalState finalState = this.State;
            this.State = TopState();
            var oldPending2 = SavePending();
            VisitAlways(node.Body);
            RestorePending(oldPending2); // process any forward branches within the lambda body
            ImmutableArray<PendingBranch> pendingReturns = RemoveReturns();
            RestorePending(oldPending);
            Join(ref finalState, ref this.State);
            foreach (PendingBranch returnBranch in pendingReturns)
            {
                this.State = returnBranch.State;
                Join(ref finalState, ref this.State);
            }

            this.State = finalState;
            return null;
        }
    }
}
