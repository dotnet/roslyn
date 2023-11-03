// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class ControlFlowPass : AbstractFlowPass<ControlFlowPass.LocalState, ControlFlowPass.LocalFunctionState>
    {
        private readonly PooledDictionary<LabelSymbol, BoundNode> _labelsDefined = PooledDictionary<LabelSymbol, BoundNode>.GetInstance();
        private readonly PooledHashSet<LabelSymbol> _labelsUsed = PooledHashSet<LabelSymbol>.GetInstance();
        protected bool _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = false; // By default, just let the original exception to bubble up.

        private readonly ArrayBuilder<(LocalSymbol symbol, BoundBlock block)> _usingDeclarations = ArrayBuilder<(LocalSymbol, BoundBlock)>.GetInstance();
        private BoundBlock _currentBlock = null;

        protected override void Free()
        {
            _labelsDefined.Free();
            _labelsUsed.Free();
            _usingDeclarations.Free();
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

        internal struct LocalState : ILocalState
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

        internal sealed class LocalFunctionState : AbstractLocalFunctionState
        {
            public LocalFunctionState(LocalState unreachableState)
                : base(unreachableState.Clone(), unreachableState.Clone())
            { }
        }

        protected override LocalFunctionState CreateLocalFunctionState(LocalFunctionSymbol symbol) => new LocalFunctionState(UnreachableState());

        protected override bool Meet(ref LocalState self, ref LocalState other)
        {
            var old = self;
            self.Alive &= other.Alive;
            self.Reported &= other.Reported;
            Debug.Assert(!self.Alive || !self.Reported);
            return self.Alive != old.Alive;
        }

        protected override bool Join(ref LocalState self, ref LocalState other)
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

        protected override LocalState TopState()
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
            foreach (var (label, node) in _labelsDefined)
            {
                if (node is BoundSwitchStatement) continue;

                if (!_labelsUsed.Contains(label))
                {
                    Diagnostics.Add(ErrorCode.WRN_UnreferencedLabel, label.GetFirstLocation());
                }
            }

            return result;
        }

        /// <summary>
        /// Perform control flow analysis, reporting all necessary diagnostics.  Returns true if the end of
        /// the body might be reachable...
        /// </summary>
        public static bool Analyze(CSharpCompilation compilation, Symbol member, BoundBlock block, DiagnosticBag diagnostics)
        {
            var walker = new ControlFlowPass(compilation, member, block);

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
                if (pending.Branch is null)
                {
                    continue;
                }

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
                case BoundKind.LocalFunctionStatement:
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
            if (!this.State.Alive &&
                !this.State.Reported &&
                !statement.WasCompilerGenerated &&
                statement.Syntax.Span.Length != 0)
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
            foreach (var branch in PendingBranches.AsEnumerable())
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

        // For purpose of control flow analysis, awaits do not create pending branches, so asynchronous usings and foreachs don't either
        public sealed override bool AwaitUsingAndForeachAddsPendingBranch => false;

        protected override void VisitLabel(BoundLabeledStatement node)
        {
            _labelsDefined[node.Label] = _currentBlock;
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

            // check for illegal jumps across using declarations
            var sourceLocation = node.Syntax.Location;
            var sourceStart = sourceLocation.SourceSpan.Start;
            var targetStart = node.Label.GetFirstLocation().SourceSpan.Start;

            foreach (var usingDecl in _usingDeclarations)
            {
                var usingStart = usingDecl.symbol.GetFirstLocation().SourceSpan.Start;
                if (sourceStart < usingStart && targetStart > usingStart)
                {
                    // No forward jumps
                    Diagnostics.Add(ErrorCode.ERR_GoToForwardJumpOverUsingVar, sourceLocation);
                    break;
                }
                else if (sourceStart > usingStart && targetStart < usingStart)
                {
                    // Backwards jump, so we must have already seen the label, or it must be a switch case label. If it is a switch case label, we know
                    // that either the user received an error for having a using declaration at the top level in a switch statement, or the label is a valid
                    // target to branch to.
                    Debug.Assert(_labelsDefined.ContainsKey(node.Label));

                    // Error if label and using are part of the same block
                    if (_labelsDefined[node.Label] == usingDecl.block)
                    {
                        Diagnostics.Add(ErrorCode.ERR_GoToBackwardJumpOverUsingVar, sourceLocation);
                        break;
                    }
                }
            }

            return base.VisitGotoStatement(node);
        }

        protected override void VisitSwitchSection(BoundSwitchSection node, bool isLastSection)
        {
            base.VisitSwitchSection(node, isLastSection);

            // Check for switch section fall through error
            if (this.State.Alive)
            {
                var syntax = node.SwitchLabels.Last().Syntax;
                Diagnostics.Add(isLastSection ? ErrorCode.ERR_SwitchFallOut : ErrorCode.ERR_SwitchFallThrough,
                                new SourceLocation(syntax), syntax.ToString());
            }
        }

        public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
        {
            foreach (var section in node.SwitchSections)
            {
                foreach (var label in section.SwitchLabels)
                {
                    _labelsDefined[label.Label] = node;
                }
            }

            return base.VisitSwitchStatement(node);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            var parentBlock = _currentBlock;
            _currentBlock = node;
            var initialUsingCount = _usingDeclarations.Count;
            foreach (var local in node.Locals)
            {
                if (local.IsUsing)
                {
                    _usingDeclarations.Add((local, node));
                }
            }

            var result = base.VisitBlock(node);

            _usingDeclarations.Clip(initialUsingCount);
            _currentBlock = parentBlock;
            return result;
        }
    }
}
