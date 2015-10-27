// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class ControlFlowPass : AbstractFlowPass<ControlFlowPass.LocalState>
    {
        private readonly PooledHashSet<LabelSymbol> _labelsDefined = PooledHashSet<LabelSymbol>.GetInstance();
        private readonly PooledHashSet<LabelSymbol> _labelsUsed = PooledHashSet<LabelSymbol>.GetInstance();
        protected bool _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = false; // By default, just let the original exception to bubble up.

        protected override void Free()
        {
            _labelsDefined.Free();
            _labelsUsed.Free();

            base.Free();
        }

        internal ControlFlowPass(CSharpCompilation compilation, Symbol member, BoundNode node)
            : base(compilation, member, node)
        {
        }

        internal ControlFlowPass(CSharpCompilation compilation, Symbol member, BoundNode node, BoundNode firstInRegion, BoundNode lastInRegion)
            : base(compilation, member, node, firstInRegion, lastInRegion)
        {
        }

        internal struct LocalState : AbstractLocalState
        {
            internal bool Alive;
            internal bool Reported; // reported unreachable statement

            internal LocalState(bool live, bool reported)
            {
                this.Alive = live;
                this.Reported = reported;
            }

            /// <summary>
            /// Produce a duplicate of this flow analysis state.
            /// </summary>
            /// <returns></returns>
            public LocalState Clone()
            {
                return this;
            }

            public bool Reachable
            {
                get { return Alive; }
            }
        }

        protected override void UnionWith(ref LocalState self, ref LocalState other)
        {
            self.Alive &= other.Alive;
            self.Reported &= other.Reported;
            Debug.Assert(!self.Alive || !self.Reported);
        }

        protected override bool IntersectWith(ref LocalState self, ref LocalState other)
        {
            var old = self;
            self.Alive |= other.Alive;
            self.Reported &= other.Reported;
            Debug.Assert(!self.Alive || !self.Reported);
            return self.Alive != old.Alive;
        }

        protected override string Dump(LocalState state)
        {
            return "[alive: " + state.Alive + "; reported: " + state.Reported + "]";
        }

        protected override LocalState ReachableState()
        {
            return new LocalState(true, false);
        }

        protected override LocalState UnreachableState()
        {
            return new LocalState(false, this.State.Reported);
        }

        protected override LocalState LabelState(LabelSymbol label)
        {
            LocalState result = base.LabelState(label);
            // We don't want errors reported in one pass to suppress errors in the next pass.
            result.Reported = false;
            return result;
        }

        public override BoundNode Visit(BoundNode node)
        {
            // there is no need to scan the contents of an expression, as expressions
            // do not contribute to reachability analysis (except for constants, which
            // are handled by the caller).
            if (!(node is BoundExpression))
            {
                return base.Visit(node);
            }

            return null;
        }

        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            this.Diagnostics.Clear();  // clear reported diagnostics
            var result = base.Scan(ref badRegion);
            foreach (var label in _labelsDefined)
            {
                if (!_labelsUsed.Contains(label))
                {
                    Diagnostics.Add(ErrorCode.WRN_UnreferencedLabel, label.Locations[0]);
                }
            }

            return result;
        }

        /// <summary>
        /// Perform control flow analysis, reporting all necessary diagnostics.  Returns true if the end of
        /// the body might be reachable..
        /// </summary>
        public static bool Analyze(CSharpCompilation compilation, Symbol member, BoundNode node, DiagnosticBag diagnostics)
        {
            var walker = new ControlFlowPass(compilation, member, node);

            if (diagnostics != null)
            {
                walker._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = true;
            }

            try
            {
                bool badRegion = false;
                var result = walker.Analyze(ref badRegion, diagnostics);
                Debug.Assert(!badRegion);
                return result;
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex) when (diagnostics != null)
            {
                ex.AddAnError(diagnostics);
                return true;
            }
            finally
            {
                walker.Free();
            }
        }

        protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException;
        }

        /// <summary>
        /// Analyze the body, reporting all necessary diagnostics.  Returns true if the end of the
        /// body might be reachable.
        /// </summary>
        /// <returns></returns>
        protected bool Analyze(ref bool badRegion, DiagnosticBag diagnostics)
        {
            ImmutableArray<PendingBranch> returns = Analyze(ref badRegion);

            if (diagnostics != null)
            {
                diagnostics.AddRange(this.Diagnostics);
            }

            // TODO: if in the body of a struct constructor, check that "this" is assigned at each return.
            return State.Alive;
        }

        protected override ImmutableArray<PendingBranch> RemoveReturns()
        {
            var result = base.RemoveReturns();
            foreach (var pending in result)
            {
                switch (pending.Branch.Kind)
                {
                    case BoundKind.GotoStatement:
                        {
                            var leave = pending.Branch;
                            var loc = new SourceLocation(leave.Syntax.GetFirstToken());
                            Diagnostics.Add(ErrorCode.ERR_LabelNotFound, loc, ((BoundGotoStatement)pending.Branch).Label.Name);
                            break;
                        }
                    case BoundKind.BreakStatement:
                    case BoundKind.ContinueStatement:
                        {
                            var leave = pending.Branch;
                            var loc = new SourceLocation(leave.Syntax.GetFirstToken());
                            Diagnostics.Add(ErrorCode.ERR_BadDelegateLeave, loc);
                            break;
                        }
                    case BoundKind.ReturnStatement:
                        break;
                    default:
                        break; // what else could it be?
                }
            }
            return result;
        }

        protected override void VisitStatement(BoundStatement statement)
        {
            switch (statement.Kind)
            {
                case BoundKind.NoOpStatement:
                case BoundKind.Block:
                case BoundKind.ThrowStatement:
                case BoundKind.LabeledStatement:
                    base.VisitStatement(statement);
                    break;
                case BoundKind.StatementList:
                    base.VisitStatementList((BoundStatementList)statement);
                    break;
                default:
                    CheckReachable(statement);
                    base.VisitStatement(statement);
                    break;
            }
        }

        private void CheckReachable(BoundStatement statement)
        {
            if (!this.State.Alive && !this.State.Reported && !statement.WasCompilerGenerated && statement.Syntax.Span.Length != 0)
            {
                var firstToken = statement.Syntax.GetFirstToken();
                Diagnostics.Add(ErrorCode.WRN_UnreachableCode, new SourceLocation(firstToken));
                this.State.Reported = true;
            }
        }

        protected override void VisitTryBlock(BoundStatement tryBlock, BoundTryStatement node, ref LocalState tryState)
        {
            if (node.CatchBlocks.IsEmpty)
            {
                base.VisitTryBlock(tryBlock, node, ref tryState);
                return;
            }

            var oldPending = SavePending(); // we do not support branches into a try block
            base.VisitTryBlock(tryBlock, node, ref tryState);
            RestorePending(oldPending);
        }

        protected override void VisitCatchBlock(BoundCatchBlock catchBlock, ref LocalState finallyState)
        {
            var oldPending = SavePending(); // we do not support branches into a catch block
            base.VisitCatchBlock(catchBlock, ref finallyState);
            RestorePending(oldPending);
        }

        protected override void VisitFinallyBlock(BoundStatement finallyBlock, ref LocalState endState)
        {
            var oldPending1 = SavePending(); // we do not support branches into a finally block
            var oldPending2 = SavePending(); // track only the branches out of the finally block
            base.VisitFinallyBlock(finallyBlock, ref endState);
            RestorePending(oldPending2); // resolve branches that remain within the finally block
            foreach (var branch in PendingBranches)
            {
                if (branch.Branch == null) continue; // a tracked exception
                var location = new SourceLocation(branch.Branch.Syntax.GetFirstToken());
                switch (branch.Branch.Kind)
                {
                    case BoundKind.YieldBreakStatement:
                    case BoundKind.YieldReturnStatement:
                        // ERR_BadYieldInFinally reported during initial binding
                        break;
                    default:
                        Diagnostics.Add(ErrorCode.ERR_BadFinallyLeave, location);
                        break;
                }
            }

            RestorePending(oldPending1);
        }


        protected override void VisitLabel(BoundLabeledStatement node)
        {
            _labelsDefined.Add(node.Label);
            base.VisitLabel(node);
        }

        public override BoundNode VisitLabeledStatement(BoundLabeledStatement node)
        {
            VisitLabel(node);
            CheckReachable(node);
            VisitStatement(node.Body);
            return null;
        }

        public override BoundNode VisitGotoStatement(BoundGotoStatement node)
        {
            _labelsUsed.Add(node.Label);
            return base.VisitGotoStatement(node);
        }

        protected override void VisitSwitchSectionLabel(LabelSymbol label, BoundSwitchSection node)
        {
            _labelsDefined.Add(label);
            base.VisitSwitchSectionLabel(label, node);

            // switch statement labels are always considered to be referenced
            _labelsUsed.Add(label);
        }

        public override BoundNode VisitSwitchSection(BoundSwitchSection node, bool lastSection)
        {
            base.VisitSwitchSection(node);

            // Check for switch section fall through error
            if (this.State.Alive)
            {
                Debug.Assert(node.SwitchLabels.Any());

                var boundLabel = node.SwitchLabels.Last();
                Diagnostics.Add(lastSection ? ErrorCode.ERR_SwitchFallOut : ErrorCode.ERR_SwitchFallThrough,
                                new SourceLocation(boundLabel.Syntax), boundLabel.Label.Name);
                this.State.Reported = true;
            }

            return null;
        }
    }
}
