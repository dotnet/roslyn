// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree
{
    [ExportWorkspaceService(typeof(ISymbolTreeInfoCacheService)), Shared]
    internal sealed partial class SymbolTreeInfoCacheService : ISymbolTreeInfoCacheService
    {

        public readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> ProjectIdToInfo = new();
        public readonly ConcurrentDictionary<MetadataId, MetadataInfo> MetadataIdToInfo = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SymbolTreeInfoCacheService()
        {
        }

        public async ValueTask<SymbolTreeInfo?> TryGetMetadataSymbolTreeInfoAsync(
            Solution solution,
            PortableExecutableReference reference,
            CancellationToken cancellationToken)
        {
            var metadataId = SymbolTreeInfo.GetMetadataIdNoThrow(reference);
            if (metadataId == null)
                return null;

            var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, reference, cancellationToken);

            // See if the last value produced matches what the caller is asking for.  If so, return that.
            if (MetadataIdToInfo.TryGetValue(metadataId, out var metadataInfo) &&
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

        public async Task<SymbolTreeInfo?> TryGetSourceSymbolTreeInfoAsync(
            Project project, CancellationToken cancellationToken)
        {
            // See if the last value produced matches what the caller is asking for.  If so, return that.
            var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);
            if (ProjectIdToInfo.TryGetValue(project.Id, out var projectInfo) &&
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
