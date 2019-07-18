// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class InheritedByGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            foreach (var node in context.InputNodes)
            {
                if (!(graphBuilder.GetSymbol(node) is INamedTypeSymbol namedType))
                {
                    continue;
                }

                if (namedType.TypeKind == TypeKind.Class)
                {
                    var derivedTypes = await DependentTypeFinder.FindImmediatelyDerivedClassesAsync(namedType, solution, cancellationToken).ConfigureAwait(false);
                    foreach (var derivedType in derivedTypes)
                    {
                        var symbolNode = await graphBuilder.AddNodeForSymbolAsync(
                            derivedType.Symbol, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node);
                    }
                }
                else if (namedType.TypeKind == TypeKind.Interface)
                {
                    var derivedTypes = await DependentTypeFinder.FindImmediatelyDerivedAndImplementingTypesAsync(
                        namedType, solution, cancellationToken).ConfigureAwait(false);
                    foreach (var derivedType in derivedTypes)
                    {
                        var symbolNode = await graphBuilder.AddNodeForSymbolAsync(
                            derivedType.Symbol, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node);
                    }
                }
            }

            return graphBuilder;
        }
    }
}
