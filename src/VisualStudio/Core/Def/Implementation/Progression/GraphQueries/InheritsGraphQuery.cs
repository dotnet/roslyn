﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class InheritsGraphQuery : IGraphQuery
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

                    if (graphBuilder.GetSymbol(node) is INamedTypeSymbol namedType)
                    {
                        if (namedType.BaseType != null)
                        {
                            var baseTypeNode = await graphBuilder.AddNodeForSymbolAsync(namedType.BaseType, relatedNode: node).ConfigureAwait(false);
                            newNodes.Add(baseTypeNode);
                            graphBuilder.AddLink(node, CodeLinkCategories.InheritsFrom, baseTypeNode);
                        }
                        else if (namedType.TypeKind == TypeKind.Interface && !namedType.OriginalDefinition.AllInterfaces.IsEmpty)
                        {
                            foreach (var baseNode in namedType.OriginalDefinition.AllInterfaces.Distinct())
                            {
                                var baseTypeNode = await graphBuilder.AddNodeForSymbolAsync(baseNode, relatedNode: node).ConfigureAwait(false);
                                newNodes.Add(baseTypeNode);
                                graphBuilder.AddLink(node, CodeLinkCategories.InheritsFrom, baseTypeNode);
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
