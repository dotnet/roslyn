// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Produces a MoveNext() method for an async method.
    /// </summary>
    internal class AsyncMethodToStateMachineRewriter : MethodToStateMachineRewriter
    {
        /// <summary>
        /// The method being rewritten.
        /// </summary>
        protected readonly MethodSymbol _method;

        /// <summary>
        /// The field of the generated async class used to store the async method builder: an instance of
        /// <see cref="AsyncVoidMethodBuilder"/>, <see cref="AsyncTaskMethodBuilder"/>, or <see cref="AsyncTaskMethodBuilder{TResult}"/> depending on the
        /// return type of the async method.
        /// </summary>
        protected readonly FieldSymbol _asyncMethodBuilderField;

        /// <summary>
        /// A collection of well-known members for the current async method builder.
        /// </summary>
        protected readonly AsyncMethodBuilderMemberCollection _asyncMethodBuilderMemberCollection;

        /// <summary>
        /// The exprReturnLabel is used to label the return handling code at the end of the async state-machine
        /// method. Return expressions are rewritten as unconditional branches to exprReturnLabel.
        /// </summary>
        protected readonly LabelSymbol _exprReturnLabel;

        /// <summary>
        /// The label containing a return from the method when the async method has not completed.
        /// </summary>
        private readonly LabelSymbol _exitLabel;

        /// <summary>
        /// The field of the generated async class used in generic task returning async methods to store the value
        /// of rewritten return expressions. The return-handling code then uses <c>SetResult</c> on the async method builder
        /// to make the result available to the caller.
        /// </summary>
        private readonly LocalSymbol? _exprRetValue;

        private readonly LoweredDynamicOperationFactory _dynamicFactory;

        private readonly Dictionary<TypeSymbol, FieldSymbol> _awaiterFields;
        private int _nextAwaiterId;

        private readonly Dictionary<BoundValuePlaceholderBase, BoundExpression> _placeholderMap;

        /// <summary>
        /// Containing Symbols are not checked after this step - for performance reasons we can allow inaccurate locals
        /// </summary>
        protected override bool EnforceAccurateContainerForLocals => false;

        internal AsyncMethodToStateMachineRewriter(
            MethodSymbol method,
            int methodOrdinal,
            AsyncMethodBuilderMemberCollection asyncMethodBuilderMemberCollection,
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
            : base(F, method, state, instanceIdField, hoistedVariables, nonReusableLocalProxies, nonReusableFieldsForCleanup, synthesizedLocalOrdinals, stateMachineStateDebugInfoBuilder, slotAllocatorOpt, nextFreeHoistedLocalSlot, diagnostics)
        {
            _method = method;
            _asyncMethodBuilderMemberCollection = asyncMethodBuilderMemberCollection;
            _asyncMethodBuilderField = builder;
            _exprReturnLabel = F.GenerateLabel("exprReturn");
            _exitLabel = F.GenerateLabel("exitLabel");

            _exprRetValue = method.IsAsyncEffectivelyReturningGenericTask(F.Compilation)
                ? F.SynthesizedLocal(asyncMethodBuilderMemberCollection.ResultType, syntax: F.Syntax, kind: SynthesizedLocalKind.AsyncMethodReturnValue)
                : null;

            _dynamicFactory = new LoweredDynamicOperationFactory(F, methodOrdinal);
            _awaiterFields = new Dictionary<TypeSymbol, FieldSymbol>(Symbols.SymbolEqualityComparer.IgnoringDynamicTupleNamesAndNullability);
            _nextAwaiterId = slotAllocatorOpt?.PreviousAwaiterSlotCount ?? 0;

            _placeholderMap = new Dictionary<BoundValuePlaceholderBase, BoundExpression>();
        }

#nullable disable

        private FieldSymbol GetAwaiterField(TypeSymbol awaiterType)
        {
            FieldSymbol result;

            // Awaiters of the same type always share the same slot, regardless of what await expressions they belong to.
            // Even in case of nested await expressions only one awaiter is active.
            // So we don't need to tie the awaiter variable to a particular await expression and only use its type
            // to find the previous awaiter field.
            if (!_awaiterFields.TryGetValue(awaiterType, out result))
            {
                int slotIndex;
                if (slotAllocator == null || !slotAllocator.TryGetPreviousAwaiterSlotIndex(F.ModuleBuilderOpt.Translate(awaiterType, F.Syntax, F.Diagnostics.DiagnosticBag), F.Diagnostics.DiagnosticBag, out slotIndex))
                {
                    slotIndex = _nextAwaiterId++;
                }

                string fieldName = GeneratedNames.AsyncAwaiterFieldName(slotIndex);
                result = F.StateMachineField(awaiterType, fieldName, SynthesizedLocalKind.AwaiterField, slotIndex);
                _awaiterFields.Add(awaiterType, result);
            }

            return result;
        }

        protected sealed override HotReloadExceptionCode EncMissingStateErrorCode
            => HotReloadExceptionCode.CannotResumeSuspendedAsyncMethod;

        protected sealed override StateMachineState FirstIncreasingResumableState
            => StateMachineState.FirstResumableAsyncState;

        /// <summary>
        /// Generate the body for <c>MoveNext()</c>.
        /// </summary>
        internal void GenerateMoveNext(BoundStatement body, MethodSymbol moveNextMethod)
        {
            F.CurrentFunction = moveNextMethod;
            BoundStatement rewrittenBody = VisitBody(body);

            ImmutableArray<StateMachineFieldSymbol> rootScopeHoistedLocals;
            TryUnwrapBoundStateMachineScope(ref rewrittenBody, out rootScopeHoistedLocals);

            var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            bodyBuilder.Add(F.HiddenSequencePoint());
            bodyBuilder.Add(F.Assignment(F.Local(cachedState), F.Field(F.This(), stateField)));
            bodyBuilder.Add(CacheThisIfNeeded());

            var exceptionLocal = F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Exception));
            bodyBuilder.Add(
                GenerateTopLevelTry(
                    F.Block(ImmutableArray<LocalSymbol>.Empty,
                        // switch (state) ...
                        F.HiddenSequencePoint(),
                        Dispatch(isOutermost: true),
                        // [body]
                        rewrittenBody
                    ),
                    F.CatchBlocks(generateExceptionHandling(exceptionLocal, rootScopeHoistedLocals)))
                );

            // ReturnLabel (for the rewritten return expressions in the user's method body)
            bodyBuilder.Add(F.Label(_exprReturnLabel));

            // this.state = finishedState
            var stateDone = F.Assignment(F.Field(F.This(), stateField), F.Literal(this.FinishedState));
            var block = body.Syntax as BlockSyntax;
            if (block == null)
            {
                // this happens, for example, in (async () => await e) where there is no block syntax
                bodyBuilder.Add(stateDone);
            }
            else
            {
                bodyBuilder.Add(F.SequencePointWithSpan(block, block.CloseBraceToken.Span, stateDone));
                bodyBuilder.Add(F.HiddenSequencePoint());
                // The remaining code is hidden to hide the fact that it can run concurrently with the task's continuation
            }

            bodyBuilder.Add(GenerateCleanupForExit(rootScopeHoistedLocals));

            bodyBuilder.Add(GenerateSetResultCall());

            // this code is hidden behind a hidden sequence point.
            bodyBuilder.Add(F.Label(_exitLabel));
            bodyBuilder.Add(F.Return());

            var newStatements = bodyBuilder.ToImmutableAndFree();

            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            locals.Add(cachedState);
            if ((object)cachedThis != null) locals.Add(cachedThis);
            if ((object)_exprRetValue != null) locals.Add(_exprRetValue);

            var newBody =
                F.SequencePoint(
                    body.Syntax,
                    F.Block(
                        locals.ToImmutableAndFree(),
                        newStatements));

            if (rootScopeHoistedLocals.Length > 0)
            {
                newBody = MakeStateMachineScope(rootScopeHoistedLocals, newBody);
            }

            newBody = F.Instrument(newBody, instrumentation);

            F.CloseMethod(newBody);
            return;

            BoundCatchBlock generateExceptionHandling(LocalSymbol exceptionLocal, ImmutableArray<StateMachineFieldSymbol> rootHoistedLocals)
            {
                // catch (Exception ex)
                // {
                //     _state = finishedState;
                //
                //     for each hoisted local:
                //     <>x__y = default
                //
                //     builder.SetException(ex);  OR  if (this.combinedTokens != null) this.combinedTokens.Dispose(); _promiseOfValueOrEnd.SetException(ex); /* for async-iterator method */
                //     return;
                // }

                // _state = finishedState;
                BoundStatement assignFinishedState =
                    F.ExpressionStatement(F.AssignmentExpression(F.Field(F.This(), stateField), F.Literal(this.FinishedState)));

                // builder.SetException(ex);  OR  if (this.combinedTokens != null) this.combinedTokens.Dispose(); _promiseOfValueOrEnd.SetException(ex);
                BoundStatement callSetException = GenerateSetExceptionCall(exceptionLocal);

                return new BoundCatchBlock(
                    F.Syntax,
                    ImmutableArray.Create(exceptionLocal),
                    F.Local(exceptionLocal),
                    exceptionLocal.Type,
                    exceptionFilterPrologueOpt: null,
                    exceptionFilterOpt: null,
                    body: F.Block(
                        assignFinishedState, // _state = finishedState;
                        GenerateCleanupForExit(rootHoistedLocals),
                        callSetException, // builder.SetException(ex);  OR  _promiseOfValueOrEnd.SetException(ex);
                        GenerateReturn(false)), // return;
                    isSynthesizedAsyncCatchAll: true);
            }
        }

        protected virtual StateMachineState FinishedState
            => StateMachineState.AsyncFinishedState;

        protected virtual BoundStatement GenerateTopLevelTry(BoundBlock tryBlock, ImmutableArray<BoundCatchBlock> catchBlocks)
            => F.Try(tryBlock, catchBlocks);

        protected virtual BoundStatement GenerateSetResultCall()
        {
            // builder.SetResult([RetVal])
            return F.ExpressionStatement(
                F.Call(
                    F.Field(F.This(), _asyncMethodBuilderField),
                    _asyncMethodBuilderMemberCollection.SetResult,
                    _method.IsAsyncEffectivelyReturningGenericTask(F.Compilation)
                        ? ImmutableArray.Create<BoundExpression>(F.Local(_exprRetValue))
                        : ImmutableArray<BoundExpression>.Empty));
        }

        protected virtual BoundStatement GenerateCleanupForExit(ImmutableArray<StateMachineFieldSymbol> rootHoistedLocals)
        {
            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            // Cleanup all hoisted local variables
            // so that they can be collected by GC if needed
            foreach (var hoistedLocal in rootHoistedLocals)
            {
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(F.Diagnostics, F.Compilation.Assembly);
                var isManagedType = hoistedLocal.Type.IsManagedType(ref useSiteInfo);
                F.Diagnostics.Add(hoistedLocal.GetFirstLocationOrNone(), useSiteInfo);
                if (!isManagedType)
                {
                    continue;
                }

                builder.Add(F.Assignment(F.Field(F.This(), hoistedLocal), F.NullOrDefault(hoistedLocal.Type)));
            }

            return F.Block(builder.ToImmutableAndFree());
        }

        protected virtual BoundStatement GenerateSetExceptionCall(LocalSymbol exceptionLocal)
        {
            Debug.Assert(!CurrentMethod.IsIterator); // an override handles async-iterators

            // builder.SetException(ex);
            return F.ExpressionStatement(
                F.Call(
                    F.Field(F.This(), _asyncMethodBuilderField),
                    _asyncMethodBuilderMemberCollection.SetException,
                    F.Local(exceptionLocal)));
        }

        protected sealed override BoundStatement GenerateReturn(bool finished)
        {
            return F.Goto(_exitLabel);
        }

        #region Visitors

        protected virtual BoundStatement VisitBody(BoundStatement body)
            => (BoundStatement)Visit(body);

        public sealed override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
        {
            if (node.Expression.Kind == BoundKind.AwaitExpression)
            {
                return VisitAwaitExpression((BoundAwaitExpression)node.Expression, resultPlace: null);
            }
            else if (node.Expression.Kind == BoundKind.AssignmentOperator)
            {
                var expression = (BoundAssignmentOperator)node.Expression;
                if (expression.Right.Kind == BoundKind.AwaitExpression)
                {
                    return VisitAwaitExpression((BoundAwaitExpression)expression.Right, resultPlace: expression.Left);
                }
            }

            BoundExpression expr = (BoundExpression)this.Visit(node.Expression);
            return (expr != null) ? node.Update(expr) : (BoundStatement)F.StatementList();
        }

        public sealed override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            // await expressions must, by now, have been moved to the top level.
            throw ExceptionUtilities.Unreachable();
        }

        public sealed override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression
            return node;
        }

#nullable enable
        protected virtual BoundStatement? MakeAwaitPreamble() { return null; }
#nullable disable

        private BoundBlock VisitAwaitExpression(BoundAwaitExpression node, BoundExpression resultPlace)
        {
            Debug.Assert(node.AwaitableInfo.RuntimeAsyncAwaitCall is null);
            BoundStatement preamble = MakeAwaitPreamble();

            var expression = (BoundExpression)Visit(node.Expression);
            var awaitablePlaceholder = node.AwaitableInfo.AwaitableInstancePlaceholder;

            if (awaitablePlaceholder != null)
            {
                _placeholderMap.Add(awaitablePlaceholder, expression);
            }

            var getAwaiter = node.AwaitableInfo.IsDynamic ?
                MakeCallMaybeDynamic(expression, null, WellKnownMemberNames.GetAwaiter) :
                (BoundExpression)Visit(node.AwaitableInfo.GetAwaiter);

            resultPlace = (BoundExpression)Visit(resultPlace);
            MethodSymbol getResult = VisitMethodSymbol(node.AwaitableInfo.GetResult);
            MethodSymbol isCompletedMethod = ((object)node.AwaitableInfo.IsCompleted != null) ? VisitMethodSymbol(node.AwaitableInfo.IsCompleted.GetMethod) : null;
            TypeSymbol type = VisitType(node.Type);

            if (awaitablePlaceholder != null)
            {
                _placeholderMap.Remove(awaitablePlaceholder);
            }

            // The awaiter temp facilitates EnC method remapping and thus have to be long-lived.
            // It transfers the awaiter objects from the old version of the MoveNext method to the new one.
            Debug.Assert(node.Syntax.IsKind(SyntaxKind.AwaitExpression) || node.WasCompilerGenerated);

            var awaiterTemp = F.SynthesizedLocal(getAwaiter.Type, syntax: node.Syntax, kind: SynthesizedLocalKind.Awaiter);
            var awaitIfIncomplete = F.Block(
                    // temp $awaiterTemp = <expr>.GetAwaiter();
                    F.Assignment(
                        F.Local(awaiterTemp),
                        getAwaiter),

                    // hidden sequence point facilitates EnC method remapping, see explanation on SynthesizedLocalKind.Awaiter:
                    F.HiddenSequencePoint(),

                    // if(!($awaiterTemp.IsCompleted)) { ... }
                    F.If(
                        condition: F.Not(GenerateGetIsCompleted(awaiterTemp, isCompletedMethod)),
                        thenClause: GenerateAwaitForIncompleteTask(awaiterTemp, node.DebugInfo)));
            BoundExpression getResultCall = MakeCallMaybeDynamic(
                F.Local(awaiterTemp),
                getResult,
                WellKnownMemberNames.GetResult,
                resultsDiscarded: resultPlace == null);

            // [$resultPlace = ] $awaiterTemp.GetResult();
            BoundStatement getResultStatement = resultPlace != null && !type.IsVoidType() ?
                F.Assignment(resultPlace, getResultCall) :
                F.ExpressionStatement(getResultCall);

            var statementsBuilder = ArrayBuilder<BoundStatement>.GetInstance(preamble is null ? 2 : 3);
            if (preamble is not null)
            {
                statementsBuilder.Add(preamble);
            }
            statementsBuilder.Add(awaitIfIncomplete);
            statementsBuilder.Add(getResultStatement);

            return F.Block([awaiterTemp], statementsBuilder.ToImmutableAndFree());
        }

        public override BoundNode VisitAwaitableValuePlaceholder(BoundAwaitableValuePlaceholder node)
        {
            return _placeholderMap[node];
        }

        private BoundExpression MakeCallMaybeDynamic(
            BoundExpression receiver,
            MethodSymbol methodSymbol = null,
            string methodName = null,
            bool resultsDiscarded = false)
        {
            if ((object)methodSymbol != null)
            {
                // non-dynamic:
                Debug.Assert(receiver != null);

                return methodSymbol.IsStatic
                    ? F.StaticCall(methodSymbol.ContainingType, methodSymbol, receiver)
                    : F.Call(receiver, methodSymbol);
            }

            // dynamic:
            Debug.Assert(methodName != null);
            return _dynamicFactory.MakeDynamicMemberInvocation(
                methodName,
                receiver,
                typeArgumentsWithAnnotations: ImmutableArray<TypeWithAnnotations>.Empty,
                loweredArguments: ImmutableArray<BoundExpression>.Empty,
                argumentNames: ImmutableArray<string>.Empty,
                refKinds: ImmutableArray<RefKind>.Empty,
                hasImplicitReceiver: false,
                resultDiscarded: resultsDiscarded).ToExpression();
        }

        private BoundExpression GenerateGetIsCompleted(LocalSymbol awaiterTemp, MethodSymbol getIsCompletedMethod)
        {
            if (awaiterTemp.Type.IsDynamic())
            {
                return _dynamicFactory.MakeDynamicConversion(
                    _dynamicFactory.MakeDynamicGetMember(
                        F.Local(awaiterTemp),
                        WellKnownMemberNames.IsCompleted,
                        false).ToExpression(),
                    isExplicit: true,
                    isArrayIndex: false,
                    isChecked: false,
                    resultType: F.SpecialType(SpecialType.System_Boolean)).ToExpression();
            }

            return F.Call(F.Local(awaiterTemp), getIsCompletedMethod);
        }

        private BoundBlock GenerateAwaitForIncompleteTask(LocalSymbol awaiterTemp, BoundAwaitExpressionDebugInfo debugInfo)
        {
            var awaitSyntax = awaiterTemp.GetDeclaratorSyntax();
            AddResumableState(awaitSyntax, debugInfo.AwaitId, out StateMachineState stateNumber, out GeneratedLabelSymbol resumeLabel);

            TypeSymbol awaiterFieldType = awaiterTemp.Type.IsVerifierReference()
                ? F.SpecialType(SpecialType.System_Object)
                : awaiterTemp.Type;

            FieldSymbol awaiterField = GetAwaiterField(awaiterFieldType);

            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            blockBuilder.Add(
                // this.state = cachedState = stateForLabel
                GenerateSetBothStates(stateNumber));

            blockBuilder.Add(
                    // Emit await yield point to be injected into PDB
                    F.NoOp(NoOpStatementFlavor.AwaitYieldPoint));

            // this.<>t__awaiter = $awaiterTemp

            BoundExpression awaiterTempRef = F.Local(awaiterTemp);

            if (!TypeSymbol.Equals(awaiterFieldType, awaiterTemp.Type, TypeCompareKind.ConsiderEverything2))
            {
                Debug.Assert(awaiterFieldType.IsObjectType() || TypeSymbol.Equals(awaiterFieldType, awaiterTempRef.Type, TypeCompareKind.AllIgnoreOptions));
                Conversion c = F.ClassifyEmitConversion(awaiterTempRef, awaiterFieldType);
                Debug.Assert(c.IsImplicit);
                Debug.Assert(c.IsReference || c.IsIdentity);
                awaiterTempRef = F.Convert(awaiterFieldType, awaiterTempRef, c);
            }

            blockBuilder.Add(
                    F.Assignment(
                    F.Field(F.This(), awaiterField),
                    awaiterTempRef));

            blockBuilder.Add(awaiterTemp.Type.IsDynamic()
                ? GenerateAwaitOnCompletedDynamic(awaiterTemp)
                : GenerateAwaitOnCompleted(awaiterTemp.Type, awaiterTemp));

            blockBuilder.Add(
                GenerateReturn(false));

            if (F.Compilation.Options.EnableEditAndContinue)
            {
                for (int i = 0; i < debugInfo.ReservedStateMachineCount; i++)
                {
                    AddResumableState(awaitSyntax, new AwaitDebugId((byte)(debugInfo.AwaitId.RelativeStateOrdinal + 1 + i)), out _, out var dummyResumeLabel);
                    blockBuilder.Add(F.Label(dummyResumeLabel));
                }
            }

            blockBuilder.Add(
                F.Label(resumeLabel));

            blockBuilder.Add(
                    // Emit await resume point to be injected into PDB
                    F.NoOp(NoOpStatementFlavor.AwaitResumePoint));

            // $awaiterTemp = this.<>t__awaiter   or   $awaiterTemp = (AwaiterType)this.<>t__awaiter
            // $this.<>t__awaiter = null;

            BoundExpression awaiterFieldRef = F.Field(F.This(), awaiterField);

            if (!TypeSymbol.Equals(awaiterTemp.Type, awaiterField.Type, TypeCompareKind.ConsiderEverything2))
            {
                Debug.Assert(awaiterFieldRef.Type.IsObjectType() || TypeSymbol.Equals(awaiterTemp.Type, awaiterFieldRef.Type, TypeCompareKind.AllIgnoreOptions));
                Conversion c = F.ClassifyEmitConversion(awaiterFieldRef, awaiterTemp.Type);
                Debug.Assert(c.IsReference || c.IsIdentity);
                awaiterFieldRef = F.Convert(awaiterTemp.Type, awaiterFieldRef, c);
            }

            blockBuilder.Add(
                    F.Assignment(
                    F.Local(awaiterTemp),
                    awaiterFieldRef));

            blockBuilder.Add(
                F.Assignment(F.Field(F.This(), awaiterField), F.NullOrDefault(awaiterField.Type)));

            blockBuilder.Add(
                    // this.state = cachedState = NotStartedStateMachine
                    GenerateSetBothStates(StateMachineState.NotStartedOrRunningState));

            return F.Block(blockBuilder.ToImmutableAndFree());
        }

        private BoundStatement GenerateAwaitOnCompletedDynamic(LocalSymbol awaiterTemp)
        {
            //  temp $criticalNotifyCompletedTemp = $awaiterTemp as ICriticalNotifyCompletion
            //  if ($criticalNotifyCompletedTemp != null)
            //  {
            //    this.builder.AwaitUnsafeOnCompleted<ICriticalNotifyCompletion,TSM>(
            //      ref $criticalNotifyCompletedTemp,
            //      ref this)
            //  }
            //  else
            //  {
            //    temp $notifyCompletionTemp = (INotifyCompletion)$awaiterTemp
            //    this.builder.AwaitOnCompleted<INotifyCompletion,TSM>(ref $notifyCompletionTemp, ref this)
            //    free $notifyCompletionTemp
            //  }
            //  free $criticalNotifyCompletedTemp

            var criticalNotifyCompletedTemp = F.SynthesizedLocal(
                F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion),
                null);

            var notifyCompletionTemp = F.SynthesizedLocal(
                F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion),
                null);

            LocalSymbol thisTemp = (F.CurrentType.TypeKind == TypeKind.Class) ? F.SynthesizedLocal(F.CurrentType) : null;

            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            blockBuilder.Add(
                F.Assignment(
                    F.Local(criticalNotifyCompletedTemp),
                        // Use reference conversion rather than dynamic conversion:
                        F.As(F.Local(awaiterTemp), criticalNotifyCompletedTemp.Type)));

            if (thisTemp != null)
            {
                blockBuilder.Add(F.Assignment(F.Local(thisTemp), F.This()));
            }

            blockBuilder.Add(
                F.If(
                    condition: F.ObjectEqual(F.Local(criticalNotifyCompletedTemp), F.Null(criticalNotifyCompletedTemp.Type)),

                    thenClause: F.Block(
                        ImmutableArray.Create(notifyCompletionTemp),
                        F.Assignment(
                            F.Local(notifyCompletionTemp),
                                // Use reference conversion rather than dynamic conversion:
                                F.Convert(notifyCompletionTemp.Type, F.Local(awaiterTemp), Conversion.ExplicitReference)),
                        F.ExpressionStatement(
                            F.Call(
                                F.Field(F.This(), _asyncMethodBuilderField),
                                _asyncMethodBuilderMemberCollection.AwaitOnCompleted.Construct(
                                    notifyCompletionTemp.Type,
                                    F.This().Type),
                                F.Local(notifyCompletionTemp), F.This(thisTemp))),
                        F.Assignment(
                            F.Local(notifyCompletionTemp),
                            F.NullOrDefault(notifyCompletionTemp.Type))),

                    elseClauseOpt: F.Block(
                        F.ExpressionStatement(
                            F.Call(
                                F.Field(F.This(), _asyncMethodBuilderField),
                                _asyncMethodBuilderMemberCollection.AwaitUnsafeOnCompleted.Construct(
                                    criticalNotifyCompletedTemp.Type,
                                    F.This().Type),
                                F.Local(criticalNotifyCompletedTemp), F.This(thisTemp))))));

            blockBuilder.Add(
                F.Assignment(
                    F.Local(criticalNotifyCompletedTemp),
                    F.NullOrDefault(criticalNotifyCompletedTemp.Type)));

            return F.Block(
                SingletonOrPair(criticalNotifyCompletedTemp, thisTemp),
                blockBuilder.ToImmutableAndFree());
        }

        private BoundStatement GenerateAwaitOnCompleted(TypeSymbol loweredAwaiterType, LocalSymbol awaiterTemp)
        {
            // this.builder.AwaitOnCompleted<TAwaiter,TSM>(ref $awaiterTemp, ref this)
            //    or
            // this.builder.AwaitOnCompleted<TAwaiter,TSM>(ref $awaiterArrayTemp[0], ref this)

            LocalSymbol thisTemp = (F.CurrentType.TypeKind == TypeKind.Class) ? F.SynthesizedLocal(F.CurrentType) : null;

            var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            var useUnsafeOnCompleted = F.Compilation.Conversions.ClassifyImplicitConversionFromType(
                loweredAwaiterType,
                F.Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion),
                ref discardedUseSiteInfo).IsImplicit;

            var onCompleted = (useUnsafeOnCompleted ?
                _asyncMethodBuilderMemberCollection.AwaitUnsafeOnCompleted :
                _asyncMethodBuilderMemberCollection.AwaitOnCompleted).Construct(loweredAwaiterType, F.This().Type);
            if (_asyncMethodBuilderMemberCollection.CheckGenericMethodConstraints)
            {
                onCompleted.CheckConstraints(new ConstraintsHelper.CheckConstraintsArgs(F.Compilation, F.Compilation.Conversions, includeNullability: false, F.Syntax.Location, this.Diagnostics));
            }

            BoundExpression result =
                F.Call(
                    F.Field(F.This(), _asyncMethodBuilderField),
                    onCompleted,
                    F.Local(awaiterTemp), F.This(thisTemp));

            if (thisTemp != null)
            {
                result = F.Sequence(
                    ImmutableArray.Create(thisTemp),
                    ImmutableArray.Create<BoundExpression>(F.AssignmentExpression(F.Local(thisTemp), F.This())),
                    result);
            }

            return F.ExpressionStatement(result);
        }

        private static ImmutableArray<LocalSymbol> SingletonOrPair(LocalSymbol first, LocalSymbol secondOpt)
        {
            return (secondOpt == null) ? ImmutableArray.Create(first) : ImmutableArray.Create(first, secondOpt);
        }

        public sealed override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            if (node.ExpressionOpt != null)
            {
                Debug.Assert(_method.IsAsyncEffectivelyReturningGenericTask(F.Compilation));
                return F.Block(
                    F.Assignment(F.Local(_exprRetValue), (BoundExpression)Visit(node.ExpressionOpt)),
                    F.Goto(_exprReturnLabel));
            }

            return F.Goto(_exprReturnLabel);
        }
        #endregion Visitors
    }
}
