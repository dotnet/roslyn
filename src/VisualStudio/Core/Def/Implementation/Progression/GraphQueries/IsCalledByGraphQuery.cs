// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class IsCalledByGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.GraphQuery_IsCalledBy, KeyValueLogMessage.Create(LogType.UserAction), cancellationToken))
            {
                var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

                foreach (var node in context.InputNodes)
                {
                    var symbol = graphBuilder.GetSymbol(node);
                    if (symbol != null)
                    {
                        var callers = await SymbolFinder.FindCallersAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                        foreach (var caller in callers.Where(c => c.IsDirect))
                        {
                            var callerNode = await graphBuilder.AddNodeForSymbolAsync(caller.CallingSymbol, relatedNode: node).ConfigureAwait(false);
                            graphBuilder.AddLink(callerNode, CodeLinkCategories.Calls, node);
                        }
                    }
                }

                return graphBuilder;
            }
        }
    }
}
