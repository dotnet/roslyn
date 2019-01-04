// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// Compared to an async method, this handles rewriting `yield return` and
    /// `yield break`, and adds special handling for `try` to allow disposal.
    /// </summary>
    internal sealed class AsyncIteratorMethodToStateMachineRewriter : AsyncMethodToStateMachineRewriter
    {
        private readonly AsyncIteratorInfo _asyncIteratorInfo;

        /// <summary>
        /// Initially, this is the method's return value label (<see cref="AsyncMethodToStateMachineRewriter._exprReturnLabel"/>).
        /// When we enter a `try` that has a `finally`, we'll use the label directly preceding the `finally`.
        /// When we enter a `try` that has an extracted `finally`, we will use the label preceding the extracted `finally`.
        /// </summary>
        private LabelSymbol _enclosingFinallyOrExitLabel;

        /// <summary>
        /// We use _exprReturnLabel for normal end of method (ie. no more values).
        /// We use _exprReturnLabelTrue for `yield return;`.
        /// </summary>
        private readonly LabelSymbol _exprReturnLabelTrue;

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
            _enclosingFinallyOrExitLabel = _exprReturnLabel;
            _exprReturnLabelTrue = F.GenerateLabel("yieldReturn");
        }

        protected override BoundStatement GenerateSetResultCall()
        {
            // ... _exprReturnLabel: ...
            // ... this.state = FinishedState; ...

            // this.promiseOfValueOrEnd.SetResult(false);
            // return;
            // _exprReturnLabelTrue:
            // this.promiseOfValueOrEnd.SetResult(true);

            // ... _exitLabel: ...
            // ... return; ...

            return F.Block(
                // this.promiseOfValueOrEnd.SetResult(false);
                generateSetResultOnPromise(false),
                F.Return(),
                F.Label(_exprReturnLabelTrue),
                // this.promiseOfValueOrEnd.SetResult(true);
                generateSetResultOnPromise(true));

            BoundExpressionStatement generateSetResultOnPromise(bool result)
            {
                // Produce:
                // this.promiseOfValueOrEnd.SetResult(result);
                BoundFieldAccess promiseField = F.InstanceField(_asyncIteratorInfo.PromiseOfValueOrEndField);
                return F.ExpressionStatement(F.Call(promiseField, _asyncIteratorInfo.SetResultMethod, F.Literal(result)));
            }
        }

        protected override BoundStatement GenerateSetExceptionCall(LocalSymbol exceptionLocal)
        {
            // _promiseOfValueOrEnd.SetException(ex);
            return F.ExpressionStatement(F.Call(
                F.InstanceField(_asyncIteratorInfo.PromiseOfValueOrEndField),
                _asyncIteratorInfo.SetExceptionMethod,
                F.Local(exceptionLocal)));
        }

        private BoundStatement GenerateJumpToCurrentFinallyOrExit()
        {
            Debug.Assert((object)_enclosingFinallyOrExitLabel != null);
            return F.If(
                // if (disposeMode)
                F.InstanceField(_asyncIteratorInfo.DisposeModeField),
                // goto finallyOrExitLabel;
                thenClause: F.Goto(_enclosingFinallyOrExitLabel));
        }

        private BoundStatement AppendJumpToCurrentFinallyOrExit(BoundStatement node)
        {
            // Append:
            //  if (disposeMode) goto _enclosingFinallyOrExitLabel;

            return F.Block(
                node,
                GenerateJumpToCurrentFinallyOrExit());
        }

        #region Visitors

        protected override BoundStatement VisitBody(BoundStatement body)
        {
            return F.Block(
                // disposeMode = false;
                SetDisposeMode(false),
                (BoundStatement)Visit(body));
        }

        public override BoundNode VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            // Produce:
            //     _current = expression;
            //     _state = <next_state>;
            //     goto _exprReturnLabelTrue;
            //     <next_state_label>: ;
            //     this.state = cachedState = NotStartedStateMachine;
            //     if (disposeMode) goto _enclosingFinallyOrExitLabel;

            // Note: at label _exprReturnLabelTrue we have:
            //  _promiseOfValueOrEnd.SetResult(true);
            //  return;

            AddState(out int stateNumber, out GeneratedLabelSymbol resumeLabel);

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
                F.Goto(_enclosingFinallyOrExitLabel));
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
        /// - from `yield return`, we'll jump to the relevant `finally` (or method exit)
        /// - after finishing a `finally`, we'll jump to the next relevant `finally` (or method exit)
        ///
        /// Some `finally` clauses may have already been rewritten and extracted to a plain block (<see cref="AsyncExceptionHandlerRewriter"/>).
        /// In those cases, we saved the finally-entry label in <see cref="BoundTryStatement.FinallyLabelOpt"/>.
        /// </summary>
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            LabelSymbol parentFinallyOrExitLabel = _enclosingFinallyOrExitLabel;

            if (node.FinallyBlockOpt != null)
            {
                var finallyEntry = F.GenerateLabel("finallyEntry");
                _enclosingFinallyOrExitLabel = finallyEntry;

                // Add finallyEntry label:
                //  try
                //  {
                //      ...
                //      finallyEntry:
                node = node.Update(
                    tryBlock: F.Block(node.TryBlock, F.Label(finallyEntry)),
                    node.CatchBlocks, node.FinallyBlockOpt, node.FinallyLabelOpt, node.PreferFaultHandler);
            }
            else if ((object)node.FinallyLabelOpt != null)
            {
                _enclosingFinallyOrExitLabel = node.FinallyLabelOpt;
            }

            var result = (BoundStatement)base.VisitTryStatement(node);

            _enclosingFinallyOrExitLabel = parentFinallyOrExitLabel;

            if (node.FinallyBlockOpt != null)
            {
                // Append:
                //  if (disposeMode) /* jump to parent's finally or exit */
                result = AppendJumpToCurrentFinallyOrExit(result);
            }

            // Note: we add this jump to extracted `finally` blocks as well, using `VisitExtractedFinallyBlock` below

            return result;
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
