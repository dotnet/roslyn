// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    internal partial class SymbolTreeInfoIncrementalAnalyzerProvider
    {
        private class SymbolTreeInfoCacheService : ISymbolTreeInfoCacheService
        {
            private readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> _projectToInfo;
            private readonly ConcurrentDictionary<string, MetadataInfo> _metadataPathToInfo;

            public SymbolTreeInfoCacheService(
                ConcurrentDictionary<ProjectId, SymbolTreeInfo> projectToInfo,
                ConcurrentDictionary<string, MetadataInfo> metadataPathToInfo)
            {
                _projectToInfo = projectToInfo;
                _metadataPathToInfo = metadataPathToInfo;
            }

            public async Task<SymbolTreeInfo> TryGetMetadataSymbolTreeInfoAsync(
                Solution solution,
                PortableExecutableReference reference,
                CancellationToken cancellationToken)
            {
                var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, reference, cancellationToken);

                var key = GetReferenceKey(reference);
                if (key != null)
                {
                    if (_metadataPathToInfo.TryGetValue(key, out var metadataInfo) &&
                        metadataInfo.SymbolTreeInfo.Checksum == checksum)
                    {
                        return metadataInfo.SymbolTreeInfo;
                    }
                }

                // If we didn't have it in our cache, see if we can load it from disk.
                // Note: pass 'loadOnly' so we only attempt to load from disk, not to actually
                // try to create the metadata.
                var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    solution, reference, checksum, loadOnly: true, cancellationToken).ConfigureAwait(false);
                return info;
            }

            public async Task<SymbolTreeInfo> TryGetSourceSymbolTreeInfoAsync(
                Project project, CancellationToken cancellationToken)
            {
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);
                if (_projectToInfo.TryGetValue(project.Id, out var projectInfo) &&
                    projectInfo.Checksum == checksum)
                {
                    return projectInfo;
                }

                // If we didn't have it in our cache, see if we can load it from disk.
                // Note: pass 'loadOnly' so we only attempt to load from disk, not to actually
                // try to create the index.
                var info = await SymbolTreeInfo.GetInfoForSourceAssemblyAsync(
                    project, checksum, loadOnly: true, cancellationToken).ConfigureAwait(false);
                return info;
            }
        }
    }
}
