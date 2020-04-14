// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // This file contains the legacy SymbolFinder APIs.  The APIs are legacy because they
    // do not contain enough information for us to effectively remote them over to the OOP
    // process to do the work.  Specifically, they lack the "current project context" necessary
    // to be able to effectively serialize symbols to/from the remote process.

    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find symbols for members that override the specified member symbol.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindOverridesAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindOverridesAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);

            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Find symbols for declarations that implement members of the specified interface symbol
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementedInterfaceMembersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindImplementedInterfaceMembersAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Finds the derived classes of the given type. Implementations of an interface are not considered "derived", but can be found
        /// with <see cref="FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project}, CancellationToken)"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
        public static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindDerivedClassesAsync(
                SymbolAndProjectId.Create(type, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Finds the symbols that implement an interface or interface member.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementationsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var result = await FindImplementationsAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, projects, cancellationToken).ConfigureAwait(false);
            return result.SelectAsArray(s => s.Symbol);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken = default)
        {
            return FindCallersAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, documents: null, cancellationToken);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Document> documents, CancellationToken cancellationToken = default)
        {
            return FindCallersAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, documents, cancellationToken);
        }
    }
}
