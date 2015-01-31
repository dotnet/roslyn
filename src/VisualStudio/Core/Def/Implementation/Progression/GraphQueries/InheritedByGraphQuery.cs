// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class InheritedByGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            foreach (var node in context.InputNodes)
            {
                var namedType = graphBuilder.GetSymbol(node) as INamedTypeSymbol;
                if (namedType == null)
                {
                    continue;
                }

                if (namedType.TypeKind == TypeKind.Class)
                {
                    var derivedTypes = await namedType.FindDerivedClassesAsync(solution, cancellationToken).ConfigureAwait(false);
                    foreach (var derivedType in derivedTypes)
                    {
                        var symbolNode = await graphBuilder.AddNodeForSymbolAsync(derivedType, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node);
                    }
                }
                else if (namedType.TypeKind == TypeKind.Interface)
                {
                    var derivedTypes = await namedType.FindDerivedInterfacesAsync(solution, cancellationToken).ConfigureAwait(false);
                    foreach (var derivedType in derivedTypes)
                    {
                        var symbolNode = await graphBuilder.AddNodeForSymbolAsync(derivedType, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node);
                    }
                }
            }

            return graphBuilder;
        }
    }
}
