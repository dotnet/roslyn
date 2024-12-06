// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Look to see if this is a linq method call. If so, return the invocation so the debugger can provide a
            // custom linq experience for it.
            if (expression.Parent is TMemberExpressionSyntax { Parent: TInvocationExpressionSyntax invocation } memberAccess &&
                syntaxFacts.GetRightSideOfDot(memberAccess) == expression &&
                IsLinqExtensionMethod(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol))
            {
                return (DebugDataTipInfoKind.LinqExpression, invocation.Span);
            }
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
