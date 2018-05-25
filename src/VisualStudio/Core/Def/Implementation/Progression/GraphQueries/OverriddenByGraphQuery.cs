// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class OverriddenByGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            foreach (var node in context.InputNodes)
            {
                var symbol = graphBuilder.GetSymbol(node);
                if (symbol != null)
                {
                    var overriddenMember = symbol.OverriddenMember();

                    if (overriddenMember != null)
                    {
                        var symbolNode = await graphBuilder.AddNodeForSymbolAsync(overriddenMember, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(node, RoslynGraphCategories.Overrides, symbolNode);
                    }
                }
            }

            return graphBuilder;
        }
    }
}
