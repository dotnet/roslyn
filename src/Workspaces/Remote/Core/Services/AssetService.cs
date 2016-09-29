// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
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
        private const int CleanupInterval = 3; // 3 minutes
        private const int PurgeAfter = 30; // 30 minutes

        private static readonly TimeSpan s_purgeAfterTimeSpan = TimeSpan.FromMinutes(PurgeAfter);
        private static readonly TimeSpan s_cleanupIntervalTimeSpan = TimeSpan.FromMinutes(CleanupInterval);

        // PREVIEW: unfortunately, I need dummy workspace since workspace services can be workspace specific
        private static readonly Serializer s_serializer = new Serializer(new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: "dummy").Services);

        private readonly ConcurrentDictionary<int, AssetSource> _assetSources =
            new ConcurrentDictionary<int, AssetSource>(concurrencyLevel: 4, capacity: 10);

        private readonly ConcurrentDictionary<Checksum, Entry> _assets =
            new ConcurrentDictionary<Checksum, Entry>(concurrencyLevel: 4, capacity: 10);

        public AssetService()
        {
            Task.Run(CleanAssetsAsync, CancellationToken.None);
        }

        private async Task CleanAssetsAsync()
        {
            while (true)
            {
                CleanAssets();

                await Task.Delay(s_cleanupIntervalTimeSpan).ConfigureAwait(false);
            }
        }

        private void CleanAssets()
        {
            var current = DateTime.UtcNow;

            using (Logger.LogBlock(FunctionId.AssetService_CleanAssets, CancellationToken.None))
            {
                foreach (var kvp in _assets.ToArray())
                {
                    if (current - kvp.Value.LastAccessed <= s_purgeAfterTimeSpan)
                    {
                        continue;
                    }

                    // If it fails, we'll just leave it in the asset pool.
                    Entry entry;
                    _assets.TryRemove(kvp.Key, out entry);
                }
            }
        }

        public T Deserialize<T>(string kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            return s_serializer.Deserialize<T>(kind, reader, cancellationToken);
        }

        public void RegisterAssetSource(int serviceId, AssetSource assetSource)
        {
            Contract.ThrowIfFalse(_assetSources.TryAdd(serviceId, assetSource));
        }

        public void UnregisterAssetSource(int serviceId)
        {
            AssetSource dummy;
            _assetSources.TryRemove(serviceId, out dummy);
        }

        public async Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            T asset;
            if (TryGetAsset(checksum, out asset))
            {
                return asset;
            }

            using (Logger.LogBlock(FunctionId.AssetService_GetAssetAsync, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
            {
                // TODO: what happen if service doesn't come back. timeout?
                var value = await RequestAssetAsync(checksum, cancellationToken).ConfigureAwait(false);

                _assets.TryAdd(checksum, new Entry(value));

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
            return TryGetAsset(checksum, out unused);
        }

        private bool TryGetAsset<T>(Checksum checksum, out T value)
        {
            value = default(T);
            using (Logger.LogBlock(FunctionId.AssetService_TryGetAsset, Checksum.GetChecksumLogInfo, checksum, CancellationToken.None))
            {
                Entry entry;
                if (!_assets.TryGetValue(checksum, out entry))
                {
                    return false;
                }

                // Update timestamp
                Update(checksum, entry);

                value = (T)entry.Object;
                return true;
            }
        }

        private async Task SynchronizeAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.AssetService_SynchronizeAssetsAsync, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
            {
                var values = await RequestAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);

                foreach (var tuple in values)
                {
                    _assets.TryAdd(tuple.Item1, new Entry(tuple.Item2));
                }
            }
        }

        private void Update(Checksum checksum, Entry entry)
        {
            // entry is reference type. we update it directly. 
            // we don't care about race.
            entry.LastAccessed = DateTime.UtcNow;
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

            // the service doesn't care which asset source it uses to get the asset. if there are multiple
            // channel created (multiple caller to code analysis service), we will have multiple asset sources
            // 
            // but, there must be one that knows about the asset with the checksum that is pinned for this
            //      particular call
            foreach (var kv in _assetSources.ToArray())
            {
                var serviceId = kv.Key;
                var source = kv.Value;

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // ask one of asset source for data
                    return await source.RequestAssetsAsync(serviceId, checksums, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsExpected(ex))
                {
                    // cancellation could be from either caller or asset side when cancelled. we only throw cancellation if
                    // caller side is cancelled. otherwise, we move to next asset source
                    cancellationToken.ThrowIfCancellationRequested();

                    // connection to the asset source has closed.
                    // move to next asset source
                    continue;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        private bool IsExpected(Exception ex)
        {
            // these exception can happen if either operation is cancelled, connection to asset source is closed
            return ex is OperationCanceledException || ex is IOException || ex is ObjectDisposedException;
        }

        private class Entry
        {
            // mutable field
            public DateTime LastAccessed;

            // this can't change for same checksum
            public readonly object Object;

            public Entry(object @object)
            {
                LastAccessed = DateTime.UtcNow;
                Object = @object;
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
                var solutionChecksumObject = await _assetService.GetAssetAsync<SolutionChecksumObject>(solutionChecksum, cancellationToken).ConfigureAwait(false);

                // second, get direct children of the solution
                await SynchronizeSolutionAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);

                // third, get direct children for all projects in the solution
                await SynchronizeProjectsAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);

                // last, get direct children for all documents in the solution
                await SynchronizeDocumentsAsync(solutionChecksumObject, cancellationToken).ConfigureAwait(false);
            }

            private async Task SynchronizeSolutionAsync(SolutionChecksumObject solutionChecksumObject, CancellationToken cancellationToken)
            {
                // get children of solution checksum object at once
                using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
                {
                    var solutionChecksums = pooledObject.Object;

                    AddIfNeeded(solutionChecksums, solutionChecksumObject.Children);
                    await _assetService.SynchronizeAssetsAsync(solutionChecksums, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task SynchronizeProjectsAsync(SolutionChecksumObject solutionChecksumObject, CancellationToken cancellationToken)
            {
                // get children of project checksum objects at once
                using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
                {
                    var projectChecksums = pooledObject.Object;

                    foreach (var projectChecksum in solutionChecksumObject.Projects)
                    {
                        var projectChecksumObject = await _assetService.GetAssetAsync<ProjectChecksumObject>(projectChecksum, cancellationToken).ConfigureAwait(false);
                        AddIfNeeded(projectChecksums, projectChecksumObject.Children);
                    }

                    await _assetService.SynchronizeAssetsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task SynchronizeDocumentsAsync(SolutionChecksumObject solutionChecksumObject, CancellationToken cancellationToken)
            {
                // get children of document checksum objects at once
                using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
                {
                    var documentChecksums = pooledObject.Object;

                    foreach (var projectChecksum in solutionChecksumObject.Projects)
                    {
                        var projectChecksumObject = await _assetService.GetAssetAsync<ProjectChecksumObject>(projectChecksum, cancellationToken).ConfigureAwait(false);

                        foreach (var checksum in projectChecksumObject.Documents)
                        {
                            var documentChecksumObject = await _assetService.GetAssetAsync<DocumentChecksumObject>(checksum, cancellationToken).ConfigureAwait(false);
                            AddIfNeeded(documentChecksums, documentChecksumObject.Children);
                        }

                        foreach (var checksum in projectChecksumObject.AdditionalDocuments)
                        {
                            var documentChecksumObject = await _assetService.GetAssetAsync<DocumentChecksumObject>(checksum, cancellationToken).ConfigureAwait(false);
                            AddIfNeeded(documentChecksums, documentChecksumObject.Children);
                        }
                    }

                    await _assetService.SynchronizeAssetsAsync(documentChecksums, cancellationToken).ConfigureAwait(false);
                }
            }

            private void AddIfNeeded(HashSet<Checksum> checksums, object[] checksumOrCollections)
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

