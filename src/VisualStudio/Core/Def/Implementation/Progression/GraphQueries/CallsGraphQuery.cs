// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.CodeSchema;
using Microsoft.VisualStudio.GraphModel.Schemas;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class CallsGraphQuery : IGraphQuery
    {
        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            foreach (var node in context.InputNodes)
            {
                var symbol = graphBuilder.GetSymbol(node);
                if (symbol != null)
                {
                    foreach (var newSymbol in await GetCalledMethodSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var newNode = await graphBuilder.AddNodeForSymbolAsync(newSymbol, relatedNode: node).ConfigureAwait(false);
                        graphBuilder.AddLink(node, CodeLinkCategories.Calls, newNode);
                    }
                }
            }

            return graphBuilder;
        }

        private static async Task<IEnumerable<ISymbol>> GetCalledMethodSymbolsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var symbols = new List<ISymbol>();

            foreach (var reference in symbol.DeclaringSyntaxReferences)
            {
                var semanticModel = await solution.GetDocument(reference.SyntaxTree).GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                foreach (var syntaxNode in (await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false)).DescendantNodes())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var newSymbol = semanticModel.GetSymbolInfo(syntaxNode, cancellationToken).Symbol;
                    if (newSymbol is IMethodSymbol _ && (newSymbol.CanBeReferencedByName || ((IMethodSymbol)newSymbol).MethodKind == MethodKind.Constructor))
                    {
                        symbols.Add(newSymbol);
                    }
                }
            }

            return symbols;
        }
    }
}
