// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal sealed class CallsGraphQuery : IGraphQuery
{
    public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
    {
        var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

        foreach (var node in context.InputNodes)
        {
            var symbol = graphBuilder.GetSymbol(node, cancellationToken);
            if (symbol != null)
            {
                foreach (var newSymbol in await GetCalledMethodSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var newNode = await graphBuilder.AddNodeAsync(newSymbol, relatedNode: node, cancellationToken).ConfigureAwait(false);
                    graphBuilder.AddLink(node, CodeLinkCategories.Calls, newNode, cancellationToken);
                }
            }
        }

        return graphBuilder;
    }

    private static async Task<ImmutableArray<ISymbol>> GetCalledMethodSymbolsAsync(
        ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var symbols);

        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            var semanticModel = await solution.GetDocument(reference.SyntaxTree).GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var syntaxNode in (await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false)).DescendantNodes())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var newSymbol = semanticModel.GetSymbolInfo(syntaxNode, cancellationToken).Symbol;
                if (newSymbol != null && newSymbol is IMethodSymbol &&
                    (newSymbol.CanBeReferencedByName || ((IMethodSymbol)newSymbol).MethodKind == MethodKind.Constructor))
                {
                    symbols.Add(newSymbol);
                }
            }
        }

        return symbols.ToImmutable();
    }
}
