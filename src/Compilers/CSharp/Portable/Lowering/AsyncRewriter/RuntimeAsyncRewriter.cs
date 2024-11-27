// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        if (!originalType.Equals(Task)
            && !originalType.Equals(TaskT)
            && !originalType.Equals(ValueTask)
            && !originalType.Equals(ValueTaskT))
        {
            return base.VisitAwaitExpression(node);
        }

        var runtimeAsyncMethodSymbol = new RuntimeAsyncMethodSymbol(awaitedCall.Method, _compilation);

        return awaitedCall.Update(runtimeAsyncMethodSymbol, runtimeAsyncMethodSymbol.ReturnType);
    }
}
