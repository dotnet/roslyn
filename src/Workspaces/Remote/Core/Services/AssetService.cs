// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This service provide a way to get roslyn objects from checksum
    /// 
    /// TODO: change this service to workspace service
    /// </summary>
    internal class AssetService
    {
        // PREVIEW: unfortunately, I need dummy workspace since workspace services can be workspace specific
        private static readonly Serializer s_serializer = new Serializer(new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: "dummy").Services);

        private readonly int _sessionId;
        private readonly AssetStorage _assetStorage;

        public AssetService(int sessionId, AssetStorage assetStorage)
        {
            _sessionId = sessionId;
            _assetStorage = assetStorage;
        }

        public T Deserialize<T>(string kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            return s_serializer.Deserialize<T>(kind, reader, cancellationToken);
        }

        public async Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            T asset;
            if (_assetStorage.TryGetAsset(checksum, out asset))
            {
                return asset;
            }

            using (Logger.LogBlock(FunctionId.AssetService_GetAssetAsync, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
            {
                // TODO: what happen if service doesn't come back. timeout?
                var value = await RequestAssetAsync(checksum, cancellationToken).ConfigureAwait(false);

                _assetStorage.TryAddAsset(checksum, value);
                return (T)value;
            }
        }

        public async Task<List<ValueTuple<Checksum, T>>> GetAssetsAsync<T>(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            // this only works when caller wants to get same kind of assets at once

            // bulk synchronize checksums first
            await SynchronizeAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);

            var list = new List<ValueTuple<Checksum, T>>();
            foreach (var checksum in checksums)
            {
                list.Add(ValueTuple.Create(checksum, await GetAssetAsync<T>(checksum, cancellationToken).ConfigureAwait(false)));
            }

            return list;
        }

        public async Task SynchronizeAssetsAsync(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
        {
            // if caller wants to get different kind of assets at once, he needs to first sync asserts and use
            // GetAssetAsync to get those.
            var syncer = new ChecksumSynchronizer(this);
            await syncer.SynchronizeAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);
        }

        public async Task SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            // this will pull in assets that belong to the given solution checksum to this remote host.
            // this one is not supposed to be used for functionality but only for perf. that is why it doesn't return anything.
            // to get actual data GetAssetAsync should be used. and that will return actual data and if there is any missing data in cache, GetAssetAsync
            // itself will bring that data in from data source (VS)

            // one can call this method to make cache hot for all assets that belong to the solution checksum so that GetAssetAsync call will most likely cache hit.
            // it is most likely since we might change cache hueristic in future which make data to live a lot shorter in the cache, and the data might get expired
            // before one actually consume the data. 
            using (Logger.LogBlock(FunctionId.AssetService_SynchronizeSolutionAssetsAsync, Checksum.GetChecksumLogInfo, solutionChecksum, cancellationToken))
            {
                var syncer = new ChecksumSynchronizer(this);
                await syncer.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool EnsureCacheEntryIfExists(Checksum checksum)
        {
            // this will check whether checksum exists in the cache and if it does,
            // it will touch the entry so that it doesn't expire right after we checked it.
            //
            // even if it got expired after this for whatever reason, functionality wise everything will still work, 
            // just perf will be impacted since we will fetch it from data source (VS)
            object unused;
            return _assetStorage.TryGetAsset(checksum, out unused);
        }

        private async Task SynchronizeAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.AssetService_SynchronizeAssetsAsync, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
            {
                var values = await RequestAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);

                foreach (var tuple in values)
                {
                    _assetStorage.TryAddAsset(tuple.Item1, tuple.Item2);
                }
            }
        }

        private async Task<object> RequestAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                pooledObject.Object.Add(checksum);

                var tuple = await RequestAssetsAsync(pooledObject.Object, cancellationToken).ConfigureAwait(false);
                return tuple[0].Item2;
            }
        }

        private async Task<IList<ValueTuple<Checksum, object>>> RequestAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            if (checksums.Count == 0)
            {
                return SpecializedCollections.EmptyList<ValueTuple<Checksum, object>>();
            }

            var source = _assetStorage.GetAssetSource(_sessionId);

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // ask one of asset source for data
                return await source.RequestAssetsAsync(_sessionId, checksums, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can happen if StreamJsonRpc get disconnected
                // in the middle of read/write due to cancellation
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }

        private struct ChecksumSynchronizer
        {
            private readonly AssetService _assetService;

            public ChecksumSynchronizer(AssetService assetService)
            {
                _assetService = assetService;
            }

            public async Task SynchronizeAssetsAsync(IEnumerable<Checksum> checksums, CancellationToken cancellationToken)
            {
                using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
                {
                    AddIfNeeded(pooledObject.Object, checksums);
                    await _assetService.SynchronizeAssetsAsync(pooledObject.Object, cancellationToken).ConfigureAwait(false);
                }
            }

            public async Task SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
            {
                // this will make 4 round trip to data source (VS) to get all assets that belong to the given solution checksum

                // first, get solution checksum object for the given solution checksum
                var solutionChecksumObject = await _assetService.GetAssetAsync<SolutionStateChecksums>(solutionChecksum, cancellationToken).ConfigureAwait(false);

                // second, get direct children of the solution
                await SynchronizeSolutionAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);

                // third, get direct children for all projects in the solution
                await SynchronizeProjectsAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);

                // last, get direct children for all documents in the solution
                await SynchronizeDocumentsAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);
            }

            private async Task SynchronizeSolutionAsync(SolutionStateChecksums solutionChecksumObject, CancellationToken cancellationToken)
            {
                // get children of solution checksum object at once
                using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
                {
                    var solutionChecksums = pooledObject.Object;

                    AddIfNeeded(solutionChecksums, solutionChecksumObject.Children);
                    await _assetService.SynchronizeAssetsAsync(solutionChecksums, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task SynchronizeProjectsAsync(SolutionStateChecksums solutionChecksumObject, CancellationToken cancellationToken)
            {
                // get children of project checksum objects at once
                using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
                {
                    var projectChecksums = pooledObject.Object;

                    foreach (var projectChecksum in solutionChecksumObject.Projects)
                    {
                        var projectChecksumObject = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);
                        AddIfNeeded(projectChecksums, projectChecksumObject.Children);
                    }

                    await _assetService.SynchronizeAssetsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task SynchronizeDocumentsAsync(SolutionStateChecksums solutionChecksumObject, CancellationToken cancellationToken)
            {
                // get children of document checksum objects at once
                using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
                {
                    var documentChecksums = pooledObject.Object;

                    foreach (var projectChecksum in solutionChecksumObject.Projects)
                    {
                        var projectChecksumObject = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);

                        foreach (var checksum in projectChecksumObject.Documents)
                        {
                            var documentChecksumObject = await _assetService.GetAssetAsync<DocumentStateChecksums>(checksum, cancellationToken).ConfigureAwait(false);
                            AddIfNeeded(documentChecksums, documentChecksumObject.Children);
                        }

                        foreach (var checksum in projectChecksumObject.AdditionalDocuments)
                        {
                            var documentChecksumObject = await _assetService.GetAssetAsync<DocumentStateChecksums>(checksum, cancellationToken).ConfigureAwait(false);
                            AddIfNeeded(documentChecksums, documentChecksumObject.Children);
                        }
                    }

                    await _assetService.SynchronizeAssetsAsync(documentChecksums, cancellationToken).ConfigureAwait(false);
                }
            }

            private void AddIfNeeded(HashSet<Checksum> checksums, IReadOnlyList<object> checksumOrCollections)
            {
                foreach (var checksumOrCollection in checksumOrCollections)
                {
                    var checksum = checksumOrCollection as Checksum;
                    if (checksum != null)
                    {
                        AddIfNeeded(checksums, checksum);
                        continue;
                    }

                    var checksumCollection = checksumOrCollection as ChecksumCollection;
                    if (checksumCollection != null)
                    {
                        AddIfNeeded(checksums, checksumCollection);
                        continue;
                    }

                    throw ExceptionUtilities.UnexpectedValue(checksumOrCollection);
                }
            }

            private void AddIfNeeded(HashSet<Checksum> checksums, IEnumerable<Checksum> collection)
            {
                foreach (var checksum in collection)
                {
                    AddIfNeeded(checksums, checksum);
                }
            }

            private void AddIfNeeded(HashSet<Checksum> checksums, Checksum checksum)
            {
                if (!_assetService.EnsureCacheEntryIfExists(checksum))
                {
                    checksums.Add(checksum);
                }
            }
        }
    }
}