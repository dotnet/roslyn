// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class RuntimeAsyncRewriter : BoundTreeRewriterWithStackGuard
{
    public static BoundStatement Rewrite(
        BoundStatement node,
        MethodSymbol method,
        TypeCompilationState compilationState,
        BindingDiagnosticBag diagnostics)
    {
        Debug.Assert(method.IsAsync);

        var variablesToHoist = IteratorAndAsyncCaptureWalker.Analyze(compilationState.Compilation, method, node, isRuntimeAsync: true, diagnostics.DiagnosticBag);
        var hoistedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
        var factory = new SyntheticBoundNodeFactory(method, node.Syntax, compilationState, diagnostics);
        var rewriter = new RuntimeAsyncRewriter(factory, variablesToHoist, hoistedLocals);
        var thisStore = hoistThisIfNeeded(rewriter);
        var result = (BoundStatement)rewriter.Visit(node);

        if (thisStore is not null)
        {
            result = factory.Block(hoistedLocals.ToImmutableAndFree(),
                factory.HiddenSequencePoint(),
                factory.ExpressionStatement(thisStore),
                result);
        }
        else if (hoistedLocals.Count > 0)
        {
            result = factory.Block(hoistedLocals.ToImmutableAndFree(), result);
        }
        else
        {
            hoistedLocals.Free();
        }

        return SpillSequenceSpiller.Rewrite(result, method, compilationState, diagnostics);

        static BoundAssignmentOperator? hoistThisIfNeeded(RuntimeAsyncRewriter rewriter)
        {
            Debug.Assert(rewriter._factory.CurrentFunction is not null);
            var thisParameter = rewriter._factory.CurrentFunction.ThisParameter;
            if (thisParameter is { Type.IsValueType: true, RefKind: not RefKind.None })
            {
                // This is a struct or a type parameter. We need to replace it with a hoisted local to preserve behavior from
                // compiler-generated state machines; `this` is a ref, but results are not observable outside of the method.
                // We do this regardless of whether `this` is captured to a ref local, because any usage of `ldarg.0` in these
                // scenarios is illegal after the first await. We could be more precise and only do this if `this` is actually
                // used after the first await, but at the moment we don't feel that is worth the complexity.
                var hoistedThis = rewriter._factory.StoreToTemp(rewriter._factory.This(), out BoundAssignmentOperator store, kind: SynthesizedLocalKind.AwaitByRefSpill);
                rewriter._hoistedLocals.Add(hoistedThis.LocalSymbol);
                rewriter._proxies.Add(thisParameter, new CapturedToExpressionSymbolReplacement<ParameterSymbol>(hoistedThis, hoistedSymbols: [], isReusable: true));
                return store;
            }

            return null;
        }
    }

    /// <summary>
    /// When lowering an async-iterator method with runtime async codegen, we first produce a state machine MoveNextAsync method,
    /// which deals with hoisting of variables and ref locals, but leaves await lowering to a subsequent phase.
    /// This method performs the actual lowering of the await expressions.
    /// </summary>
    public static BoundStatement RewriteWithoutHoisting(
        BoundStatement node,
        MethodSymbol method,
        TypeCompilationState compilationState,
        BindingDiagnosticBag diagnostics)
    {
        Debug.Assert(method is SynthesizedStateMachineMoveNextMethod { RuntimeAsync: true, IsAsync: true });

        OrderedSet<Symbol> variablesToHoist = new OrderedSet<Symbol>();
        var hoistedLocals = ArrayBuilder<LocalSymbol>.GetInstance();
        var factory = new SyntheticBoundNodeFactory(method, node.Syntax, compilationState, diagnostics);
        var rewriter = new RuntimeAsyncRewriter(factory, variablesToHoist, hoistedLocals);
        var result = (BoundStatement)rewriter.Visit(node);

        hoistedLocals.Free();

        return SpillSequenceSpiller.Rewrite(result, method, compilationState, diagnostics);
    }

    private readonly SyntheticBoundNodeFactory _factory;
    private readonly Dictionary<BoundAwaitableValuePlaceholder, BoundExpression> _placeholderMap;
    private readonly IReadOnlySet<Symbol> _variablesToHoist;
    private readonly RefInitializationHoister<LocalSymbol, BoundLocal> _refInitializationHoister;
    private readonly ArrayBuilder<LocalSymbol> _hoistedLocals;
    private readonly Dictionary<Symbol, CapturedSymbolReplacement> _proxies = [];

    private RuntimeAsyncRewriter(SyntheticBoundNodeFactory factory, IReadOnlySet<Symbol> variablesToHoist, ArrayBuilder<LocalSymbol> hoistedLocals)
    {
        Debug.Assert(factory.CurrentFunction != null);
        _factory = factory;
        _placeholderMap = [];
        _variablesToHoist = variablesToHoist;
        _refInitializationHoister = new RefInitializationHoister<LocalSymbol, BoundLocal>(_factory, _factory.CurrentFunction, TypeMap.Empty);
        _hoistedLocals = hoistedLocals;
    }

    [return: NotNullIfNotNull(nameof(node))]
    public override BoundNode? Visit(BoundNode? node)
    {
        if (node == null) return node;
        var oldSyntax = _factory.Syntax;
        _factory.Syntax = node.Syntax;
        var result = base.Visit(node);
        _factory.Syntax = oldSyntax;
        return result;
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

        if (awaitableInfo.IsDynamic)
        {
            // https://github.com/dotnet/roslyn/issues/79762: await dynamic will need runtime checks, see AsyncMethodToStateMachine.GenerateAwaitOnCompletedDynamic
            Debug.Assert(_factory.CurrentFunction is not null);
            // Method '{0}' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
            _factory.Diagnostics.Add(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync,
                node.Syntax.Location,
                _factory.CurrentFunction);
            return node;
        }

        var runtimeAsyncAwaitCall = awaitableInfo.RuntimeAsyncAwaitCall;
        Debug.Assert(runtimeAsyncAwaitCall is not null);
        Debug.Assert(awaitableInfo.RuntimeAsyncAwaitCallPlaceholder is not null);
        var runtimeAsyncAwaitMethod = runtimeAsyncAwaitCall.Method;
        Debug.Assert(runtimeAsyncAwaitMethod is not null);
        Debug.Assert(ReferenceEquals(
            runtimeAsyncAwaitMethod.ContainingType.OriginalDefinition,
            _factory.Compilation.GetSpecialType(InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers)));
        Debug.Assert(runtimeAsyncAwaitMethod.Name is "Await" or "UnsafeAwaitAwaiter" or "AwaitAwaiter");

        if (runtimeAsyncAwaitMethod.Name == "Await")
        {
            // This is the direct await case, with no need for the full pattern.
            // System.Runtime.CompilerServices.RuntimeHelpers.Await(awaitedExpression)
            var expr = VisitExpression(node.Expression);
            _placeholderMap.Add(awaitableInfo.RuntimeAsyncAwaitCallPlaceholder, expr);
            var call = Visit(awaitableInfo.RuntimeAsyncAwaitCall);
            _placeholderMap.Remove(awaitableInfo.RuntimeAsyncAwaitCallPlaceholder);
            return call;
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
        Debug.Assert(awaitableInfo.RuntimeAsyncAwaitCall is not null);
        Debug.Assert(awaitableInfo.RuntimeAsyncAwaitCallPlaceholder is not null);
        _placeholderMap.Add(awaitableInfo.RuntimeAsyncAwaitCallPlaceholder, tmp);
        var awaitCall = (BoundCall)Visit(awaitableInfo.RuntimeAsyncAwaitCall);
        _placeholderMap.Remove(awaitableInfo.RuntimeAsyncAwaitCallPlaceholder);

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

    public override BoundNode? VisitAssignmentOperator(BoundAssignmentOperator node)
    {
        if (node.Left is not BoundLocal leftLocal)
        {
            return base.VisitAssignmentOperator(node);
        }

        BoundExpression visitedRight;

        if (_variablesToHoist.Contains(leftLocal.LocalSymbol) && !_proxies.ContainsKey(leftLocal.LocalSymbol))
        {
            Debug.Assert(leftLocal.LocalSymbol.SynthesizedKind == SynthesizedLocalKind.Spill ||
                         (leftLocal.LocalSymbol.SynthesizedKind == SynthesizedLocalKind.ForEachArray && leftLocal.LocalSymbol.Type.HasInlineArrayAttribute(out _) && leftLocal.LocalSymbol.Type.TryGetInlineArrayElementField() is object));
            Debug.Assert(node.IsRef);
            visitedRight = VisitExpression(node.Right);
            return _refInitializationHoister.HoistRefInitialization(
                leftLocal.LocalSymbol,
                visitedRight,
                _proxies,
                createHoistedLocal,
                createHoistedAccess,
                this,
                isRuntimeAsync: true);
        }

        var visitedLeftOrProxy = VisitExpression(leftLocal);
        visitedRight = VisitExpression(node.Right);

        if (visitedLeftOrProxy is not BoundLocal visitLeftLocal)
        {
            // Proxy replacement occurred. We need to reassign the proxy into our local as a sequence.
            // ref leftLocal = ref proxy;
            // leftLocal = visitedRight;
            var assignment = _factory.AssignmentExpression(leftLocal, visitedLeftOrProxy, isRef: true);
            return _factory.Sequence([assignment], node.Update(leftLocal, visitedRight, node.IsRef, node.Type));
        }

        return node.Update(visitedLeftOrProxy, visitedRight, node.IsRef, node.Type);

        static LocalSymbol createHoistedLocal(TypeSymbol type, RuntimeAsyncRewriter @this, LocalSymbol local)
        {
            var hoistedLocal = @this._factory.SynthesizedLocal(type, syntax: local.GetDeclaratorSyntax(), kind: SynthesizedLocalKind.AwaitByRefSpill);
            @this._hoistedLocals.Add(hoistedLocal);
            return hoistedLocal;
        }

        static BoundLocal createHoistedAccess(LocalSymbol local, RuntimeAsyncRewriter @this)
            => @this._factory.Local(local);
    }

    private bool TryReplaceWithProxy(Symbol localOrParameter, SyntaxNode syntax, [NotNullWhen(true)] out BoundNode? replacement)
    {
        if (_proxies.TryGetValue(localOrParameter, out CapturedSymbolReplacement? proxy))
        {
            replacement = proxy.Replacement(syntax, makeFrame: null, this);
            return true;
        }

        replacement = null;
        return false;
    }

    public override BoundNode VisitLocal(BoundLocal node)
    {
        if (TryReplaceWithProxy(node.LocalSymbol, node.Syntax, out BoundNode? replacement))
        {
            return replacement;
        }

        Debug.Assert(!_variablesToHoist.Contains(node.LocalSymbol));
        return base.VisitLocal(node)!;
    }

    public override BoundNode? VisitParameter(BoundParameter node)
    {
        if (TryReplaceWithProxy(node.ParameterSymbol, node.Syntax, out BoundNode? replacement))
        {
            // Currently, the only parameter we expect to be replaced is `this`, which is handled through VisitThisReference.
            // Any other ref to a parameter should have either already been hoisted to a local during local rewriting, or should
            // be an illegal ref to a parameter across an await.
            throw ExceptionUtilities.Unreachable();
        }

        Debug.Assert(!_variablesToHoist.Contains(node.ParameterSymbol));
        return base.VisitParameter(node);
    }

    public override BoundNode? VisitThisReference(BoundThisReference node)
    {
        Debug.Assert(_factory.CurrentFunction is not null);
        var thisParameter = this._factory.CurrentFunction.ThisParameter;
        Debug.Assert(thisParameter is not null);
        if (TryReplaceWithProxy(thisParameter, node.Syntax, out BoundNode? replacement))
        {
            return replacement;
        }

        Debug.Assert(thisParameter is not { Type.IsValueType: true, RefKind: RefKind.Ref });
        return base.VisitThisReference(node);
    }

    public override BoundNode? VisitExpressionStatement(BoundExpressionStatement node)
    {
        var expr = VisitExpression(node.Expression);
        if (expr is null)
        {
            // Happens when the node is a hoisted expression that has no side effects.
            // The generated proxy will have the original content from this node and we can drop it.
            return _factory.StatementList();
        }

        return node.Update(expr);
    }

    public override BoundNode? VisitStateMachineInstanceId(BoundStateMachineInstanceId node)
        => throw ExceptionUtilities.Unreachable();
}
