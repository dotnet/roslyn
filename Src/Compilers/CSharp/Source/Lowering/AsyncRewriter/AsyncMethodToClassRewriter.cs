using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class AsyncRewriter
    {   
        sealed internal class AsyncMethodToClassRewriter : StateMachineMethodToClassRewriter
        {
            /// <summary>
            /// The method being rewritten.
            /// </summary>
            private readonly MethodSymbol method;

            /// <summary>
            /// The field of the generated async class used to store the async method builder: an instance of
            /// AsyncVoidMethodBuilder, AsyncTaskMethodBuilder, or AsyncTaskMethodBuilder&lt;T&gt; depending on the
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
            /// The field of the generated async class used in generic task returning async methods to store the value
            /// of rewritten return expressions. The return-handling code then uses SetResult on the async method builder
            /// to make the result available to the caller.
            /// </summary>
            private readonly LocalSymbol exprRetValue;

            private readonly LoweredDynamicOperationFactory dynamicFactory;

            private readonly Dictionary<TypeSymbol, FieldSymbol> awaiterFields = new Dictionary<TypeSymbol, FieldSymbol>();

            internal AsyncMethodToClassRewriter(
               MethodSymbol method,
               AsyncMethodBuilderMemberCollection asyncMethodBuilderMemberCollection,
               SyntheticBoundNodeFactory factory,
               FieldSymbol state,
               FieldSymbol builder,
               Dictionary<Symbol, CapturedSymbol> localProxies,
               DiagnosticBag diagnostics)
                : base(factory, state, localProxies, diagnostics)
            {
                this.method = method;
                this.asyncMethodBuilderMemberCollection = asyncMethodBuilderMemberCollection;
                this.asyncMethodBuilderField = builder;
                this.exprReturnLabel = factory.GenerateLabel("exprReturn");

                this.exprRetValue = method.IsGenericTaskReturningAsync()
                    ? factory.SynthesizedLocal(asyncMethodBuilderMemberCollection.ResultType, GeneratedNames.AsyncExprRetValueFieldName())
                    : null;

                this.dynamicFactory = new LoweredDynamicOperationFactory(factory);
            }

            private FieldSymbol GetAwaiterField(TypeSymbol awaiterType)
            {
                FieldSymbol result;
                if (!awaiterFields.TryGetValue(awaiterType, out result))
                {
                    result = F.SynthesizeField(awaiterType, GeneratedNames.AsyncAwaiterFieldName(CompilationState.GenerateTempNumber()));
                    awaiterFields.Add(awaiterType, result);
                }
                return result;
            }

            /// <summary>
            /// Generate the body for MoveNext()
            /// </summary>
            internal void GenerateMoveNext(BoundStatement body, MethodSymbol moveNextMethod)
            {
                F.CurrentMethod = moveNextMethod;

                int initialState;
                GeneratedLabelSymbol initialLabel;
                AddState(out initialState, out initialLabel);

                var exceptionLocal = F.SynthesizedLocal(F.WellKnownType(WellKnownType.System_Exception), GeneratedNames.AsyncExceptionFieldName());

                BoundStatement rewrittenBody = (BoundStatement)Visit(body);
                rewrittenBody = (BoundStatement)new AwaitLoweringRewriterPass1(F).Visit(rewrittenBody);
                rewrittenBody = (BoundStatement)new AwaitLoweringRewriterPass2(F, CompilationState).Visit(rewrittenBody);

                UnloweredSpillNodeVerifier.Verify(rewrittenBody);

                var bodyBuilder = ArrayBuilder<BoundStatement>.GetInstance();

                bodyBuilder.Add(
                    F.Try(
                        F.Block(
                            ReadOnlyArray<LocalSymbol>.Empty,
                            // switch (state) ...
                            Dispatch(),
                            // TODO(t-liam) - debugging support...
                            // dev11(LanguageAnalysis\Compiler\ResumableRewriter.cpp:325) does codegen here to add a
                            // debug nop and the 'NoCullNormalExit' label
                            F.Label(initialLabel),
                            // [body]
                            rewrittenBody
                        ),
                        F.CatchBlocks(
                            // TODO(t-liam) - debugging support...
                            // dev11(LanguageAnalysis\Compiler\ResumableRewriter.cpp:391)
                            // // Async void method generated catch handlers have their IL start offset
                            // // recorded to the PDB for special exception handling in the debugger.
                            // // Mark the EXPRHANDLER as such so we know to record its offset later.
                            F.Catch(
                                F.WellKnownType(WellKnownType.System_Exception),
                                exceptionLocal,
                                F.Block(
                                    // state = finishedState
                                    F.Assignment(F.Field(F.This(), state), F.Literal(StateMachineStates.FinishedStateMachine)),
                                    // builder.SetException(ex)
                                    F.ExpressionStatement(
                                        F.Call(
                                            F.Field(F.This(), asyncMethodBuilderField),
                                            asyncMethodBuilderMemberCollection.SetException,
                                            ReadOnlyArray<BoundExpression>.CreateFrom(F.Local(exceptionLocal)))),
                                    F.Return()
                                )
                            )
                        )
                    ));

                // ReturnLabel (for the rewritten return expressions in the user's method body)
                bodyBuilder.Add(F.Label(exprReturnLabel));

                // state = finishedState
                bodyBuilder.Add(F.Assignment(F.Field(F.This(), state), F.Literal(StateMachineStates.FinishedStateMachine)));

                // builder.SetResult([RetVal])
                bodyBuilder.Add(
                    F.ExpressionStatement(
                        F.Call(
                            F.Field(F.This(), asyncMethodBuilderField),
                            asyncMethodBuilderMemberCollection.SetResult,
                            method.IsGenericTaskReturningAsync()
                                ? ReadOnlyArray<BoundExpression>.CreateFrom(F.Local(exprRetValue))
                                : ReadOnlyArray<BoundExpression>.Empty)));

                bodyBuilder.Add(F.Return());

                var newBody = bodyBuilder.ToReadOnlyAndFree();

                F.CloseMethod(
                    F.SequencePoint(
                        body.Syntax,
                        F.Block(
                            exprRetValue != null ? ReadOnlyArray<LocalSymbol>.CreateFrom(exprRetValue) : ReadOnlyArray<LocalSymbol>.Empty,
                            newBody)));
            }

            protected override string ResumeLabelName { get { return "asyncLabel"; } }

            protected override BoundStatement GenerateReturn(bool finished)
            {
                return F.Return();
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
                    var awaitExpression = VisitAwaitExpression((BoundAwaitExpression)node.Expression, resultsDiscarded: true);
                    return node.Update(awaitExpression);
                }

                return base.VisitExpressionStatement(node);
            }

            public override BoundNode VisitAwaitExpression(BoundAwaitExpression node)
            {
                return VisitAwaitExpression(node, resultsDiscarded: false);
            }

            private BoundExpression VisitAwaitExpression(BoundAwaitExpression node, bool resultsDiscarded)
            {
                var expression = (BoundExpression)Visit(node.Expression);

                MethodSymbol getAwaiter = VisitMethodSymbol(node.GetAwaiter);
                MethodSymbol getResult = VisitMethodSymbol(node.GetResult);
                MethodSymbol isCompletedMethod = (node.IsCompleted != null) ? VisitMethodSymbol(node.IsCompleted.GetMethod) : null;
                TypeSymbol type = VisitType(node.Type);

                LocalSymbol awaiterTemp;
                if (getResult == null)
                {
                    awaiterTemp = F.SynthesizedLocal(DynamicTypeSymbol.Instance, name: null);
                }
                else if (type.IsDynamic())
                {
                    var awaiterType = ((NamedTypeSymbol)getAwaiter.ReturnType).OriginalNamedTypeDefinition.Construct(F.SpecialType(SpecialType.System_Object));
                    awaiterTemp = F.SynthesizedLocal(awaiterType, null);
                    getResult = ((MethodSymbol)getResult.OriginalDefinition).AsMember(awaiterType);
                    isCompletedMethod = ((MethodSymbol)isCompletedMethod.OriginalDefinition).AsMember(awaiterType);
                }
                else
                {
                    awaiterTemp = F.SynthesizedLocal(getAwaiter.ReturnType, name: null);
                }

                var awaitIfIncomplete = F.Block(
                    // temp $awaiterTemp = <expr>.GetAwaiter();
                    F.Assignment(
                        F.Local(awaiterTemp),
                        MakeCallMaybeDynamic(expression, getAwaiter, "GetAwaiter")),

                    // if(!($awaiterTemp.IsCompleted)) { ... }
                    F.If(
                        condition: F.Not(GenerateGetIsCompleted(awaiterTemp, isCompletedMethod)),
                        thenClause: GenerateAwaitForIncompleteTask(awaiterTemp)));

                BoundExpression getResultCall = MakeCallMaybeDynamic(
                    F.Local(awaiterTemp),
                    getResult,
                    "GetResult",
                    resultsDiscarded: resultsDiscarded);

                var nullAwaiter = F.AssignmentExpression(F.Local(awaiterTemp), F.NullOrDefault(awaiterTemp.Type));
                
                BoundExpression onAwaitFinished;
                if (!resultsDiscarded && type.SpecialType != SpecialType.System_Void)
                {
                    // $resultTemp = $awaiterTemp.GetResult();
                    // $awaiterTemp = null;
                    // $resultTemp
                    LocalSymbol resultTemp = F.SynthesizedLocal(type, null);
                    onAwaitFinished = F.Sequence(
                        resultTemp,
                        F.AssignmentExpression(F.Local(resultTemp), getResultCall),
                        nullAwaiter,
                        F.Local(resultTemp));
                }
                else
                {
                    // $awaiterTemp.GetResult();
                    // $awaiterTemp = null;
                    onAwaitFinished = F.Sequence(
                        ReadOnlyArray<LocalSymbol>.Empty,
                        getResultCall,
                        nullAwaiter);
                }

                return F.SpillSequence(
                    ReadOnlyArray<LocalSymbol>.CreateFrom(awaiterTemp),
                    ReadOnlyArray<BoundSpillTemp>.Empty,
                    ReadOnlyArray<FieldSymbol>.Empty,
                    ReadOnlyArray<BoundStatement>.CreateFrom(awaitIfIncomplete),
                    onAwaitFinished);
            }

            private BoundExpression MakeCallMaybeDynamic(
                BoundExpression receiver,
                MethodSymbol methodSymbol = null,
                String methodName = null,
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
                else
                {
                    // dynamic:
                    Debug.Assert(methodName != null);
                    return dynamicFactory.MakeDynamicMemberInvocation(
                        methodName,
                        receiver,
                        typeArguments: ReadOnlyArray<TypeSymbol>.Empty,
                        loweredArguments: ReadOnlyArray<BoundExpression>.Empty,
                        argumentNames: ReadOnlyArray<String>.Empty,
                        refKinds: ReadOnlyArray<RefKind>.Empty,
                        hasImplicitReceiver: false,
                        resultDiscarded: resultsDiscarded).ToExpression();
                }
            }

            private BoundExpression GenerateGetIsCompleted(LocalSymbol awaiterTemp, MethodSymbol getIsCompletedMethod)
            {
                if (awaiterTemp.Type.IsDynamic())
                {
                    return dynamicFactory.MakeDynamicConversion(
                        dynamicFactory.MakeDynamicGetMember(
                            F.Local(awaiterTemp),
                            "IsCompleted",
                            false).ToExpression(),
                        isExplicit: true,
                        isArrayIndex: false,
                        isChecked: false,
                        resultType: F.SpecialType(SpecialType.System_Boolean)).ToExpression();
                }
                else
                {
                    return F.Call(F.Local(awaiterTemp), getIsCompletedMethod);
                }
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
                    // state = stateForLabel
                    F.Assignment(F.Field(F.This(), state), F.Literal(stateNumber)));

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
                    F.Return());

                blockBuilder.Add(
                    F.Label(resumeLabel));

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
                    // state = NotStartedStateMachine
                    F.Assignment(F.Field(F.This(), state), F.Literal(StateMachineStates.NotStartedStateMachine)));

                return F.Block(blockBuilder.ToReadOnlyAndFree());
            }

            private BoundStatement GenerateAwaitOnCompletedDynamic(LocalSymbol awaiterTemp)
            {
                //  temp $criticalNotifyCompletedTemp = $awaiterTemp as ICriticalNotifyCompletion
                //  if (
                //      $criticalNotifyCompletedTemp != null
                //     ) {
                //    this.builder.AwaitUnsafeOnCompleted<
                //         ICriticalNotifyCompletion,TSM>(ref $criticalNotifyCompletedTemp, ref this)
                //  } else {
                //    temp $notifyCompletionTemp = (INotifyCompletion)$awaiterTemp
                //       this.builder.AwaitOnCompleted<INotifyCompletion,TSM>(
                //         ref $notifyCompletionTemp, ref this)
                //       free $notifyCompletionTemp
                //  }
                //  free $criticalNotifyCompletedTemp

                var criticalNotifyCompletedTemp = F.SynthesizedLocal(
                    F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion),
                    null);

                var notifyCompletionTemp = F.SynthesizedLocal(
                    F.WellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion),
                    null);

                return F.Block(
                    ReadOnlyArray<LocalSymbol>.CreateFrom(criticalNotifyCompletedTemp),

                    F.Assignment(
                        F.Local(criticalNotifyCompletedTemp),
                        F.As(F.Local(awaiterTemp), criticalNotifyCompletedTemp.Type)),

                    F.If(
                        condition: F.ObjectEqual(F.Local(criticalNotifyCompletedTemp), F.Null(criticalNotifyCompletedTemp.Type)),

                        thenClause: F.Block(
                            ReadOnlyArray<LocalSymbol>.CreateFrom(notifyCompletionTemp),
                            F.Assignment(
                                F.Local(notifyCompletionTemp),
                                F.Convert(notifyCompletionTemp.Type, F.Local(awaiterTemp))),
                            F.ExpressionStatement(
                                F.Call(
                                    F.Field(F.This(), asyncMethodBuilderField),
                                    asyncMethodBuilderMemberCollection.AwaitOnCompleted.Construct(
                                        notifyCompletionTemp.Type,
                                        F.This().Type),
                                    new BoundExpression[] {F.Local(notifyCompletionTemp), F.This()})),
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
                                    new BoundExpression[] {F.Local(criticalNotifyCompletedTemp), F.This()})))),

                    F.Assignment(
                        F.Local(criticalNotifyCompletedTemp),
                        F.NullOrDefault(criticalNotifyCompletedTemp.Type)));
            }

            private BoundExpressionStatement GenerateAwaitOnCompleted(TypeSymbol loweredAwaiterType, LocalSymbol awaiterTemp)
            {
                // this.builder.AwaitOnCompleted<TAwaiter,TSM>(ref $awaiterTemp, ref this)
                //    or
                // this.builder.AwaitOnCompleted<TAwaiter,TSM>(ref $awaiterArrayTemp[0], ref this)

                return F.ExpressionStatement(
                    F.Call(
                        F.Field(F.This(), asyncMethodBuilderField),
                        (loweredAwaiterType.AllInterfaces.Contains(
                            F.Compilation.GetWellKnownType(
                                WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion))
                             ? asyncMethodBuilderMemberCollection.AwaitUnsafeOnCompleted
                             : asyncMethodBuilderMemberCollection.AwaitOnCompleted).Construct(loweredAwaiterType, F.This().Type),
                        new BoundExpression[] { F.Local(awaiterTemp), F.This() }));
            }

            public override BoundNode VisitReturnStatement(BoundReturnStatement node)
            {
                if (node.ExpressionOpt != null)
                {
                    Debug.Assert(method.IsGenericTaskReturningAsync());
                    return F.Block(
                        F.Assignment(F.Local(exprRetValue), (BoundExpression)Visit(node.ExpressionOpt)),
                        F.Goto(exprReturnLabel));
                }

                return F.Goto(exprReturnLabel);
            }

            #endregion Visitors
        }
    }
}