// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // All the logic for finding source declarations in a given solution/project with some name 
    // is in this file.  

    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
            => FindSourceDeclarationsAsync(solution, name, ignoreCase, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                return await FindSourceDeclarationsWithNormalQueryAsync(
                    solution, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
            => FindSourceDeclarationsAsync(project, name, ignoreCase, SymbolFilter.All, cancellationToken);

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                return await FindSourceDeclarationsithNormalQueryAsync(
                    project, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<ImmutableArray<ISymbol>> FindSourceDeclarationsWithNormalQueryAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            using (var session = await TryGetRemoteSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                if (session != null)
                {
                    var dehydated = await session.InvokeAsync<SerializableSymbolAndProjectId[]>(
                        nameof(IRemoteSymbolFinder.FindSolutionSourceDeclarationsAsync),
                        name, ignoreCase, filter).ConfigureAwait(false);

                    return await RehydrateAsync(solution, dehydated, cancellationToken).ConfigureAwait(false);
                }
            }

            var symbolAndProjectIds = await FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                solution, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
            return symbolAndProjectIds.SelectAsArray(t => t.Symbol);
        }

        private static async Task<ImmutableArray<ISymbol>> FindSourceDeclarationsithNormalQueryAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            var solution = project.Solution;
            using (var session = await TryGetRemoteSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                if (session != null)
                {
                    var dehydated = await session.InvokeAsync<SerializableSymbolAndProjectId[]>(
                        nameof(IRemoteSymbolFinder.FindProjectSourceDeclarationsAsync),
                        project.Id, name, ignoreCase, filter).ConfigureAwait(false);

                    return await RehydrateAsync(solution, dehydated, cancellationToken).ConfigureAwait(false);
                }
            }

            var symbolsAndProjectIds = await FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                project, name, ignoreCase, filter, cancellationToken).ConfigureAwait(false);
            return symbolsAndProjectIds.SelectAsArray(t => t.Symbol);
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var query = SearchQuery.Create(name, ignoreCase);
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                await AddCompilationDeclarationsWithNormalQueryAsync(
                    project, query, filter, result, cancellationToken).ConfigureAwait(false);
            }

            return result.ToImmutableAndFree();
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var list = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            await AddCompilationDeclarationsWithNormalQueryAsync(
                project, SearchQuery.Create(name, ignoreCase), filter, list, cancellationToken).ConfigureAwait(false);
            return list.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<ISymbol>> RehydrateAsync(
            Solution solution, SerializableSymbolAndProjectId[] dehydated, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<ISymbol>.GetInstance(dehydated.Length);

            foreach (var serialized in dehydated)
            {
                var rehydrated = await serialized.RehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);
                result.Add(rehydrated.Symbol);
            }

            return result.ToImmutableAndFree();
        }
    }
}