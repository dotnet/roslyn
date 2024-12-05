// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Debugging;

internal abstract class AbstractDataTipInfoGetter<
    TExpressionSyntax,
    TMemberExpressionSyntax,
    TInvocationExpressionSyntax>
    where TExpressionSyntax : SyntaxNode
    where TMemberExpressionSyntax : TExpressionSyntax
    where TInvocationExpressionSyntax : TExpressionSyntax
{
    protected static async ValueTask<(DebugDataTipInfoKind kind, TextSpan? expressionSpan)> ComputeKindAsync(
        Document document, TExpressionSyntax expression, bool includeKind, CancellationToken cancellationToken)
    {
        if (includeKind)
        {
            var semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // as long as we keep seeing `.LinqMethod(...)` keep walking upwards.

            var isLinqExpression = false;
            while (expression.Parent is TMemberExpressionSyntax { Parent: TInvocationExpressionSyntax invocation } &&
                   IsLinqExtensionMethod(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol))
            {
                expression = invocation;
                isLinqExpression = true;
            }

            // As long as we saw at least one linq method, then return the span of hte outermost expression back to the
            // debugger to evaluate.
            if (isLinqExpression)
                return (DebugDataTipInfoKind.LinqExpression, expression.Span);
        }

        return default;
    }

    private static bool IsLinqExtensionMethod([NotNullWhen(true)] ISymbol? symbol)
        => symbol is IMethodSymbol
        {
            MethodKind: MethodKind.ReducedExtension,
            ContainingType.Name: nameof(Enumerable),
            ContainingType.ContainingNamespace.Name: nameof(System.Linq),
            ContainingType.ContainingNamespace.ContainingNamespace.Name: nameof(System),
            ContainingType.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true,
        };
}
