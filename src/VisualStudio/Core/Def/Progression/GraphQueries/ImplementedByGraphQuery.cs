// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class ImplementedByGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.GraphQuery_ImplementedBy, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

                foreach (var node in context.InputNodes)
                {
                    var symbol = graphBuilder.GetSymbol(node, cancellationToken);
                    if (symbol is INamedTypeSymbol or
                        IMethodSymbol or
                        IPropertySymbol or
                        IEventSymbol)
                    {
                        var implementations = await SymbolFinder.FindImplementationsAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

                        foreach (var implementation in implementations)
                        {
                            var symbolNode = await graphBuilder.AddNodeAsync(
                                implementation, relatedNode: node, cancellationToken).ConfigureAwait(false);
                            graphBuilder.AddLink(
                                symbolNode, CodeLinkCategories.Implements, node, cancellationToken);
                        }
                    }
                }

                return graphBuilder;
            }
        }
    }
}
