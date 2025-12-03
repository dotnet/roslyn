// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed partial class RuntimeAsyncIteratorRewriter
{
    /// <summary>
    /// Generates the MoveNextAsync method for an async-iterator state machine with runtime-async codegen.
    /// This means that the generated MoveNextAsync method is itself async and its body contains awaits which will be lowered later on.
    /// But the `yield return` and `yield break` statements are lowered away here.
    /// The entrypoint is <see cref="GenerateMoveNextAsync"/>.
    /// We use the same state values as for regular async-iterators, minus the states for `await` suspensions.
    /// </summary>
    internal sealed class MoveNextAsyncRewriter : MethodToStateMachineRewriter
    {
        private readonly FieldSymbol _currentField;
        private readonly FieldSymbol _disposeModeField; // whether the state machine is in dispose mode (ie. skipping all logic except that in `catch` and `finally`, yielding no new elements)
        private readonly FieldSymbol? _combinedTokensField; // CancellationTokenSource for combining tokens

        /// <summary>
        /// Where should we jump to to continue the execution of disposal path.
        ///
        /// Initially, this is the method's top-level disposal label, indicating the end of the method.
        /// Inside a `try` or `catch` with a `finally`, we'll use the label directly preceding the `finally`.
        /// Inside a `try` or `catch` with an extracted `finally`, we will use the label preceding the extracted `finally`.
        /// Inside a `finally`, we'll have no/null label (disposal continues without a jump).
        /// See <see cref="VisitTryStatement"/> and <see cref="VisitFinally"/>
        /// </summary>
        private LabelSymbol? _currentDisposalLabel;

        /// <summary>
        /// States for `yield return` are decreasing from <see cref="StateMachineState.InitialAsyncIteratorState"/>.
        /// </summary>
        private readonly ResumableStateMachineStateAllocator _iteratorStateAllocator;

        internal MoveNextAsyncRewriter(
            SyntheticBoundNodeFactory F,
            MethodSymbol originalMethod,
            FieldSymbol state,
            FieldSymbol? instanceIdField,
            IReadOnlySet<Symbol> hoistedVariables,
            IReadOnlyDictionary<Symbol, CapturedSymbolReplacement> nonReusableLocalProxies,
            ImmutableArray<FieldSymbol> nonReusableFieldsForCleanup,
            SynthesizedLocalOrdinalsDispenser synthesizedLocalOrdinals,
            ArrayBuilder<StateMachineStateDebugInfo> stateMachineStateDebugInfoBuilder,
            VariableSlotAllocator? slotAllocator,
            int nextFreeHoistedLocalSlot,
            BindingDiagnosticBag diagnostics,
            FieldSymbol currentField,
            FieldSymbol disposeModeField,
            FieldSymbol? combinedTokensField)
            : base(F, originalMethod, state, instanceIdField, hoistedVariables, nonReusableLocalProxies, nonReusableFieldsForCleanup,
                  synthesizedLocalOrdinals, stateMachineStateDebugInfoBuilder, slotAllocator, nextFreeHoistedLocalSlot, diagnostics)
        {
            _currentField = currentField;
            _disposeModeField = disposeModeField;
            _combinedTokensField = combinedTokensField;

            _currentDisposalLabel = F.GenerateLabel("topLevelDisposeLabel");
            _iteratorStateAllocator = new ResumableStateMachineStateAllocator(
                slotAllocator,
                firstState: StateMachineState.FirstResumableAsyncIteratorState,
                increasing: false);
        }

        /// <inheritdoc cref="MoveNextAsyncRewriter"/>
        [SuppressMessage("Style", """VSTHRD200:Use "Async" suffix for async methods""", Justification = "Standard naming convention for generating 'MoveNextAsync'")]
        internal BoundStatement GenerateMoveNextAsync(BoundStatement body)
        {
            var rewrittenBody = visitBody(body);

            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            // {
            blockBuilder.Add(F.HiddenSequencePoint());

            // cachedState = this.state
            blockBuilder.Add(F.Assignment(F.Local(cachedState), F.Field(F.This(), stateField)));

            // cachedThis = capturedThis; // if needed
            blockBuilder.Add(CacheThisIfNeeded());

            // try
            // {
            //   switch (cachedState) ... dispatch ...
            //  ... rewritten body, which ends with `disposeMode = true; goto topLevelDisposeLabel;` ...
            // }
            // catch (Exception ex)
            // {
            //   _state = finishedState;
            //   if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; }
            //   _current = default;
            //   ... clear locals ...
            //   throw;
            // }
            var exceptionLocal = F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Exception));
            blockBuilder.Add(
                GenerateTopLevelTry(
                    F.Block(
                        // switch (cachedState) ... dispatch ...
                        F.HiddenSequencePoint(),
                        Dispatch(isOutermost: true),
                        // ... rewritten body ...
                        rewrittenBody
                    ),
                    F.CatchBlocks(generateExceptionHandling(exceptionLocal)))
                );

            // TopLevelDisposeLabel:
            Debug.Assert(_currentDisposalLabel is not null);
            blockBuilder.Add(F.Label(_currentDisposalLabel));

            var block = (BlockSyntax)body.Syntax;

            // this.state = finishedState
            var stateDone = F.Assignment(F.Field(F.This(), stateField), F.Literal(StateMachineState.FinishedState));
            blockBuilder.Add(F.SequencePointWithSpan(block, block.CloseBraceToken.Span, stateDone));
            blockBuilder.Add(F.HiddenSequencePoint());

            // We need to clean nested hoisted local variables too (not just top-level ones)
            // as they are not cleaned when exiting a block if we exit using a `yield break`
            // or if the caller interrupts the enumeration after we reached a `yield return`.
            // So we clean both top-level and nested hoisted local variables

            // clear managed hoisted locals
            blockBuilder.Add(GenerateAllHoistedLocalsCleanup());

            // if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; }
            addDisposeCombinedTokensIfNeeded(blockBuilder);

            // _current = default;
            blockBuilder.Add(GenerateClearCurrent());

            // Note: we're producing a runtime-async body, so return a bool instead of ValueTask<bool>
            // return false;
            var resultFalse = new BoundReturnStatement(F.Syntax, RefKind.None, F.Literal(false), @checked: false) { WasCompilerGenerated = true };
            blockBuilder.Add(resultFalse);

            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            locals.Add(cachedState);
            if (cachedThis is not null) locals.Add(cachedThis);

            var newBody = F.Block(locals.ToImmutableAndFree(), blockBuilder.ToImmutableAndFree());
            return F.Instrument(newBody, instrumentation);

            BoundCatchBlock generateExceptionHandling(LocalSymbol exceptionLocal)
            {
                // catch (Exception ex)
                // {
                //     _state = finishedState;
                //
                //     for each hoisted local:
                //       <>x__y = default
                //
                //     if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; }
                //
                //     _current = default;
                //     throw;
                // }

                var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                // _state = finishedState;
                BoundStatement assignFinishedState =
                    F.ExpressionStatement(F.AssignmentExpression(F.Field(F.This(), stateField), F.Literal(StateMachineState.FinishedState)));
                blockBuilder.Add(assignFinishedState);

                blockBuilder.Add(GenerateAllHoistedLocalsCleanup());

                // if (this.combinedTokens != null) { this.combinedTokens.Dispose(); this.combinedTokens = null; }
                addDisposeCombinedTokensIfNeeded(blockBuilder);

                // _current = default;
                blockBuilder.Add(GenerateClearCurrent());

                // throw null;
                blockBuilder.Add(F.Throw(null));

                return new BoundCatchBlock(
                    F.Syntax,
                    [exceptionLocal],
                    F.Local(exceptionLocal),
                    exceptionLocal.Type,
                    exceptionFilterPrologueOpt: null,
                    exceptionFilterOpt: null,
                    body: F.Block(blockBuilder.ToImmutableAndFree()),
                    isSynthesizedAsyncCatchAll: true);
            }

            void addDisposeCombinedTokensIfNeeded(ArrayBuilder<BoundStatement> builder)
            {
                AsyncIteratorMethodToStateMachineRewriter.AddDisposeCombinedTokensIfNeeded(builder, _combinedTokensField, F);
            }

            // Lower the body, adding an entry state (-3) at the start,
            // so that we can differentiate an async-iterator that was never moved forward with MoveNextAsync()
            // from one that is running (-1).
            // Then we can guard against some bad usages of DisposeAsync.
            BoundStatement visitBody(BoundStatement body)
            {
                // Produce:
                //  initialStateResumeLabel:
                //  if (disposeMode) goto _exprReturnLabel;
                //  this.state = cachedState = -1;
                //  ... rewritten body, which ends with `disposeMode = true; goto topLevelDisposeLabel;` ...

                AddState(StateMachineState.InitialAsyncIteratorState, out GeneratedLabelSymbol resumeLabel);

                var rewrittenBody = (BoundStatement)Visit(body);

                return F.Block(
                    F.Label(resumeLabel), // initialStateResumeLabel:
                    GenerateConditionalJumpToCurrentDisposalLabel(), // if (disposeMode) goto _exprReturnLabel;
                    GenerateSetBothStates(StateMachineState.NotStartedOrRunningState), // this.state = cachedState = -1;
                    rewrittenBody);
            }
        }

        internal BoundStatement GenerateTopLevelTry(BoundBlock tryBlock, ImmutableArray<BoundCatchBlock> catchBlocks)
            => F.Try(tryBlock, catchBlocks);

        private BoundExpressionStatement GenerateClearCurrent()
        {
            Debug.Assert(_currentField is not null);

            // _current = default;
            return F.Assignment(F.InstanceField(_currentField), F.Default(_currentField.Type));
        }

        private BoundStatement GenerateConditionalJumpToCurrentDisposalLabel()
        {
            Debug.Assert(_currentDisposalLabel is not null);
            return F.If(
                // if (disposeMode)
                F.InstanceField(_disposeModeField),
                //   goto currentDisposalLabel;
                thenClause: F.Goto(_currentDisposalLabel));
        }

        private BoundStatement AppendConditionalJumpToCurrentDisposalLabel(BoundStatement node)
        {
            Debug.Assert(_currentDisposalLabel is not null);
            // Append:
            //  if (disposeMode) goto currentDisposalLabel;

            return F.Block(
                node,
                GenerateConditionalJumpToCurrentDisposalLabel());
        }

        private BoundExpressionStatement SetDisposeMode()
        {
            return F.Assignment(F.InstanceField(_disposeModeField), F.Literal(true));
        }

        protected override BoundStatement GenerateReturn(bool finished)
        {
            throw ExceptionUtilities.Unreachable();
        }

        protected override BoundBinaryOperator ShouldEnterFinallyBlock()
        {
            return AsyncIteratorMethodToStateMachineRewriter.ShouldEnterFinallyBlock(cachedState, F);
        }

        protected override StateMachineState FirstIncreasingResumableState
            => StateMachineState.FirstResumableIteratorState;

        protected override HotReloadExceptionCode EncMissingStateErrorCode
            => HotReloadExceptionCode.CannotResumeSuspendedAsyncMethod; // PROTOTYPE revisit for EnC support

        /// <summary>
        /// Containing Symbols are not checked after this step - for performance reasons we can allow inaccurate locals
        /// </summary>
        protected override bool EnforceAccurateContainerForLocals => false;

        #region Visitor methods
        public override BoundNode? VisitYieldReturnStatement(BoundYieldReturnStatement node)
        {
            // Produce:
            //     _current = expression;
            //     _state = cachedState = <next_state>;
            //     return true;
            //     <next_state_resume_label>: ;
            //     <hidden sequence point>
            //     _state = cachedState = NotStartedStateMachine;
            //     if (disposeMode) goto currentDisposalLabel;

            AddResumableState(_iteratorStateAllocator, node.Syntax, awaitId: default,
                out StateMachineState nextState, out GeneratedLabelSymbol nextStateResumeLabel);

            var rewrittenExpression = (BoundExpression)Visit(node.Expression);
            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            // _current = expression;
            blockBuilder.Add(F.Assignment(F.InstanceField(_currentField), rewrittenExpression));

            // this.state = cachedState = stateNumber;
            // Note: we set cachedState too as it used to skip any finally blocks
            blockBuilder.Add(GenerateSetBothStates(nextState));

            // Note: we're producing a runtime-async method, so return a bool instead of ValueTask<bool>
            // return true;
            var resultTrue = new BoundReturnStatement(F.Syntax, RefKind.None, F.Literal(true), @checked: false) { WasCompilerGenerated = true };
            blockBuilder.Add(resultTrue);

            // <next_state_label>: ;
            blockBuilder.Add(F.Label(nextStateResumeLabel));
            blockBuilder.Add(F.HiddenSequencePoint());

            // this.state = cachedState = NotStartedStateMachine
            blockBuilder.Add(GenerateSetBothStates(StateMachineState.NotStartedOrRunningState));

            // if (disposeMode) goto currentDisposalLabel;
            Debug.Assert(_currentDisposalLabel is not null); // no yield return allowed inside a finally
            blockBuilder.Add(GenerateConditionalJumpToCurrentDisposalLabel());

            blockBuilder.Add(F.HiddenSequencePoint());

            return F.Block(blockBuilder.ToImmutableAndFree());
        }

        public override BoundNode? VisitYieldBreakStatement(BoundYieldBreakStatement node)
        {
            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            // disposeMode = true;
            blockBuilder.Add(SetDisposeMode());

            // goto currentDisposalLabel;
            Debug.Assert(_currentDisposalLabel is not null);
            blockBuilder.Add(F.Goto(_currentDisposalLabel));

            return F.Block(blockBuilder.ToImmutableAndFree());
        }

        public override BoundNode? VisitReturnStatement(BoundReturnStatement node)
        {
            Debug.Assert(_currentDisposalLabel is not null);
            return F.Block(SetDisposeMode(), F.Goto(_currentDisposalLabel));
        }

        /// <inheritdoc cref="AsyncIteratorMethodToStateMachineRewriter.VisitTryStatement"/>
        public override BoundNode VisitTryStatement(BoundTryStatement node)
        {
            var savedDisposalLabel = _currentDisposalLabel;
            LabelSymbol? afterFinally = null;
            if (node.FinallyBlockOpt is not null)
            {
                afterFinally = F.GenerateLabel("afterFinally");
                _currentDisposalLabel = afterFinally;
            }
            else if (node.FinallyLabelOpt is not null)
            {
                _currentDisposalLabel = node.FinallyLabelOpt;
            }

            var result = (BoundStatement)base.VisitTryStatement(node);

            if (afterFinally != null)
            {
                // Append a label immediately after the try-catch-finally statement,
                // which disposal within `try`/`catch` blocks jumps to in order to pass control flow to the `finally` block implicitly:
                //  tryEnd:
                result = F.Block(result, F.Label(afterFinally));
            }

            _currentDisposalLabel = savedDisposalLabel;

            if (node.FinallyBlockOpt != null && _currentDisposalLabel is not null)
            {
                // Append:
                //  if (disposeMode) goto currentDisposalLabel;
                result = AppendConditionalJumpToCurrentDisposalLabel(result);
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

        /// <inheritdoc cref="AsyncIteratorMethodToStateMachineRewriter.VisitExtractedFinallyBlock"/>
        public override BoundNode VisitExtractedFinallyBlock(BoundExtractedFinallyBlock extractedFinally)
        {
            // Remove the wrapping and optionally append:
            //  if (disposeMode) goto currentDisposalLabel;

            BoundStatement result = VisitFinally(extractedFinally.FinallyBlock);

            if (_currentDisposalLabel is not null)
            {
                result = AppendConditionalJumpToCurrentDisposalLabel(result);
            }

            return result;
        }

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            // We need to clear _current before awaiting when it is a managed type,
            // ie. so we may release some references.
            BoundExpression? preamble = makeAwaitPreamble();
            var rewrittenAwait = (BoundExpression?)base.VisitAwaitExpression(node);
            Debug.Assert(rewrittenAwait is not null);

            if (preamble is null)
            {
                return rewrittenAwait;
            }

            return F.Sequence([], [preamble], rewrittenAwait);

            BoundExpression? makeAwaitPreamble()
            {
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(F.Diagnostics, F.Compilation.Assembly);
                var field = _currentField;
                bool isManaged = field.Type.IsManagedType(ref useSiteInfo);
                F.Diagnostics.Add(field.GetFirstLocationOrNone(), useSiteInfo);

                if (isManaged)
                {
                    // _current = default;
                    return F.AssignmentExpression(F.InstanceField(_currentField), F.Default(_currentField.Type));
                }

                return null;
            }
        }

        public override BoundNode? VisitLambda(BoundLambda node)
            => throw ExceptionUtilities.Unreachable();

        public override BoundNode? VisitUnboundLambda(UnboundLambda node)
            => throw ExceptionUtilities.Unreachable();

        public override BoundNode? VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
            => throw ExceptionUtilities.Unreachable();
        #endregion
    }
}
