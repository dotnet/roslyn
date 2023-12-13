// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Async2Rewriter : BoundTreeRewriterWithStackGuard
    {
        private readonly SyntheticBoundNodeFactory _factory;

        private Async2Rewriter(SyntheticBoundNodeFactory factory)
        {
            _factory = factory;
        }

        public override BoundNode? VisitCall(BoundCall node)
        {
            var loweredCall = (BoundCall)base.VisitCall(node);

            var calledMethod = loweredCall.Method.GetConstructedLeastOverriddenMethod(_factory.CurrentType, requireSameReturnType: true);
            if (calledMethod.IsAsync2)
            {
                calledMethod = new AsyncThunkForAsync2Method(calledMethod);
                loweredCall = loweredCall.Update(loweredCall.ReceiverOpt, loweredCall.InitialBindingReceiverIsSubjectToCloning, calledMethod, loweredCall.Arguments);
            }

            return loweredCall;
        }

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            var loweredAwait = (BoundAwaitExpression)base.VisitAwaitExpression(node);

            if (_factory.CurrentFunction?.IsAsync2 == true)
            {
                BoundExpression arg = loweredAwait.Expression;
                if (arg is BoundCall call && call.Method is AsyncThunkForAsync2Method thunk)
                {
                    // instead of calling a thunk and awaiting, call the underlying method
                    MethodSymbol method = thunk.UnderlyingMethod;
                    return call.Update(
                        call.ReceiverOpt,
                        call.InitialBindingReceiverIsSubjectToCloning,
                        method,
                        call.Arguments,
                        call.ArgumentNamesOpt,
                        call.ArgumentRefKindsOpt,
                        call.IsDelegateCall,
                        call.Expanded,
                        call.InvokedAsExtensionMethod,
                        call.ArgsToParamsOpt,
                        call.DefaultArguments,
                        call.ResultKind,
                        loweredAwait.Type);
                }
                else
                {
                    // REWRITE: 
                    // await arg
                    //
                    // == INTO ===> 
                    //
                    // sequence
                    // {
                    //   var awaiter = arg.GetAwaiter();
                    //   if (awaiter.IsComplete())
                    //   {
                    //       UnsafeAwaitAwaiterFromRuntimeAsync(awaiter)
                    //   }
                    //   awaiter.GetResult()
                    // }

                    BoundCall getAwaiter = _factory.Call(arg, (MethodSymbol)loweredAwait.AwaitableInfo.GetAwaiter!.ExpressionSymbol!);
                    BoundLocal awaiterTmp = _factory.StoreToTemp(getAwaiter, out var awaiterTempAssignment);

                    BoundCall isCompleted = _factory.Call(awaiterTmp, loweredAwait.AwaitableInfo.IsCompleted!.GetMethod);

                    MethodSymbol helperMethod = (MethodSymbol)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__UnsafeAwaitAwaiterFromRuntimeAsync_TAwaiter)!;
                    helperMethod = helperMethod.Construct(getAwaiter.Type);
                    BoundCall helperCall = _factory.Call(null, helperMethod, awaiterTmp);

                    BoundExpression ifNotCompleteCallHelper = _factory.Conditional(isCompleted, _factory.Default(helperCall.Type), helperCall, helperCall.Type);

                    MethodSymbol getResultMethod = (MethodSymbol)loweredAwait.AwaitableInfo.GetResult!;
                    BoundCall getResult = _factory.Call(awaiterTmp, getResultMethod);

                    return _factory.Sequence(
                        ImmutableArray.Create(awaiterTmp.LocalSymbol),
                        ImmutableArray.Create(
                            awaiterTempAssignment,
                            ifNotCompleteCallHelper),
                        getResult);
                }
            }

            return loweredAwait;
        }

        internal static BoundStatement Rewrite(BoundStatement body, MethodSymbol method, TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var rewriter = new Async2Rewriter(new SyntheticBoundNodeFactory(method, body.Syntax, compilationState, diagnostics));
            return (BoundStatement)rewriter.Visit(body);
        }
    }
}
