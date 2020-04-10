// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class CallsGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            foreach (var node in context.InputNodes)
            {
                var symbolAndProjectId = graphBuilder.GetSymbolAndProjectId(node);
                if (symbolAndProjectId.Symbol != null)
                {
                    foreach (var newSymbol in await GetCalledMethodSymbolsAsync(symbolAndProjectId, solution, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var newNode = await graphBuilder.AddNodeAsync(newSymbol, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(node, CodeLinkCategories.Calls, newNode);
                    }
                }
            }

            return graphBuilder;
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> GetCalledMethodSymbolsAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SymbolAndProjectId>.GetInstance(out var symbols);

            foreach (var reference in symbolAndProjectId.Symbol.DeclaringSyntaxReferences)
            {
                var semanticModel = await solution.GetDocument(reference.SyntaxTree).GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                foreach (var syntaxNode in (await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false)).DescendantNodes())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var newSymbol = semanticModel.GetSymbolInfo(syntaxNode, cancellationToken).Symbol;
                    if (newSymbol != null && newSymbol is IMethodSymbol &&
                        (newSymbol.CanBeReferencedByName || ((IMethodSymbol)newSymbol).MethodKind == MethodKind.Constructor))
                    {
                        symbols.Add(symbolAndProjectId.WithSymbol(newSymbol));
                    }
                }
            }

            return symbols.ToImmutable();
        }
    }
}
