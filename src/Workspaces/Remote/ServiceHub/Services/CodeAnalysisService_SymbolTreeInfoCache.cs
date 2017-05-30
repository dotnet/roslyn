// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteSymbolTreeInfoCacheService
    {
        public async Task<SerializableSymbolAndProjectId[]> TryFindSourceSymbolsAsync(
            ProjectId projectId, SymbolFilter filter, string queryName, SearchKind queryKind)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var symbols = await AddImportSymbolTreeInfoService.TryFindSourceSymbolsInCurrentProcessAsync(
                solution.GetProject(projectId), filter, SearchQuery.Create(queryName, queryKind),
                new ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>>(), this.CancellationToken).ConfigureAwait(false);

            return symbols.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
        }

        public async Task<SerializableSymbolAndProjectId[]> TryFindMetadataSymbolsAsync(
            Checksum metadataChecksum, ProjectId assemblyProjectId, SymbolFilter filter,
            string queryName, SearchKind queryKind)
        {
            var solution = await GetSolutionAsync().ConfigureAwait(false);
            var metadataReference = await RoslynServices.AssetService.GetAssetAsync<PortableExecutableReference>(
                metadataChecksum, CancellationToken).ConfigureAwait(false);

            var project = solution.GetProject(assemblyProjectId);
            var compilation = await project.GetCompilationAsync(CancellationToken).ConfigureAwait(false);
            var projectAssembly = compilation.Assembly;

            var symbols = await AddImportSymbolTreeInfoService.TryFindMetadataSymbolsInCurrentProcessAsync(
                solution, metadataReference, projectAssembly, assemblyProjectId,
                filter, SearchQuery.Create(queryName, queryKind), CancellationToken).ConfigureAwait(false);

            return symbols.Select(SerializableSymbolAndProjectId.Dehydrate).ToArray();
        }
    }
}