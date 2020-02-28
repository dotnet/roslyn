﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class OverridesGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.GraphQuery_Overrides, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

                foreach (var node in context.InputNodes)
                {
                    var symbol = graphBuilder.GetSymbol(node);
                    if (symbol is IMethodSymbol || symbol is IPropertySymbol || symbol is IEventSymbol)
                    {
                        var overrides = await SymbolFinder.FindOverridesAsync(symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                        foreach (var o in overrides)
                        {
                            var symbolNode = await graphBuilder.AddNodeForSymbolAsync(o, relatedNode: node).ConfigureAwait(false);
                            graphBuilder.AddLink(symbolNode, RoslynGraphCategories.Overrides, node);
                        }
                    }
                }

                return graphBuilder;
            }
        }
    }
}
