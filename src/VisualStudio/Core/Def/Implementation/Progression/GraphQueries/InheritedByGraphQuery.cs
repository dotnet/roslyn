// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                var symbolAndProjectId = graphBuilder.GetSymbolAndProjectId(node);
                if (!(symbolAndProjectId.Symbol is INamedTypeSymbol namedType))
                    continue;

                if (namedType.TypeKind == TypeKind.Class)
                {
                    var derivedTypes = await DependentTypeFinder.FindImmediatelyDerivedClassesAsync(
                        symbolAndProjectId.WithSymbol(namedType), solution, cancellationToken).ConfigureAwait(false);
                    foreach (var derivedType in derivedTypes)
                    {
                        var symbolNode = await graphBuilder.AddNodeAsync(
                            derivedType, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node);
                    }
                }
                else if (namedType.TypeKind == TypeKind.Interface)
                {
                    var derivedTypes = await DependentTypeFinder.FindImmediatelyDerivedAndImplementingTypesAsync(
                        symbolAndProjectId.WithSymbol(namedType), solution, cancellationToken).ConfigureAwait(false);
                    foreach (var derivedType in derivedTypes)
                    {
                        var symbolNode = await graphBuilder.AddNodeAsync(
                            derivedType, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(symbolNode, CodeLinkCategories.InheritsFrom, node);
                    }
                }
            }

            return graphBuilder;
        }
    }
}
