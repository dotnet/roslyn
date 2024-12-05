// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Debugging;

internal abstract class AbstractDataTipInfoGetter<TExpressionSyntax>
    where TExpressionSyntax : SyntaxNode
{
    protected static async ValueTask<(DebugDataTipInfoKind kind, SemanticModel? semanticModel)> ComputeKindAsync(
        Document document, TExpressionSyntax expression, bool includeKind, CancellationToken cancellationToken)
    {
        if (includeKind)
        {
            var semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
            if (symbol is IMethodSymbol
                {
                    MethodKind: MethodKind.ReducedExtension,
                    ContainingType.Name: nameof(Enumerable),
                    ContainingType.ContainingNamespace.Name: nameof(System.Linq),
                    ContainingType.ContainingNamespace.ContainingNamespace.Name: nameof(System),
                    ContainingType.ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true,
                })
            {
                return (DebugDataTipInfoKind.LinqExpression, semanticModel);
            }
        }

        return default;
    }
}
