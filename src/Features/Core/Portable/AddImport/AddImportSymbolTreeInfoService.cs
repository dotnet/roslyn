// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal static class AddImportSymbolTreeInfoService
    {
        #region Source

        public static async Task<ImmutableArray<SymbolAndProjectId>> TryFindSourceSymbolsAsync(
            Project project, SymbolFilter filter, SearchQuery searchQuery,
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(searchQuery.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            var session = await project.Solution.TryCreateCodeAnalysisServiceSessionAsync(
                AddImportOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed, cancellationToken).ConfigureAwait(false);
            using (session)
            {
                if (session == null)
                {
                    return await TryFindSourceSymbolsInCurrentProcessAsync(
                        project, filter, searchQuery, projectToAssembly, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await TryFindSourceSymbolsInRemoteProcessAsync(
                        session, project, filter, searchQuery, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> TryFindSourceSymbolsInCurrentProcessAsync(
            Project project, SymbolFilter filter, SearchQuery searchQuery,
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
            CancellationToken cancellationToken)
        {
            var service = project.Solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
            var info = await service.TryGetSourceSymbolTreeInfoAsync(project, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                // Looks like there was nothing in the cache.  Return no results for now.
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            // Don't create the assembly until it is actually needed by the SymbolTreeInfo.FindAsync
            // code.  Creating the assembly can be costly and we want to avoid it until it is actually
            // needed.
            var lazyAssembly = projectToAssembly.GetOrAdd(project, CreateLazyAssembly);

            var declarations = await info.FindAsync(
                searchQuery, lazyAssembly, project.Id,
                filter, cancellationToken).ConfigureAwait(false);

            return declarations;
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> TryFindSourceSymbolsInRemoteProcessAsync(
            RemoteHostClient.Session session, Project project, SymbolFilter filter,
            SearchQuery searchQuery, CancellationToken cancellationToken)
        {
            var array = await session.InvokeAsync<SerializableSymbolAndProjectId[]>(
                nameof(IRemoteSymbolTreeInfoCacheService.TryFindSourceSymbolsAsync),
                new object[] { project.Id, filter, searchQuery.Name, searchQuery.Kind }).ConfigureAwait(false);

            return await ConvertAsync(project.Solution, array, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> ConvertAsync(
            Solution solution, SerializableSymbolAndProjectId[] array, CancellationToken cancellationToken)
        {
            if (array == null)
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            foreach (var value in array)
            {
                // Don't force getting the symbol on this end.  We don't want to perform
                // an expensive computation locally as that may slow down Add-Import greatly.
                var symbolAndProjectId = await value.TryRehydrateAsync(
                    solution, forceCompilation: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                result.AddIfNotNull(symbolAndProjectId);
            }

            return result.ToImmutableAndFree();
        }

        #endregion

        #region Metadata

        internal static async Task<ImmutableArray<SymbolAndProjectId>> TryFindMetadataSymbolsAsync(
            Solution solution, PortableExecutableReference metadataReference, Checksum metadataChecksum,
            IAssemblySymbol assembly, ProjectId assemblyProjectId,
            SymbolFilter filter, SearchQuery searchQuery,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(searchQuery.Kind == SearchKind.Custom, "Custom queries are not supported in this API");

            var session = await solution.TryCreateCodeAnalysisServiceSessionAsync(
                AddImportOptions.OutOfProcessAllowed, WellKnownExperimentNames.OutOfProcessAllowed, cancellationToken).ConfigureAwait(false);
            using (session)
            {
                if (session == null)
                {
                    return await TryFindMetadataSymbolsInCurrentProcessAsync(
                        solution, metadataReference, assembly, assemblyProjectId, 
                        filter, searchQuery, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await TryFindMetadataSymbolsInRemoteProcessAsync(
                        session, solution, metadataChecksum, assemblyProjectId,
                        filter, searchQuery, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> TryFindMetadataSymbolsInCurrentProcessAsync(
            Solution solution, PortableExecutableReference metadataReference,
            IAssemblySymbol assembly, ProjectId assemblyProjectId,
            SymbolFilter filter, SearchQuery searchQuery, CancellationToken cancellationToken)
        {
            var service = solution.Workspace.Services.GetService<ISymbolTreeInfoCacheService>();
            var info = await service.TryGetMetadataSymbolTreeInfoAsync(solution, metadataReference, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var declarations = await info.FindAsync(
                searchQuery, assembly, assemblyProjectId,
                filter, cancellationToken).ConfigureAwait(false);

            return declarations;
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> TryFindMetadataSymbolsInRemoteProcessAsync(
            RemoteHostClient.Session session, Solution solution,
            Checksum metadataChecksum, ProjectId assemblyProjectId, 
            SymbolFilter filter, SearchQuery searchQuery, 
            CancellationToken cancellationToken)
        {
            var array = await session.InvokeAsync<SerializableSymbolAndProjectId[]>(
                nameof(IRemoteSymbolTreeInfoCacheService.TryFindMetadataSymbolsAsync),
                new object[] { metadataChecksum, assemblyProjectId, filter, searchQuery.Name, searchQuery.Kind }).ConfigureAwait(false);

            return await ConvertAsync(solution, array, cancellationToken).ConfigureAwait(false);

        }

        #endregion

        private static AsyncLazy<IAssemblySymbol> CreateLazyAssembly(Project project)
        {
            return new AsyncLazy<IAssemblySymbol>(
                async c =>
                {
                    var compilation = await project.GetCompilationAsync(c).ConfigureAwait(false);
                    return compilation.Assembly;
                }, cacheResult: true);
        }
    }
}