// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class ImplementsGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.GraphQuery_Implements, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

                foreach (var node in context.InputNodes)
                {
                    var symbol = graphBuilder.GetSymbol(node);
                    if (symbol is INamedTypeSymbol namedType)
                    {
                        var implementedSymbols = ImmutableArray<ISymbol>.CastUp(namedType.AllInterfaces);

                        await AddImplementedSymbolsAsync(graphBuilder, node, implementedSymbols).ConfigureAwait(false);
                    }
                    else if (symbol is IMethodSymbol ||
                             symbol is IPropertySymbol ||
                             symbol is IEventSymbol)
                    {
                        var implements = await SymbolFinder.FindImplementedInterfaceMembersArrayAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                        await AddImplementedSymbolsAsync(graphBuilder, node, implements).ConfigureAwait(false);
                    }
                }

                return graphBuilder;
            }
        }

        private static async Task AddImplementedSymbolsAsync(
            GraphBuilder graphBuilder, GraphNode node,
            ImmutableArray<ISymbol> implementedSymbols)
        {
            foreach (var interfaceType in implementedSymbols)
            {
                var interfaceTypeNode = await graphBuilder.AddNodeAsync(interfaceType, relatedNode: node).ConfigureAwait(false);
                graphBuilder.AddLink(node, CodeLinkCategories.Implements, interfaceTypeNode);
            }
        }
    }
}
