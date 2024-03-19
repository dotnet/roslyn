// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class AssetProvider
{
    private readonly struct ChecksumSynchronizer(AssetProvider assetProvider)
    {
        // make sure there is always only 1 bulk synchronization
        private static readonly SemaphoreSlim s_gate = new(initialCount: 1);

        private readonly AssetProvider _assetProvider = assetProvider;

        public async ValueTask SynchronizeAssetsAsync(
            AssetHint assetHint,
            HashSet<Checksum> checksums,
            Dictionary<Checksum, object> results,
            CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await _assetProvider.SynchronizeAssetsAsync(assetHint, checksums, results, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            SolutionStateChecksums solutionChecksumObject;
            using var _1 = PooledDictionary<Checksum, object>.GetInstance(out var map);

            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // this will make 4 round trip to data source (VS) to get all assets that belong to the given solution checksum

                // first, get solution checksum object for the given solution checksum
                var solutionCompilationChecksumObject = await _assetProvider.GetAssetAsync<SolutionCompilationStateChecksums>(
                    assetHint: AssetHint.None, solutionChecksum, cancellationToken).ConfigureAwait(false);
                solutionChecksumObject = await _assetProvider.GetAssetAsync<SolutionStateChecksums>(
                    assetHint: AssetHint.None, solutionCompilationChecksumObject.SolutionState, cancellationToken).ConfigureAwait(false);

                // second, get direct children of the solution
                {
                    using var _2 = PooledHashSet<Checksum>.GetInstance(out var checksums);

                    solutionChecksumObject.AddAllTo(checksums);
                    checksums.Remove(solutionChecksumObject.Checksum);
                    await _assetProvider.SynchronizeAssetsAsync(assetHint: AssetHint.None, checksums, map, cancellationToken).ConfigureAwait(false);
                }
            }

            // third and last get direct children for all projects and documents in the solution 
            foreach (var project in solutionChecksumObject.Projects)
            {
                var projectStateChecksums = (ProjectStateChecksums)map[project.checksum];
                await SynchronizeProjectAssetsAsync(projectStateChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        public async ValueTask SynchronizeProjectAssetsAsync(ProjectStateChecksums projectChecksum, CancellationToken cancellationToken)
        {
            using (await s_gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                await SynchronizeProjectAssets_NoLockAsync(projectChecksum, cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask SynchronizeProjectAssets_NoLockAsync(ProjectStateChecksums projectChecksum, CancellationToken cancellationToken)
        {
            // get children of project checksum objects at once
            using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksums);
            using var _2 = PooledDictionary<Checksum, object>.GetInstance(out var map);

            checksums.Add(projectChecksum.Info);
            checksums.Add(projectChecksum.CompilationOptions);
            checksums.Add(projectChecksum.ParseOptions);
            AddAll(checksums, projectChecksum.ProjectReferences);
            AddAll(checksums, projectChecksum.MetadataReferences);
            AddAll(checksums, projectChecksum.AnalyzerReferences);
            AddAll(checksums, projectChecksum.Documents.Checksums);
            AddAll(checksums, projectChecksum.AdditionalDocuments.Checksums);
            AddAll(checksums, projectChecksum.AnalyzerConfigDocuments.Checksums);

            await _assetProvider.SynchronizeAssetsAsync(
                assetHint: projectChecksum.ProjectId, checksums, map, cancellationToken).ConfigureAwait(false);

            checksums.Clear();

            CollectChecksumChildren(projectChecksum.Documents.Checksums);
            CollectChecksumChildren(projectChecksum.AdditionalDocuments.Checksums);
            CollectChecksumChildren(projectChecksum.AnalyzerConfigDocuments.Checksums);

            await _assetProvider.SynchronizeAssetsAsync(
                assetHint: projectChecksum.ProjectId, checksums, map, cancellationToken).ConfigureAwait(false);

            void CollectChecksumChildren(ChecksumCollection collection)
            {
                foreach (var checksum in collection)
                {
                    // These DocumentStateChecksums must be here due to the synchronizing step that just happened above. 
                    var checksumObject = (DocumentStateChecksums)map[checksum];
                    checksums.Add(checksumObject.Info);
                    checksums.Add(checksumObject.Text);
                }
            }
        }

        private static void AddAll(HashSet<Checksum> checksums, ChecksumCollection checksumCollection)
        {
            foreach (var checksum in checksumCollection)
                checksums.Add(checksum);
        }
    }
}
