// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
        return (BoundStatement)rewriter.Visit(node);
    }

    private readonly CSharpCompilation _compilation;
    private readonly SyntheticBoundNodeFactory _factory;

    private RuntimeAsyncRewriter(CSharpCompilation compilation, SyntheticBoundNodeFactory factory)
    {
        _compilation = compilation;
        _factory = factory;
    }

    private NamedTypeSymbol Task
    {
        get => field ??= _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task);
    } = null!;

    private NamedTypeSymbol TaskT
    {
        get => field ??= _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
    } = null!;

    private NamedTypeSymbol ValueTask
    {
        get => field ??= _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask);
    } = null!;

    private NamedTypeSymbol ValueTaskT
    {
        get => field ??= _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask_T);
    } = null!;

    public BoundExpression VisitExpression(BoundExpression node)
    {
        var result = Visit(node);
        Debug.Assert(result is BoundExpression);
        return (BoundExpression)result;
    }

    public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
    {
        var nodeType = node.Expression.Type;
        Debug.Assert(nodeType is not null);
        var originalType = nodeType.OriginalDefinition;

        WellKnownMember awaitCall;
        TypeWithAnnotations? maybeNestedType = null;

        if (ReferenceEquals(originalType, Task))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitTask;
        }
        else if (ReferenceEquals(originalType, TaskT))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitTaskT_T;
            maybeNestedType = ((NamedTypeSymbol)nodeType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
        }
        else if (ReferenceEquals(originalType, ValueTask))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitValueTask;
        }
        else if (ReferenceEquals(originalType, ValueTaskT))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitValueTaskT_T;
            maybeNestedType = ((NamedTypeSymbol)nodeType).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
        }
        else
        {
            // PROTOTYPE: when it's not a method with Task/TaskT/ValueTask/ValueTaskT returns, use the helpers
            return base.VisitAwaitExpression(node);
        }

        // PROTOTYPE: Make sure that we report an error in initial binding if these are missing
        var awaitMethod = (MethodSymbol?)_compilation.GetWellKnownTypeMember(awaitCall);
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

        // System.Runtime.CompilerServices.RuntimeHelpers.Await(awaitedCall)
        return _factory.Call(receiver: null, awaitMethod, VisitExpression(node.Expression));
    }
}
