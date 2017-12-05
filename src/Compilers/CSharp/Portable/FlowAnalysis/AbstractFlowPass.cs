// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An abstract flow pass that takes some shortcuts in analyzing finally blocks, in order to enable
    /// the analysis to take place without tracking exceptions or repeating the analysis of a finally block
    /// for each exit from a try statement.  The shortcut results in a slightly less precise
    /// (but still conservative) analysis, but that less precise analysis is all that is required for
    /// the language specification.  The most significant shortcut is that we do not track the state
    /// where exceptions can arise.  That does not affect the soundness for most analyses, but for those
    /// analyses whose soundness would be affected (e.g. "data flows out"), we track "unassignments" to keep
    /// the analysis sound.
    /// </summary>
    internal abstract partial class AbstractFlowPass<TLocalState> : PreciseAbstractFlowPass<TLocalState>
        where TLocalState : PreciseAbstractFlowPass<TLocalState>.AbstractLocalState
    {
        protected readonly bool trackUnassignments; // for the data flows out walker, we track unassignments as well as assignments

        protected AbstractFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            bool trackUnassignments = false)
            : base(compilation, member, node)
        {
            this.trackUnassignments = trackUnassignments;
        }

        protected AbstractFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion,
            bool trackRegions = true,
            bool trackUnassignments = false)
            : base(compilation, member, node, firstInRegion, lastInRegion, trackRegions)
        {
            this.trackUnassignments = trackUnassignments;
        }

        protected abstract void UnionWith(ref TLocalState self, ref TLocalState other);

        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var oldPending = SavePending(); // we do not allow branches into a try statement
            var initialState = this.State.Clone();
            VisitTryBlock(node.TryBlock, node, ref initialState);
            var finallyState = initialState.Clone();
            var endState = this.State;
            foreach (var catchBlock in node.CatchBlocks)
            {
                SetState(initialState.Clone());
                VisitCatchBlock(catchBlock, ref finallyState);
                IntersectWith(ref endState, ref this.State);
            }

            if (node.FinallyBlockOpt != null)
            {
                // branches from the finally block, while illegal, should still not be considered
                // to execute the finally block before occurring.  Also, we do not handle branches
                // *into* the finally block.
                SetState(finallyState);
                var tryAndCatchPending = SavePending();
                var unsetInFinally = AllBitsSet();
                VisitFinallyBlock(node.FinallyBlockOpt, ref unsetInFinally);
                foreach (var pend in tryAndCatchPending.PendingBranches)
                {
                    if (pend.Branch == null) continue; // a tracked exception
                    if (pend.Branch.Kind != BoundKind.YieldReturnStatement)
                    {
                        UnionWith(ref pend.State, ref this.State);
                        if (trackUnassignments) IntersectWith(ref pend.State, ref unsetInFinally);
                    }
                }

                RestorePending(tryAndCatchPending);
                UnionWith(ref endState, ref this.State);
                if (trackUnassignments) IntersectWith(ref endState, ref unsetInFinally);
            }

            SetState(endState);
            RestorePending(oldPending);
            return null;
        }

        protected virtual void VisitTryBlock(BoundStatement tryBlock, BoundTryStatement node, ref TLocalState tryState)
        {
            VisitStatement(tryBlock);
        }

        protected virtual void VisitCatchBlock(BoundCatchBlock catchBlock, ref TLocalState finallyState)
        {
            if (catchBlock.ExceptionSourceOpt != null)
            {
                VisitLvalue(catchBlock.ExceptionSourceOpt);
            }

            if (catchBlock.ExceptionFilterOpt != null)
            {
                VisitCondition(catchBlock.ExceptionFilterOpt);
                SetState(StateWhenTrue);
            }

            VisitStatement(catchBlock.Body);
        }

        protected virtual void VisitFinallyBlock(BoundStatement finallyBlock, ref TLocalState unsetInFinally)
        {
            VisitStatement(finallyBlock); // this should generate no pending branches
        }
    }
}
