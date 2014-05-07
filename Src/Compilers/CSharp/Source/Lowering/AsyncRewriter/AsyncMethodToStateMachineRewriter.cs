// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class AsyncMethodToStateMachineRewriter : MethodToStateMachineRewriter
    {
        /// <summary>
        /// The method being rewritten.
        /// </summary>
        private readonly MethodSymbol method;

        /// <summary>
        /// The field of the generated async class used to store the async method builder: an instance of
        /// <see cref="AsyncVoidMethodBuilder"/>, <see cref="AsyncTaskMethodBuilder"/>, or <see cref="AsyncTaskMethodBuilder{TResult}"/> depending on the
        /// return type of the async method.
        /// </summary>
        private readonly FieldSymbol asyncMethodBuilderField;

        /// <summary>
        /// A collection of well-known members for the current async method builder.
        /// </summary>
        private readonly AsyncMethodBuilderMemberCollection asyncMethodBuilderMemberCollection;

        /// <summary>
        /// The exprReturnLabel is used to label the return handling code at the end of the async state-machine
        /// method. Return expressions are rewritten as unconditional branches to exprReturnLabel.
        /// </summary>
        private readonly LabelSymbol exprReturnLabel;

        /// <summary>
        /// The label containing a return from the method when the async method has not completed.
        /// </summary>
        private readonly LabelSymbol exitLabel;

        /// <summary>
        /// The field of the generated async class used in generic task returning async methods to store the value
        /// of rewritten return expressions. The return-handling code then uses <c>SetResult</c> on the async method builder
        /// to make the result available to the caller.
        /// </summary>
        private readonly LocalSymbol exprRetValue;

        private readonly LoweredDynamicOperationFactory dynamicFactory;

        private readonly Dictionary<TypeSymbol, FieldSymbol> awaiterFields = new Dictionary<TypeSymbol, FieldSymbol>();

        internal AsyncMethodToStateMachineRewriter(
            MethodSymbol method,
            AsyncMethodBuilderMemberCollection asyncMethodBuilderMemberCollection,
            SyntheticBoundNodeFactory F,
            FieldSymbol state,
            FieldSymbol builder,
            HashSet<Symbol> variablesCaptured,
            Dictionary<Symbol, CapturedSymbolReplacement> initialProxies,
            DiagnosticBag diagnostics,
            bool generateDebugInfo)
            : base(F, method, state, variablesCaptured, initialProxies, diagnostics,
                   useFinalizerBookkeeping: false,
                   generateDebugInfo: generateDebugInfo)
        {
            this.method = method;
            this.asyncMethodBuilderMemberCollection = asyncMethodBuilderMemberCollection;
            this.asyncMethodBuilderField = builder;
            this.exprReturnLabel = F.GenerateLabel("exprReturn");
            this.exitLabel = F.GenerateLabel("exitLabel");

            this.exprRetValue = method.IsGenericTaskReturningAsync(F.Compilation)
                ? F.SynthesizedLocal(asyncMethodBuilderMemberCollection.ResultType, GeneratedNames.AsyncExprRetValueFieldName())
                : null;

            this.dynamicFactory = new LoweredDynamicOperationFactory(F);
        }

        private FieldSymbol GetAwaiterField(TypeSymbol awaiterType)
        {
            FieldSymbol result;
            if (!awaiterFields.TryGetValue(awaiterType, out result))
            {
                result = F.StateMachineField(awaiterType, GeneratedNames.AsyncAwaiterFieldName(CompilationState.GenerateTempNumber()), isPublic: true);
                awaiterFields.Add(awaiterType, result);
            }

            return result;
        }

        /// <summary>
        /// Generate the body for <c>MoveNext()</c>.
        /// </summary>
        internal void GenerateMoveNext(BoundStatement body, MethodSymbol moveNextMethod)
        {
            F.CurrentMethod = moveNextMethod;

            int initialState;
            GeneratedLabelSymbol initialLabel;
            AddState(out initialState, out initialLabel);

            var exceptionLocal = F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Exception), GeneratedNames.AsyncExceptionFieldName());

            var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            bodyBuilder.Add(
                F.HiddenSequencePoint());
            bodyBuilder.Add(
                F.Assignment(F.Local(cachedState), F.Field(F.This(), stateField)));

            BoundStatement rewrittenBody = (BoundStatement)Visit(body);

            bodyBuilder.Add(
                F.Try(
                    F.Block(
                        ImmutableArray<LocalSymbol>.Empty,
                            // switch (state) ...
                            F.HiddenSequencePoint(),
                        Dispatch(),
                        F.Label(initialLabel),
                            // [body]
                            rewrittenBody
                    ),
                    F.CatchBlocks(
                        F.Catch(
                            exceptionLocal,
                            F.Block(
                                F.NoOp(method.ReturnsVoid ? NoOpStatementFlavor.AsyncMethodCatchHandler : NoOpStatementFlavor.Default),
                                F.HiddenSequencePoint(),
                                    // this.state = finishedState
                                    F.Assignment(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine)),
                                    // builder.SetException(ex)
                                    F.ExpressionStatement(
                                    F.Call(
                                        F.Field(F.This(), asyncMethodBuilderField),
                                        asyncMethodBuilderMemberCollection.SetException,
                                        F.Local(exceptionLocal))),
                                GenerateReturn(false)
                            )
                        )
                    )
                ));

            // ReturnLabel (for the rewritten return expressions in the user's method body)
            bodyBuilder.Add(F.Label(exprReturnLabel));

            // this.state = finishedState
            var stateDone = F.Assignment(F.Field(F.This(), stateField), F.Literal(StateMachineStates.FinishedStateMachine));
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

            // builder.SetResult([RetVal])
            bodyBuilder.Add(
                F.ExpressionStatement(
                    F.Call(
                        F.Field(F.This(), asyncMethodBuilderField),
                        asyncMethodBuilderMemberCollection.SetResult,
                        method.IsGenericTaskReturningAsync(F.Compilation)
                            ? ImmutableArray.Create<BoundExpression>(F.Local(exprRetValue))
                            : ImmutableArray<BoundExpression>.Empty)));

            // this code is hidden behind a hidden sequence point.
            bodyBuilder.Add(F.Label(this.exitLabel));
            bodyBuilder.Add(F.Return());

            var newBody = bodyBuilder.ToImmutableAndFree();

            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            locals.Add(cachedState);
            if ((object)exprRetValue != null) locals.Add(exprRetValue);

            F.CloseMethod(
                F.SequencePoint(
                    body.Syntax,
                    F.Block(
                        locals.ToImmutableAndFree(),
                        newBody)));
        }

        protected override BoundStatement GenerateReturn(bool finished)
        {
            return F.Goto(this.exitLabel);
        }

        #region Visitors

        private enum AwaitableDynamism
        {
            None,
            DynamicTask,
            FullDynamic
        }

        public override BoundNode VisitExpressionStatement(BoundExpressionStatement node)
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
            return (expr != null) ? node.Update(expr) : (BoundStatement)F.Block();
        }

        public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
        {
            // await expressions must, by now, have been moved to the top level.
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitBadExpression(BoundBadExpression node)
        {
            // Cannot recurse into BadExpression
            return node;
        }

        private BoundBlock VisitAwaitExpression(BoundAwaitExpression node, BoundExpression resultPlace)
        {
            var expression = (BoundExpression)Visit(node.Expression);
            resultPlace = (BoundExpression)Visit(resultPlace);
            MethodSymbol getAwaiter = VisitMethodSymbol(node.GetAwaiter);
            MethodSymbol getResult = VisitMethodSymbol(node.GetResult);
            MethodSymbol isCompletedMethod = ((object)node.IsCompleted != null) ? VisitMethodSymbol(node.IsCompleted.GetMethod) : null;
            TypeSymbol type = VisitType(node.Type);

            LocalSymbol awaiterTemp;
            if ((object)getResult == null)
            {
                awaiterTemp = F.SynthesizedLocal(DynamicTypeSymbol.Instance);
            }
            else if (type.IsDynamic())
            {
                var awaiterType = ((NamedTypeSymbol)getAwaiter.ReturnType).OriginalDefinition.Construct(F.SpecialType(SpecialType.System_Object));
                awaiterTemp = F.SynthesizedLocal(awaiterType);
                getResult = getResult.OriginalDefinition.AsMember(awaiterType);
                isCompletedMethod = isCompletedMethod.OriginalDefinition.AsMember(awaiterType);
            }
            else
            {
                awaiterTemp = F.SynthesizedLocal(getAwaiter.ReturnType);
            }

            var awaitIfIncomplete = F.Block(
                    // temp $awaiterTemp = <expr>.GetAwaiter();
                    F.Assignment(
                    F.Local(awaiterTemp),
                    MakeCallMaybeDynamic(expression, getAwaiter, WellKnownMemberNames.GetAwaiter)),

                    // if(!($awaiterTemp.IsCompleted)) { ... }
                    F.If(
                    condition: F.Not(GenerateGetIsCompleted(awaiterTemp, isCompletedMethod)),
                    thenClause: GenerateAwaitForIncompleteTask(awaiterTemp)));

            BoundExpression getResultCall = MakeCallMaybeDynamic(
                F.Local(awaiterTemp),
                getResult,
                WellKnownMemberNames.GetResult,
                resultsDiscarded: resultPlace == null);

            var nullAwaiter = F.AssignmentExpression(F.Local(awaiterTemp), F.NullOrDefault(awaiterTemp.Type));
            if (resultPlace != null && type.SpecialType != SpecialType.System_Void)
            {
                // $resultTemp = $awaiterTemp.GetResult();
                // $awaiterTemp = null;
                // $resultTemp
                LocalSymbol resultTemp = F.SynthesizedLocal(type);
                return F.Block(
                ImmutableArray.Create(awaiterTemp, resultTemp),
                    awaitIfIncomplete,
                    F.Assignment(F.Local(resultTemp), getResultCall),
                    F.ExpressionStatement(nullAwaiter),
                    F.Assignment(resultPlace, F.Local(resultTemp)));
            }
            else
            {
                // $awaiterTemp.GetResult();
                // $awaiterTemp = null;
                return F.Block(
                ImmutableArray.Create(awaiterTemp),
                    awaitIfIncomplete,
                    F.ExpressionStatement(getResultCall),
                    F.ExpressionStatement(nullAwaiter));
            }
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
            return dynamicFactory.MakeDynamicMemberInvocation(
                methodName,
                receiver,
                typeArguments: ImmutableArray<TypeSymbol>.Empty,
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
                return dynamicFactory.MakeDynamicConversion(
                    dynamicFactory.MakeDynamicGetMember(
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

        private BoundBlock GenerateAwaitForIncompleteTask(LocalSymbol awaiterTemp)
        {
            int stateNumber;
            GeneratedLabelSymbol resumeLabel;
            AddState(out stateNumber, out resumeLabel);

            TypeSymbol awaiterFieldType = awaiterTemp.Type.IsVerifierReference()
                ? F.SpecialType(SpecialType.System_Object)
                : awaiterTemp.Type;

            FieldSymbol awaiterField = GetAwaiterField(awaiterFieldType);

            var blockBuilder = ArrayBuilder<BoundStatement>.GetInstance();

            blockBuilder.Add(
                    // this.state = cachedState = stateForLabel
                    F.Assignment(F.Field(F.This(), stateField), F.AssignmentExpression(F.Local(cachedState), F.Literal(stateNumber))));

            blockBuilder.Add(
                    // Emit await yield point to be injected into PDB
                    F.NoOp(NoOpStatementFlavor.AwaitYieldPoint));

            blockBuilder.Add(
                    // this.<>t__awaiter = $awaiterTemp
                    F.Assignment(
                    F.Field(F.This(), awaiterField),
                    (awaiterField.Type == awaiterTemp.Type)
                        ? F.Local(awaiterTemp)
                        : F.Convert(awaiterFieldType, F.Local(awaiterTemp))));

            blockBuilder.Add(awaiterTemp.Type.IsDynamic()
                ? GenerateAwaitOnCompletedDynamic(awaiterTemp)
                : GenerateAwaitOnCompleted(awaiterTemp.Type, awaiterTemp));

            blockBuilder.Add(
                GenerateReturn(false));

            blockBuilder.Add(
                F.Label(resumeLabel));

            blockBuilder.Add(
                    // Emit await resume point to be injected into PDB
                    F.NoOp(NoOpStatementFlavor.AwaitResumePoint));

            blockBuilder.Add(
                    // $awaiterTemp = this.<>t__awaiter   or   $awaiterTemp = (AwaiterType)this.<>t__awaiter
                    // $this.<>t__awaiter = null;
                    F.Assignment(
                    F.Local(awaiterTemp),
                    awaiterTemp.Type == awaiterField.Type
                        ? F.Field(F.This(), awaiterField)
                        : F.Convert(awaiterTemp.Type, F.Field(F.This(), awaiterField))));

            blockBuilder.Add(
                F.Assignment(F.Field(F.This(), awaiterField), F.NullOrDefault(awaiterField.Type)));

            blockBuilder.Add(
                    // this.state = cachedState = NotStartedStateMachine
                    F.Assignment(F.Field(F.This(), stateField), F.AssignmentExpression(F.Local(cachedState), F.Literal(StateMachineStates.NotStartedStateMachine))));

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

            LocalSymbol thisTemp = (F.CurrentClass.TypeKind == TypeKind.Class) ? F.SynthesizedLocal(F.CurrentClass) : null;

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
                                F.Convert(notifyCompletionTemp.Type, F.Local(awaiterTemp), ConversionKind.ExplicitReference)),
                        F.ExpressionStatement(
                            F.Call(
                                F.Field(F.This(), asyncMethodBuilderField),
                                asyncMethodBuilderMemberCollection.AwaitOnCompleted.Construct(
                                    notifyCompletionTemp.Type,
                                    F.This().Type),
                                F.Local(notifyCompletionTemp), F.This(thisTemp))),
                        F.Assignment(
                            F.Local(notifyCompletionTemp),
                            F.NullOrDefault(notifyCompletionTemp.Type))),

                    elseClause: F.Block(
                        F.ExpressionStatement(
                            F.Call(
                                F.Field(F.This(), asyncMethodBuilderField),
                                asyncMethodBuilderMemberCollection.AwaitUnsafeOnCompleted.Construct(
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

            LocalSymbol thisTemp = (F.CurrentClass.TypeKind == TypeKind.Class) ? F.SynthesizedLocal(F.CurrentClass) : null;

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            var useUnsafeOnCompleted = F.Compilation.Conversions.ClassifyImplicitConversion(
                loweredAwaiterType,
                F.Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion),
                ref useSiteDiagnostics).IsImplicit;

            var onCompleted = (useUnsafeOnCompleted ?
                asyncMethodBuilderMemberCollection.AwaitUnsafeOnCompleted :
                asyncMethodBuilderMemberCollection.AwaitOnCompleted).Construct(loweredAwaiterType, F.This().Type);

            BoundExpression result = 
                F.Call(
                    F.Field(F.This(), asyncMethodBuilderField),
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

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            if (node.ExpressionOpt != null)
            {
                Debug.Assert(method.IsGenericTaskReturningAsync(F.Compilation));
                return F.Block(
                    F.Assignment(F.Local(exprRetValue), (BoundExpression)Visit(node.ExpressionOpt)),
                    F.Goto(exprReturnLabel));
            }

            return F.Goto(exprReturnLabel);
        }

        #endregion Visitors
    }
}
