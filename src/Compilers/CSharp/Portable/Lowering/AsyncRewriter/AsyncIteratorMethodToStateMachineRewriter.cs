// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Produces a MoveNext() method for an async-iterator method.
    /// Compared to an async method, this handles rewriting `yield return` (with states decreasing from -3) and
    /// `yield break`, and adds special handling for `try` to allow disposal.
    /// `await` is handled like in async methods (with states 0 and up).
    /// </summary>
    internal sealed class AsyncIteratorMethodToStateMachineRewriter : AsyncMethodToStateMachineRewriter
    {
        private readonly AsyncIteratorInfo _asyncIteratorInfo;

        /// <summary>
        /// Initially, this is the method's return value label (<see cref="AsyncMethodToStateMachineRewriter._exprReturnLabel"/>).
        /// Inside a `try` or `catch` with a `finally`, we'll use the label directly preceding the `finally`.
        /// Inside a `try` or `catch` with an extracted `finally`, we will use the label preceding the extracted `finally`.
        /// Inside a `finally`, we will use the label terminating the `finally` (to avoid restrictions with leave opcode).
        /// </summary>
        private LabelSymbol _enclosingFinallyEntryOrFinallyExitOrExitLabel;
        private ArrayBuilder<LabelSymbol> _previousDisposalLabels;

        /// <summary>
        /// We use _exprReturnLabel for normal end of method (ie. no more values) and `yield break;`.
        /// We use _exprReturnLabelTrue for `yield return;`.
        /// </summary>
        private readonly LabelSymbol _exprReturnLabelTrue;

        /// <summary>
        /// States for `yield return` are decreasing from -3.
        /// </summary>
        private int _nextYieldReturnState = StateMachineStates.InitialAsyncIteratorStateMachine;  // -3

        internal AsyncIteratorMethodToStateMachineRewriter(MethodSymbol method,
            int methodOrdinal,
            AsyncMethodBuilderMemberCollection asyncMethodBuilderMemberCollection,
            AsyncIteratorInfo asyncIteratorInfo,
            SyntheticBoundNodeFactory F,
            FieldSymbol state,
            FieldSymbol builder,
            IReadOnlySet<Symbol> hoistedVariables,
            IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies,
            SynthesizedLocalOrdinalsDispenser synthesizedLocalOrdinals,
            VariableSlotAllocator slotAllocatorOpt,
            int nextFreeHoistedLocalSlot,
            DiagnosticBag diagnostics)
            : base(method, methodOrdinal, asyncMethodBuilderMemberCollection, F,
                  state, builder, hoistedVariables, nonReusableLocalProxies, synthesizedLocalOrdinals,
                  slotAllocatorOpt, nextFreeHoistedLocalSlot, diagnostics)
        {
            Debug.Assert(asyncIteratorInfo != null);

            _asyncIteratorInfo = asyncIteratorInfo;
            _enclosingFinallyEntryOrFinallyExitOrExitLabel = _exprReturnLabel;
            _exprReturnLabelTrue = F.GenerateLabel("yieldReturn");
        }

        protected override BoundStatement GenerateSetResultCall()
        {
            // ... _exprReturnLabel: ...
            // ... this.state = FinishedState; ...

            // if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; } // for enumerables only
            // this.promiseOfValueOrEnd.SetResult(false);
            // return;
            // _exprReturnLabelTrue:
            // this.promiseOfValueOrEnd.SetResult(true);

            // ... _exitLabel: ...
            // ... return; ...

            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            // if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; } // for enumerables only
            AddDisposeCombinedTokensIfNeeded(builder);

            builder.AddRange(
                // this.promiseOfValueOrEnd.SetResult(false);
                generateSetResultOnPromise(false),
                F.Return(),
                F.Label(_exprReturnLabelTrue),
                // this.promiseOfValueOrEnd.SetResult(true);
                generateSetResultOnPromise(true));

            return F.Block(builder.ToImmutableAndFree());

            BoundExpressionStatement generateSetResultOnPromise(bool result)
            {
                // Produce:
                // this.promiseOfValueOrEnd.SetResult(result);
                BoundFieldAccess promiseField = F.InstanceField(_asyncIteratorInfo.PromiseOfValueOrEndField);
                return F.ExpressionStatement(F.Call(promiseField, _asyncIteratorInfo.SetResultMethod, F.Literal(result)));
            }
        }

        private void AddDisposeCombinedTokensIfNeeded(ArrayBuilder<BoundStatement> builder)
        {
            // if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; } // for enumerables only
            if (_asyncIteratorInfo.CombinedTokensField is object)
            {
                var combinedTokens = F.Field(F.This(), _asyncIteratorInfo.CombinedTokensField);
                TypeSymbol combinedTokensType = combinedTokens.Type;

                builder.Add(
                    F.If(F.ObjectNotEqual(combinedTokens, F.Null(combinedTokensType)),
                        thenClause: F.Block(
                            F.ExpressionStatement(F.Call(combinedTokens, F.WellKnownMethod(WellKnownMember.System_Threading_CancellationTokenSource__Dispose))),
                            F.Assignment(combinedTokens, F.Null(combinedTokensType)))));
            }
        }

        protected override BoundStatement GenerateSetExceptionCall(LocalSymbol exceptionLocal)
        {
            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            // if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; } // for enumerables only
            AddDisposeCombinedTokensIfNeeded(builder);

            // _promiseOfValueOrEnd.SetException(ex);
            builder.Add(F.ExpressionStatement(F.Call(
                F.InstanceField(_asyncIteratorInfo.PromiseOfValueOrEndField),
                _asyncIteratorInfo.SetExceptionMethod,
                F.Local(exceptionLocal))));

            return F.Block(builder.ToImmutableAndFree());
        }

        private BoundStatement GenerateJumpToCurrentFinallyOrExit()
        {
            Debug.Assert((object)_enclosingFinallyEntryOrFinallyExitOrExitLabel != null);
            return F.If(
                // if (disposeMode)
                F.InstanceField(_asyncIteratorInfo.DisposeModeField),
                // goto finallyOrExitLabel;
                thenClause: F.Goto(_enclosingFinallyEntryOrFinallyExitOrExitLabel));
        }

        private BoundStatement AppendJumpToCurrentFinallyOrExit(BoundStatement node)
        {
            // Append:
            //  if (disposeMode) goto _enclosingFinallyOrExitLabel;

            return F.Block(
                node,
                GenerateJumpToCurrentFinallyOrExit());
        }

        protected override BoundBinaryOperator ShouldEnterFinallyBlock()
        {
            // We should skip the finally block when:
            // - the state is 0 or greater (we're suspending on an `await`)
            // - the state is -3, -4 or lower (we're suspending on a `yield return`)
            // We don't care about state = -2 (method already completed)

            // So we only want to enter the finally when the state is -1
            return F.IntEqual(F.Local(cachedState), F.Literal(StateMachineStates.NotStartedStateMachine));
        }

        #region Visitors

        /// <summary>
        /// Lower the body, adding an entry state (-3) at the start,
        /// so that we can differentiate an async-iterator that was never moved forward with MoveNextAsync()
        /// from one that is running (-1).
        /// Then we can guard against some bad usages of DisposeAsync.
        /// </summary>
        protected override BoundStatement VisitBody(BoundStatement body)
        {
            // Produce:
            //  initialStateResumeLabel:
            //  if (disposeMode) goto _exprReturnLabel;
            //  this.state = cachedState = -1;
            //  ... rewritten body

            var initialState = _nextYieldReturnState--;
            Debug.Assert(initialState == -3);
            AddState(initialState, out GeneratedLabelSymbol resumeLabel);

            var rewrittenBody = (BoundStatement)Visit(body);

            return F.Block(
                F.Label(resumeLabel), // initialStateResumeLabel:
                GenerateJumpToCurrentFinallyOrExit(), // if (disposeMode) goto _exprReturnLabel;
                GenerateSetBothStates(StateMachineStates.NotStartedStateMachine), // this.state = cachedState = -1;
                rewrittenBody);
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            // Produce:
            //     _current = expression;
            //     _state = <next_state>;
            //     goto _exprReturnLabelTrue;
            //     <next_state_label>: ;
            //     <hidden sequence point>
            //     this.state = cachedState = NotStartedStateMachine;
            //     if (disposeMode) goto _enclosingFinallyOrExitLabel;

            // Note: at label _exprReturnLabelTrue we have:
            //  _promiseOfValueOrEnd.SetResult(true);
            //  return;

            var stateNumber = _nextYieldReturnState--;
            AddState(stateNumber, out GeneratedLabelSymbol resumeLabel);

            var rewrittenExpression = (BoundExpression)Visit(node.Expression);
            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            blockBuilder.Add(
                // _current = expression;
                F.Assignment(F.InstanceField(_asyncIteratorInfo.CurrentField), rewrittenExpression));

            blockBuilder.Add(
                // this.state = cachedState = stateForLabel
                GenerateSetBothStates(stateNumber));

            blockBuilder.Add(
                // goto _exprReturnLabelTrue;
                F.Goto(_exprReturnLabelTrue));

            blockBuilder.Add(
                // <next_state_label>: ;
                F.Label(resumeLabel));

            blockBuilder.Add(F.HiddenSequencePoint());

            blockBuilder.Add(
                // this.state = cachedState = NotStartedStateMachine
                GenerateSetBothStates(StateMachineStates.NotStartedStateMachine));

            blockBuilder.Add(
                // if (disposeMode) goto _enclosingFinallyOrExitLabel;
                GenerateJumpToCurrentFinallyOrExit());

            blockBuilder.Add(
                F.HiddenSequencePoint());

            return F.Block(blockBuilder.ToImmutableAndFree());
        }

        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            Debug.Assert(_asyncIteratorInfo != null);

            // Produce:
            //  disposeMode = true;
            //  goto _enclosingFinallyOrExitLabel;

            return F.Block(
                // disposeMode = true;
                SetDisposeMode(true),
                // goto _enclosingFinallyOrExitLabel;
                F.Goto(_enclosingFinallyEntryOrFinallyExitOrExitLabel));
        }

        private BoundExpressionStatement SetDisposeMode(bool value)
        {
            return F.Assignment(F.InstanceField(_asyncIteratorInfo.DisposeModeField), F.Literal(value));
        }

        /// <summary>
        /// An async-iterator state machine has a flag indicating "dispose mode".
        /// We enter dispose mode by calling DisposeAsync() when the state machine is paused on a `yield return`.
        /// DisposeAsync() will resume execution of the state machine from that state (using existing dispatch mechanism
        /// to restore execution from a given state, without executing other code to get there).
        ///
        /// From there, we don't want normal code flow:
        /// - from `yield return`, we'll jump to the enclosing `finally` (or method exit)
        /// - after finishing a `finally`, we'll jump to the next enclosing `finally` (or method exit)
        ///
        /// Some `finally` clauses may have already been rewritten and extracted to a plain block (<see cref="AsyncExceptionHandlerRewriter"/>).
        /// In those cases, we saved the finally-entry label in <see cref="BoundTryStatement.FinallyLabelOpt"/>.
        /// </summary>
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            _previousDisposalLabels ??= new ArrayBuilder<LabelSymbol>();
            _previousDisposalLabels.Push(_enclosingFinallyEntryOrFinallyExitOrExitLabel);

            var finallyExit = F.GenerateLabel("finallyExit");
            _previousDisposalLabels.Push(finallyExit);

            if (node.FinallyBlockOpt != null)
            {
                var finallyEntry = F.GenerateLabel("finallyEntry");
                _enclosingFinallyEntryOrFinallyExitOrExitLabel = finallyEntry;

                // Add finallyEntry label:
                //  try
                //  {
                //      ...
                //      finallyEntry:

                // Add finallyExit label:
                //  finally
                //  {
                //      ...
                //      finallyExit:
                //  }
                node = node.Update(
                    tryBlock: F.Block(node.TryBlock, F.Label(finallyEntry)),
                    node.CatchBlocks, F.Block(node.FinallyBlockOpt, F.Label(finallyExit)), node.FinallyLabelOpt, node.PreferFaultHandler);
            }
            else if ((object)node.FinallyLabelOpt != null)
            {
                _enclosingFinallyEntryOrFinallyExitOrExitLabel = node.FinallyLabelOpt;
            }

            var result = (BoundStatement)base.VisitTryStatement(node);

            // While inside the try and catch blocks, we'll use the current finallyEntry label for disposal
            // As soon as we close the try and catch blocks (ie. possibly enter the finally block), we'll use the finallyExit label for disposal (restored/popped from the stack by CloseTryCatchBlocks)
            Debug.Assert(_enclosingFinallyEntryOrFinallyExitOrExitLabel == finallyExit);

            // When exiting the try statement, we restore the previous disposal label.
            _enclosingFinallyEntryOrFinallyExitOrExitLabel = _previousDisposalLabels.Pop();

            if (node.FinallyBlockOpt != null)
            {
                // Append:
                //  if (disposeMode) /* jump to parent's finally or exit */
                result = AppendJumpToCurrentFinallyOrExit(result);
            }

            // Note: we add this jump to extracted `finally` blocks as well, using `VisitExtractedFinallyBlock` below

            return result;
        }

        protected override void CloseTryCatchBlocks()
        {
            _enclosingFinallyEntryOrFinallyExitOrExitLabel = _previousDisposalLabels.Pop();
        }

        /// <summary>
        /// Some `finally` clauses may have already been rewritten and extracted to a plain block (<see cref="AsyncExceptionHandlerRewriter"/>).
        /// The extracted block will have been wrapped as a <see cref="BoundExtractedFinallyBlock"/> so that we can process it as a `finally` block here.
        /// </summary>
        public override BoundNode VisitExtractedFinallyBlock(BoundExtractedFinallyBlock extractedFinally)
        {
            // Remove the wrapping and append:
            //  if (disposeMode) goto enclosingFinallyOrExitLabel;

            return AppendJumpToCurrentFinallyOrExit((BoundStatement)VisitBlock(extractedFinally.FinallyBlock));
        }

        #endregion Visitors
    }
}
