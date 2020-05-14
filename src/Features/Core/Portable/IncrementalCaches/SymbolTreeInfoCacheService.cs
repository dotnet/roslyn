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
            // Shared with SymbolTreeInfoIncrementalAnalyzer.  They populate these values, we read from them.

            private readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> _projectIdToInfo;
            private readonly ConcurrentDictionary<MetadataId, MetadataInfo> _metadataIdToInfo;

            public SymbolTreeInfoCacheService(
                ConcurrentDictionary<ProjectId, SymbolTreeInfo> projectIdToInfo,
                ConcurrentDictionary<MetadataId, MetadataInfo> metadataIdToInfo)
            {
                _projectIdToInfo = projectIdToInfo;
                _metadataIdToInfo = metadataIdToInfo;
            }

            public async Task<SymbolTreeInfo> TryGetMetadataSymbolTreeInfoAsync(
                Solution solution,
                PortableExecutableReference reference,
                CancellationToken cancellationToken)
            {
                var metadataId = SymbolTreeInfo.GetMetadataIdNoThrow(reference);
                if (metadataId == null)
                    return null;

                var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, reference, cancellationToken);

                // See if the last value produced matches what the caller is asking for.  If so, return that.
                if (_metadataIdToInfo.TryGetValue(metadataId, out var metadataInfo) &&
                    metadataInfo.SymbolTreeInfo.Checksum == checksum)
                {
                    return metadataInfo.SymbolTreeInfo;
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
                // See if the last value produced matches what the caller is asking for.  If so, return that.
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);
                if (_projectIdToInfo.TryGetValue(project.Id, out var projectInfo) &&
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
