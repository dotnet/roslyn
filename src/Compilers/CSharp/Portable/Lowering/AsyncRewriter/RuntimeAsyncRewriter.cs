// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
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

    private NamedTypeSymbol Task
    {
        get => field ??= _compilation.GetSpecialType(InternalSpecialType.System_Threading_Tasks_Task);
    } = null!;

    private NamedTypeSymbol TaskT
    {
        get => field ??= _compilation.GetSpecialType(InternalSpecialType.System_Threading_Tasks_Task_T);
    } = null!;

    private NamedTypeSymbol ValueTask
    {
        get => field ??= _compilation.GetSpecialType(InternalSpecialType.System_Threading_Tasks_ValueTask);
    } = null!;

    private NamedTypeSymbol ValueTaskT
    {
        get => field ??= _compilation.GetSpecialType(InternalSpecialType.System_Threading_Tasks_ValueTask_T);
    } = null!;

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
        var originalType = nodeType.OriginalDefinition;

        SpecialMember awaitCall;
        TypeWithAnnotations? maybeNestedType = null;

        if (ReferenceEquals(originalType, Task))
        {
            awaitCall = SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__AwaitTask;
        }
        else if (ReferenceEquals(originalType, TaskT))
        {
            awaitCall = SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__AwaitTaskT_T;
            maybeNestedType = ((NamedTypeSymbol)nodeType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
        }
        else if (ReferenceEquals(originalType, ValueTask))
        {
            awaitCall = SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__AwaitValueTask;
        }
        else if (ReferenceEquals(originalType, ValueTaskT))
        {
            awaitCall = SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__AwaitValueTaskT_T;
            maybeNestedType = ((NamedTypeSymbol)nodeType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
        }
        else
        {
            return RewriteCustomAwaiterAwait(node);
        }

        // PROTOTYPE: Make sure that we report an error in initial binding if these are missing
        var awaitMethod = (MethodSymbol?)_compilation.GetSpecialTypeMember(awaitCall);
        Debug.Assert(awaitMethod is not null);

        if (maybeNestedType is { } nestedType)
        {
            Debug.Assert(awaitMethod.TypeParameters.Length == 1);
            // PROTOTYPE: Check diagnostic
            awaitMethod = awaitMethod.Construct([nestedType]);
        }
#if DEBUG
        else
        {
            Debug.Assert(awaitMethod.TypeParameters.Length == 0);
        }
#endif

        // System.Runtime.CompilerServices.RuntimeHelpers.Await(awaitedExpression)
        return _factory.Call(receiver: null, awaitMethod, VisitExpression(node.Expression));
    }

    private BoundExpression RewriteCustomAwaiterAwait(BoundAwaitExpression node)
    {
        // await expr
        // becomes
        // var _tmp = expr.GetAwaiter();
        // if (!_tmp.IsCompleted)
        //    UnsafeAwaitAwaiterFromRuntimeAsync(_tmp) OR AwaitAwaiterFromRuntimeAsync(_tmp);
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

        // UnsafeAwaitAwaiterFromRuntimeAsync(_tmp) OR AwaitAwaiterFromRuntimeAsync(_tmp)
        var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
        var useUnsafeAwait = _factory.Compilation.Conversions.ClassifyImplicitConversionFromType(
            tmp.Type,
            _factory.Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ICriticalNotifyCompletion),
            ref discardedUseSiteInfo).IsImplicit;

        // PROTOTYPE: Make sure that we report an error in initial binding if these are missing
        var awaitMethod = (MethodSymbol?)_compilation.GetSpecialTypeMember(useUnsafeAwait
            ? SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__UnsafeAwaitAwaiterFromRuntimeAsync_TAwaiter
            : SpecialMember.System_Runtime_CompilerServices_AsyncHelpers__AwaitAwaiterFromRuntimeAsync_TAwaiter);

        Debug.Assert(awaitMethod is { Arity: 1 });

        var awaitCall = _factory.Call(
            receiver: null,
            awaitMethod.Construct(tmp.Type),
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
