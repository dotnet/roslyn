﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class ContainsGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);
            var nodesToProcess = context.InputNodes;

            for (var depth = 0; depth < context.LinkDepth; depth++)
            {
                // This is the list of nodes we created and will process
                var newNodes = new HashSet<GraphNode>();

                foreach (var node in nodesToProcess)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var symbol = graphBuilder.GetSymbol(node);

                    if (symbol != null)
                    {
                        foreach (var newSymbol in SymbolContainment.GetContainedSymbols(symbol))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var newNode = await graphBuilder.AddNodeAsync(newSymbol, relatedNode: node).ConfigureAwait(false);
                            graphBuilder.AddLink(node, GraphCommonSchema.Contains, newNode);
                        }
                    }
                    else if (node.HasCategory(CodeNodeCategories.File))
                    {
                        var document = graphBuilder.GetContextDocument(node);

                        if (document != null)
                        {
                            foreach (var newSymbol in await SymbolContainment.GetContainedSymbolsAsync(document, cancellationToken).ConfigureAwait(false))
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var newNode = await graphBuilder.AddNodeAsync(newSymbol, relatedNode: node).ConfigureAwait(false);
                                graphBuilder.AddLink(node, GraphCommonSchema.Contains, newNode);
                            }
                        }
                    }
                }

                nodesToProcess = newNodes;
            }

            return graphBuilder;
        }
    }
}
