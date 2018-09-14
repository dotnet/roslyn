// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // Logic related to finding declarations with a completely custom predicate goes here.
    // Completely custom predicates can not be optimized in any way as there is no way to
    // tell what the predicate will return true for.
    //
    // Also, because we have no control over these predicates, we cannot remote these queries
    // over to the OOP process.

    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, CancellationToken cancellationToken = default)
            => FindSourceDeclarationsAsync(solution, predicate, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default)
        {
            using (var query = SearchQuery.CreateCustom(predicate))
            {
                var declarations = await FindSourceDeclarationsWithCustomQueryAsync(
                    solution, query, filter, cancellationToken).ConfigureAwait(false);

                return declarations.SelectAsArray(d => d.Symbol);
            }
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithCustomQueryAsync(
            Solution solution, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Predicate_FindSourceDeclarationsAsync, cancellationToken))
            {
                var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();
                foreach (var projectId in solution.ProjectIds)
                {
                    var project = solution.GetProject(projectId);
                    var symbols = await FindSourceDeclarationsWithCustomQueryAsync(project, query, filter, cancellationToken).ConfigureAwait(false);
                    result.AddRange(symbols);
                }

                return result.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, CancellationToken cancellationToken = default)
            => FindSourceDeclarationsAsync(project, predicate, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default)
        {
            using (var query = SearchQuery.CreateCustom(predicate))
            {
                var declarations = await FindSourceDeclarationsWithCustomQueryAsync(
                    project, query, filter, cancellationToken).ConfigureAwait(false);

                return declarations.SelectAsArray(d => d.Symbol);
            }
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithCustomQueryAsync(
            Project project, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Predicate_FindSourceDeclarationsAsync, cancellationToken))
            {
                if (await project.ContainsSymbolsWithNameAsync(query.GetPredicate(), filter, cancellationToken).ConfigureAwait(false))
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    var unfiltered = compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken)
                                                .Select(s => new SymbolAndProjectId(s, project.Id))
                                                .ToImmutableArray();

                    return DeclarationFinder.FilterByCriteria(unfiltered, filter);
                }
            }

            return ImmutableArray<SymbolAndProjectId>.Empty;
        }
    }
}
