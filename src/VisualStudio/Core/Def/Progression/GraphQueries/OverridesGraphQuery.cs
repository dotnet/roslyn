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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

internal sealed class OverridesGraphQuery : IGraphQuery
{
    public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.GraphQuery_Overrides, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            foreach (var node in context.InputNodes)
            {
                var symbol = graphBuilder.GetSymbol(node, cancellationToken);
                if (symbol is IMethodSymbol or
                    IPropertySymbol or
                    IEventSymbol)
                {
                    var overrides = await SymbolFinder.FindOverridesAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                    foreach (var o in overrides)
                    {
                        var symbolNode = await graphBuilder.AddNodeAsync(o, relatedNode: node, cancellationToken).ConfigureAwait(false);
                        graphBuilder.AddLink(symbolNode, RoslynGraphCategories.Overrides, node, cancellationToken);
                    }
                }
            }

            return graphBuilder;
        }
    }
}
