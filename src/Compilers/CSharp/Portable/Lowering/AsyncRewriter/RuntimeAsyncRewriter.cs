// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp;

internal class RuntimeAsyncRewriter : BoundTreeRewriterWithStackGuard
{
    public static BoundStatement Rewrite(
        BoundStatement node,
        MethodSymbol method,
        TypeCompilationState compilationState,
        BindingDiagnosticBag diagnostics)
    {
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
        get
        {
            if (field is null)
            {
                field = _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task);
            }

            return field;
        }
    } = null!;

    private NamedTypeSymbol TaskT
    {
        get
        {
            if (field is null)
            {
                field = _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T);
            }

            return field;
        }
    } = null!;

    private NamedTypeSymbol ValueTask
    {
        get
        {
            if (field is null)
            {
                field = _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask);
            }

            return field;
        }
    } = null!;

    private NamedTypeSymbol ValueTaskT
    {
        get
        {
            if (field is null)
            {
                field = _compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask_T);
            }

            return field;
        }
    } = null!;

    public override BoundNode? VisitAwaitExpression(BoundAwaitExpression node)
    {
        // PROTOTYPE: when it's not a method with Task/TaskT/ValueTask/ValueTaskT returns, use the helpers
        if (node is not { Expression: BoundCall awaitedCall })
        {
            return base.VisitAwaitExpression(node);
        }

        var originalType = awaitedCall.Type.OriginalDefinition;

        WellKnownMember awaitCall;
        TypeWithAnnotations? maybeNestedType = null;

        if (originalType.Equals(Task))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitTask;
        }
        else if (originalType.Equals(TaskT))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitTaskT_T;
            maybeNestedType = ((NamedTypeSymbol)awaitedCall.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
        }
        else if (originalType.Equals(ValueTask))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitValueTask;
        }
        else if (originalType.Equals(ValueTaskT))
        {
            awaitCall = WellKnownMember.System_Runtime_CompilerServices_RuntimeHelpers__AwaitValueTaskT_T;
            maybeNestedType = ((NamedTypeSymbol)awaitedCall.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0];
        }
        else
        {
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
        return _factory.Call(receiver: null, awaitMethod, awaitedCall);
    }
}
