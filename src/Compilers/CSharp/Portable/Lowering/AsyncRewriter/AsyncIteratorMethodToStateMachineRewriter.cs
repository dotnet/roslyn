// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
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
        /// Where should we jump to to continue the execution of disposal path.
        ///
        /// Initially, this is the method's return value label (<see cref="AsyncMethodToStateMachineRewriter._exprReturnLabel"/>).
        /// Inside a `try` or `catch` with a `finally`, we'll use the label directly preceding the `finally`.
        /// Inside a `try` or `catch` with an extracted `finally`, we will use the label preceding the extracted `finally`.
        /// Inside a `finally`, we'll have no/null label (disposal continues without a jump).
        /// </summary>
        private LabelSymbol _currentDisposalLabel;

        /// <summary>
        /// We use _exprReturnLabel for normal end of method (ie. no more values) and `yield break;`.
        /// We use _exprReturnLabelTrue for `yield return;`.
        /// </summary>
        private readonly LabelSymbol _exprReturnLabelTrue;

        /// <summary>
        /// States for `yield return` are decreasing from <see cref="StateMachineState.InitialAsyncIteratorState"/>.
        /// </summary>
        private readonly ResumableStateMachineStateAllocator _iteratorStateAllocator;

        internal AsyncIteratorMethodToStateMachineRewriter(
            MethodSymbol method,
            int methodOrdinal,
            AsyncMethodBuilderMemberCollection asyncMethodBuilderMemberCollection,
            AsyncIteratorInfo asyncIteratorInfo,
            SyntheticBoundNodeFactory F,
            FieldSymbol state,
            FieldSymbol builder,
            FieldSymbol? instanceIdField,
            IReadOnlySet<Symbol> hoistedVariables,
            IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies,
            ImmutableArray<FieldSymbol> nonReusableFieldsForCleanup,
            SynthesizedLocalOrdinalsDispenser synthesizedLocalOrdinals,
            ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
            VariableSlotAllocator? slotAllocatorOpt,
            int nextFreeHoistedLocalSlot,
            BindingDiagnosticBag diagnostics)
            : base(method, methodOrdinal, asyncMethodBuilderMemberCollection, F,
                  state, builder, instanceIdField, hoistedVariables, nonReusableLocalProxies, nonReusableFieldsForCleanup, synthesizedLocalOrdinals,
                  stateMachineStateDebugInfoBuilder, slotAllocatorOpt, nextFreeHoistedLocalSlot, diagnostics)
        {
            Debug.Assert(asyncIteratorInfo != null);

            _asyncIteratorInfo = asyncIteratorInfo;
            _currentDisposalLabel = _exprReturnLabel;
            _exprReturnLabelTrue = F.GenerateLabel("yieldReturn");

            _iteratorStateAllocator = new ResumableStateMachineStateAllocator(
                slotAllocatorOpt,
                firstState: StateMachineState.FirstResumableAsyncIteratorState,
                increasing: false);
        }

        protected override BoundStatement? GenerateMissingStateDispatch()
        {
            var asyncDispatch = base.GenerateMissingStateDispatch();

            var iteratorDispatch = _iteratorStateAllocator.GenerateThrowMissingStateDispatch(F, F.Local(cachedState), HotReloadExceptionCode.CannotResumeSuspendedIteratorMethod);
            if (iteratorDispatch == null)
            {
                return asyncDispatch;
            }

            return (asyncDispatch != null) ? F.Block(asyncDispatch, iteratorDispatch) : iteratorDispatch;
        }

        protected override BoundStatement GenerateCleanupForExit(ImmutableArray<StateMachineFieldSymbol> rootHoistedLocals)
        {
            // We need to clean nested hoisted local variables too (not just top-level ones)
            // as they are not cleaned when exiting a block if we exit using a `yield break`
            // or if the caller interrupts the enumeration after we reached a `yield return`.
            // So we clean both top-level and nested hoisted local variables
            return GenerateAllHoistedLocalsCleanup();
        }
#nullable disable
        protected override BoundStatement GenerateSetResultCall()
        {
            // ... _exprReturnLabel: ...
            // ... this.state = FinishedState; ...
            // ... hoisted locals cleanup ...

            // if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; } // for enumerables only
            // _current = default;
            // this.builder.Complete();
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
                // _current = default;
                GenerateClearCurrent(),
                GenerateCompleteOnBuilder(),
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

        private BoundExpressionStatement GenerateClearCurrent()
        {
            // _current = default;
            var currentField = _asyncIteratorInfo.CurrentField;
            return F.Assignment(F.InstanceField(currentField), F.Default(currentField.Type));
        }

        private BoundExpressionStatement GenerateCompleteOnBuilder()
        {
            // Produce:
            // this.builder.Complete();
            return F.ExpressionStatement(
                F.Call(
                    F.Field(F.This(), _asyncMethodBuilderField),
                        _asyncMethodBuilderMemberCollection.SetResult, // AsyncIteratorMethodBuilder.Complete is the corresponding method to AsyncTaskMethodBuilder.SetResult
                        ImmutableArray<BoundExpression>.Empty));
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

            // _current = default;
            builder.Add(GenerateClearCurrent());

            // this.builder.Complete();
            builder.Add(GenerateCompleteOnBuilder());

            // _promiseOfValueOrEnd.SetException(ex);
            builder.Add(F.ExpressionStatement(F.Call(
                F.InstanceField(_asyncIteratorInfo.PromiseOfValueOrEndField),
                _asyncIteratorInfo.SetExceptionMethod,
                F.Local(exceptionLocal))));

            return F.Block(builder.ToImmutableAndFree());
        }

        private BoundStatement GenerateJumpToCurrentDisposalLabel()
        {
            Debug.Assert(_currentDisposalLabel is object);
            return F.If(
                // if (disposeMode)
                F.InstanceField(_asyncIteratorInfo.DisposeModeField),
                // goto currentDisposalLabel;
                thenClause: F.Goto(_currentDisposalLabel));
        }

        private BoundStatement AppendJumpToCurrentDisposalLabel(BoundStatement node)
        {
            Debug.Assert(_currentDisposalLabel is object);
            // Append:
            //  if (disposeMode) goto currentDisposalLabel;

            return F.Block(
                node,
                GenerateJumpToCurrentDisposalLabel());
        }

        protected override BoundBinaryOperator ShouldEnterFinallyBlock()
        {
            // We should skip the finally block when:
            // - the state is 0 or greater (we're suspending on an `await`)
            // - the state is -3, -4 or lower (we're suspending on a `yield return`)
            // We don't care about state = -2 (method already completed)

            // So we only want to enter the finally when the state is -1
            return F.IntEqual(F.Local(cachedState), F.Literal(StateMachineState.NotStartedOrRunningState));
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

            AddState(StateMachineState.InitialAsyncIteratorState, out GeneratedLabelSymbol resumeLabel);

            var rewrittenBody = (BoundStatement)Visit(body);

            Debug.Assert(_exprReturnLabel.Equals(_currentDisposalLabel));
            return F.Block(
                F.Label(resumeLabel), // initialStateResumeLabel:
                GenerateJumpToCurrentDisposalLabel(), // if (disposeMode) goto _exprReturnLabel;
                GenerateSetBothStates(StateMachineState.NotStartedOrRunningState), // this.state = cachedState = -1;
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
            //     if (disposeMode) goto currentDisposalLabel;

            // Note: at label _exprReturnLabelTrue we have:
            //  _promiseOfValueOrEnd.SetResult(true);
            //  return;

            AddResumableState(_iteratorStateAllocator, node.Syntax, awaitId: default, out var stateNumber, out GeneratedLabelSymbol resumeLabel);

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
                GenerateSetBothStates(StateMachineState.NotStartedOrRunningState));

            Debug.Assert(_currentDisposalLabel is object); // no yield return allowed inside a finally
            blockBuilder.Add(
                // if (disposeMode) goto currentDisposalLabel;
                GenerateJumpToCurrentDisposalLabel());

            blockBuilder.Add(
                F.HiddenSequencePoint());

            return F.Block(blockBuilder.ToImmutableAndFree());
        }

        public override BoundNode VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            Debug.Assert(_asyncIteratorInfo != null);

            // Produce:
            //  disposeMode = true;
            //  goto currentDisposalLabel;

            Debug.Assert(_currentDisposalLabel is object); // no yield break allowed inside a finally
            return F.Block(
                // disposeMode = true;
                SetDisposeMode(true),
                // goto currentDisposalLabel;
                F.Goto(_currentDisposalLabel));
        }

        protected override BoundStatement MakeAwaitPreamble()
        {
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(F.Diagnostics, F.Compilation.Assembly);
            var field = _asyncIteratorInfo.CurrentField;
            bool isManaged = field.Type.IsManagedType(ref useSiteInfo);
            F.Diagnostics.Add(field.GetFirstLocationOrNone(), useSiteInfo);

            if (isManaged)
            {
                // _current = default;
                return GenerateClearCurrent();
            }

            return null;
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
        /// - from `yield return` within a try, we'll jump to its `finally` if it has one (or method exit)
        /// - after finishing a `finally` within a `finally`, we'll continue
        /// - after finishing a `finally` within a `try`, jump to the its `finally` if it has one (or method exit)
        ///
        /// Some `finally` clauses may have already been rewritten and extracted to a plain block (<see cref="AsyncExceptionHandlerRewriter"/>).
        /// In those cases, we saved the finally-entry label in <see cref="BoundTryStatement.FinallyLabelOpt"/>.
        /// </summary>
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var savedDisposalLabel = _currentDisposalLabel;
            if (node.FinallyBlockOpt is object)
            {
                var finallyEntry = F.GenerateLabel("finallyEntry");
                _currentDisposalLabel = finallyEntry;

                // Add finallyEntry label:
                //  try
                //  {
                //      ...
                //      finallyEntry:
                //  }

                node = node.Update(
                    tryBlock: F.Block(node.TryBlock, F.Label(finallyEntry)),
                    node.CatchBlocks, node.FinallyBlockOpt, node.FinallyLabelOpt, node.PreferFaultHandler);
            }
            else if (node.FinallyLabelOpt is object)
            {
                _currentDisposalLabel = node.FinallyLabelOpt;
            }

            var result = (BoundStatement)base.VisitTryStatement(node);

            _currentDisposalLabel = savedDisposalLabel;

            if (node.FinallyBlockOpt != null && _currentDisposalLabel is object)
            {
                // Append:
                //  if (disposeMode) goto currentDisposalLabel;
                result = AppendJumpToCurrentDisposalLabel(result);
            }

            // Note: we add this jump to extracted `finally` blocks as well, using `VisitExtractedFinallyBlock` below

            return result;
        }

        protected override BoundBlock VisitFinally(BoundBlock finallyBlock)
        {
            // within a finally, continuing disposal doesn't require any jump
            var savedDisposalLabel = _currentDisposalLabel;
            _currentDisposalLabel = null;
            var result = base.VisitFinally(finallyBlock);
            _currentDisposalLabel = savedDisposalLabel;
            return result;
        }

        /// <summary>
        /// Some `finally` clauses may have already been rewritten and extracted to a plain block (<see cref="AsyncExceptionHandlerRewriter"/>).
        /// The extracted block will have been wrapped as a <see cref="BoundExtractedFinallyBlock"/> so that we can process it as a `finally` block here.
        /// </summary>
        public override BoundNode VisitExtractedFinallyBlock(BoundExtractedFinallyBlock extractedFinally)
        {
            // Remove the wrapping and optionally append:
            //  if (disposeMode) goto currentDisposalLabel;

            BoundStatement result = VisitFinally(extractedFinally.FinallyBlock);

            if (_currentDisposalLabel is object)
            {
                result = AppendJumpToCurrentDisposalLabel(result);
            }

            return result;
        }

        #endregion Visitors
    }
}
