// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.Progression;

namespace Microsoft.VisualStudio.LanguageServices.Progression
{
    /// <summary>
    /// A helper class that implements the creation of <see cref="GraphNode"/>s.
    /// </summary>
    public static class GraphNodeCreation
    {
        public static async Task<GraphNodeId> CreateNodeIdAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                    return await GraphNodeIdCreation.GetIdForAssemblyAsync((IAssemblySymbol)symbol, solution, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Namespace:
                    return await GraphNodeIdCreation.GetIdForNamespaceAsync((INamespaceSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);

                case SymbolKind.NamedType:
                    return await GraphNodeIdCreation.GetIdForTypeAsync((ITypeSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Method:
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return await GraphNodeIdCreation.GetIdForMemberAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Parameter:
                    return await GraphNodeIdCreation.GetIdForParameterAsync((IParameterSymbol)symbol, solution, cancellationToken).ConfigureAwait(false);

                case SymbolKind.Local:
                case SymbolKind.RangeVariable:
                    return await GraphNodeIdCreation.GetIdForLocalVariableAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                default:
                    throw new ArgumentException(string.Format(ServicesVSResources.Can_t_create_a_node_id_for_this_symbol_kind_colon_0, symbol));
            }
        }

        public static async Task<GraphNode> CreateNodeAsync(this Graph graph, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return await GraphBuilder.GetOrCreateNodeAsync(graph, symbol, solution, cancellationToken).ConfigureAwait(false);
        }
    }
}
