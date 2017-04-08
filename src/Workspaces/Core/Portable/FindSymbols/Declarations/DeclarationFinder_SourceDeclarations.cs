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

    internal static partial class DeclarationFinder
    {
        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
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
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var (succeded, results) = await TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
                solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

            if (succeded)
            {
                return results;
            }

            return await FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var query = SearchQuery.Create(name, ignoreCase);
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                await AddCompilationDeclarationsWithNormalQueryAsync(
                    project, query, criteria, result, cancellationToken).ConfigureAwait(false);
            }

            return result.ToImmutableAndFree();
        }

        private static async Task<(bool, ImmutableArray<SymbolAndProjectId>)> TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var session = await SymbolFinder.TryGetRemoteSessionAsync(solution, cancellationToken).ConfigureAwait(false);
            if (session != null)
            {
                var result = await session.InvokeAsync<SerializableSymbolAndProjectId[]>(
                    nameof(IRemoteSymbolFinder.FindSolutionSourceDeclarationsWithNormalQuery),
                    name, ignoreCase, criteria).ConfigureAwait(false);

                var rehydrated = await RehydrateAsync(
                    solution, result, cancellationToken).ConfigureAwait(false);

                return (true, rehydrated);
            }

            return (false, ImmutableArray<SymbolAndProjectId>.Empty);
        }

        public static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsithNormalQueryAsync(
            Project project, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
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
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var (succeded, results) = await TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
                project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

            if (succeded)
            {
                return results;
            }

            return await FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<(bool, ImmutableArray<SymbolAndProjectId>)> TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
            Project project, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var session = await SymbolFinder.TryGetRemoteSessionAsync(project.Solution, cancellationToken).ConfigureAwait(false);
            if (session != null)
            {
                var result = await session.InvokeAsync<SerializableSymbolAndProjectId[]>(
                    nameof(IRemoteSymbolFinder.FindProjectSourceDeclarationsWithNormalQuery),
                    project.Id, name, ignoreCase, criteria).ConfigureAwait(false);

                var rehydrated = await RehydrateAsync(
                    project.Solution, result, cancellationToken).ConfigureAwait(false);

                return (true, rehydrated);
            }

            return (false, ImmutableArray<SymbolAndProjectId>.Empty);
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var list = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            await AddCompilationDeclarationsWithNormalQueryAsync(
                project, SearchQuery.Create(name, ignoreCase),
                filter, list, cancellationToken).ConfigureAwait(false);
            return list.ToImmutableAndFree();
        }
    }
}