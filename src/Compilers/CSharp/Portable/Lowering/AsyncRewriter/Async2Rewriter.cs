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

        public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
        {
            var loweredAwait = (BoundAwaitExpression)base.VisitAwaitExpression(node)!;

            if (_factory.CurrentFunction?.IsAsync2 == true)
            {
                BoundExpression arg = loweredAwait.Expression;

                // TODO: VS does this need to be a call?
                if (arg is BoundCall call1)
                {
                    MethodSymbol? awaitHelper = null;
                    if (call1.Method.ReturnType.IsGenericNonCustomTaskType(_factory.Compilation))
                    {
                        awaitHelper = (MethodSymbol?)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__Await_Task_T);
                        if (awaitHelper != null)
                        {
                            TypeSymbol elementType = ((NamedTypeSymbol)call1.Method.ReturnType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                            awaitHelper = awaitHelper.Construct(elementType);
                        }
                    }
                    else if (call1.Method.ReturnType.IsNonGenericNonCustomTaskType(_factory.Compilation))
                    {
                        awaitHelper = (MethodSymbol?)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__Await_Task);
                    }
                    else if (call1.Method.ReturnType.IsGenericNonCustomValueTaskType(_factory.Compilation))
                    {
                        awaitHelper = (MethodSymbol?)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__Await_ValueTask_T);
                        if (awaitHelper != null)
                        {
                            TypeSymbol elementType = ((NamedTypeSymbol)call1.Method.ReturnType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                            awaitHelper = awaitHelper.Construct(elementType);
                        }
                    }
                    else if (call1.Method.ReturnType.IsNonGenericNonCustomValueTaskType(_factory.Compilation))
                    {
                        awaitHelper = (MethodSymbol?)_factory.Compilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__Await_ValueTask);
                    }

                    if (awaitHelper != null)
                    {
                        // REWRITE: 
                        // await arg
                        // == INTO ===> 
                        // Await(arg)
                        return _factory.Call(
                            null,
                            awaitHelper,
                            arg);
                    }
                }

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

            return loweredAwait;
        }

        internal static BoundStatement Rewrite(BoundStatement body, MethodSymbol method, TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var rewriter = new Async2Rewriter(new SyntheticBoundNodeFactory(method, body.Syntax, compilationState, diagnostics));
            return (BoundStatement)rewriter.Visit(body);
        }
    }
}
