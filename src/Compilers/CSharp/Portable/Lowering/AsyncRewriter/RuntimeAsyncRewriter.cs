// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class RuntimeAsyncRewriter : BoundTreeRewriterWithStackGuard
{
    public static BoundStatement Rewrite(
        BoundStatement node,
        MethodSymbol method,
        TypeCompilationState compilationState,
        BindingDiagnosticBag diagnostics)
    {
        if (!method.IsAsync)
        {
            return node;
        }

        // PROTOTYPE: struct lifting
        var rewriter = new RuntimeAsyncRewriter(compilationState.Compilation, new SyntheticBoundNodeFactory(method, node.Syntax, compilationState, diagnostics));
        var result = (BoundStatement)rewriter.Visit(node);
        return SpillSequenceSpiller.Rewrite(result, method, compilationState, diagnostics);
    }

    private readonly CSharpCompilation _compilation;
    private readonly SyntheticBoundNodeFactory _factory;
    private readonly Dictionary<BoundAwaitableValuePlaceholder, BoundExpression> _placeholderMap;

    private RuntimeAsyncRewriter(CSharpCompilation compilation, SyntheticBoundNodeFactory factory)
    {
        _compilation = compilation;
        _factory = factory;
        _placeholderMap = [];
    }

    [return: NotNullIfNotNull(nameof(node))]
    public BoundExpression? VisitExpression(BoundExpression? node)
    {
        var result = Visit(node);
        return (BoundExpression?)result;
    }

    public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
    {
        var nodeType = node.Expression.Type;
        Debug.Assert(nodeType is not null);

        var awaitableInfo = node.AwaitableInfo;
        var runtimeAsyncAwaitMethod = awaitableInfo.RuntimeAsyncAwaitMethod;
        Debug.Assert(runtimeAsyncAwaitMethod is not null);
        Debug.Assert(ReferenceEquals(
            runtimeAsyncAwaitMethod.ContainingType.OriginalDefinition,
            _factory.Compilation.GetSpecialType(InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers)));
        Debug.Assert(runtimeAsyncAwaitMethod.Name is "Await" or "UnsafeAwaitAwaiter" or "AwaitAwaiter");

        if (runtimeAsyncAwaitMethod.Name == "Await")
        {
            // This is the direct await case, with no need for the full pattern.
            // System.Runtime.CompilerServices.RuntimeHelpers.Await(awaitedExpression)
            return _factory.Call(receiver: null, runtimeAsyncAwaitMethod, VisitExpression(node.Expression));
        }
        else
        {
            return RewriteCustomAwaiterAwait(node);
        }
    }

    private BoundExpression RewriteCustomAwaiterAwait(BoundAwaitExpression node)
    {
        // await expr
        // becomes
        // var _tmp = expr.GetAwaiter();
        // if (!_tmp.IsCompleted)
        //    UnsafeAwaitAwaiter(_tmp) OR AwaitAwaiter(_tmp);
        // _tmp.GetResult()

        // PROTOTYPE: await dynamic will need runtime checks, see AsyncMethodToStateMachine.GenerateAwaitOnCompletedDynamic

        var expr = VisitExpression(node.Expression);

        var awaitableInfo = node.AwaitableInfo;
        var awaitablePlaceholder = awaitableInfo.AwaitableInstancePlaceholder;
        if (awaitablePlaceholder is not null)
        {
            _placeholderMap.Add(awaitablePlaceholder, expr);
        }

        // expr.GetAwaiter()
        var getAwaiter = VisitExpression(awaitableInfo.GetAwaiter);
        Debug.Assert(getAwaiter is not null);

        if (awaitablePlaceholder is not null)
        {
            _placeholderMap.Remove(awaitablePlaceholder);
        }

        // var _tmp = expr.GetAwaiter();
        var tmp = _factory.StoreToTemp(getAwaiter, out BoundAssignmentOperator store, kind: SynthesizedLocalKind.Awaiter);

        // _tmp.IsCompleted
        Debug.Assert(awaitableInfo.IsCompleted is not null);
        var isCompletedMethod = awaitableInfo.IsCompleted.GetMethod;
        Debug.Assert(isCompletedMethod is not null);
        var isCompletedCall = _factory.Call(tmp, isCompletedMethod);

        // UnsafeAwaitAwaiter(_tmp) OR AwaitAwaiter(_tmp)
        Debug.Assert(awaitableInfo.RuntimeAsyncAwaitMethod is not null);
        var awaitCall = _factory.Call(
            receiver: null,
            awaitableInfo.RuntimeAsyncAwaitMethod,
            tmp);

        // if (!_tmp.IsCompleted) awaitCall
        var ifNotCompleted = _factory.If(_factory.Not(isCompletedCall), _factory.ExpressionStatement(awaitCall));

        // _tmp.GetResult()
        var getResultMethod = awaitableInfo.GetResult;
        Debug.Assert(getResultMethod is not null);
        var getResultCall = _factory.Call(tmp, getResultMethod);

        // final sequence
        return _factory.SpillSequence(
            locals: [tmp.LocalSymbol],
            sideEffects: [_factory.ExpressionStatement(store), ifNotCompleted],
            result: getResultCall);
    }

    public override BoundNode VisitAwaitableValuePlaceholder(BoundAwaitableValuePlaceholder node)
    {
        return _placeholderMap[node];
    }
}
