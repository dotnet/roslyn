// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    internal class AssetService : IAssetProvider
    {
        private readonly ISerializerService _serializerService;
        private readonly int _scopeId;
        private readonly AssetStorage _assetStorage;

        public AssetService(int scopeId, AssetStorage assetStorage, ISerializerService serializerService)
        {
            _scopeId = scopeId;
            _assetStorage = assetStorage;
            _serializerService = serializerService;
        }

        public IEnumerable<T> GetGlobalAssetsOfType<T>(CancellationToken cancellationToken)
        {
            return _assetStorage.GetGlobalAssetsOfType<T>(cancellationToken);
        }

        public async Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            if (_assetStorage.TryGetAsset(checksum, out T asset))
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
            var syncer = new ChecksumSynchronizer(this);
            await syncer.SynchronizeAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);

            var list = new List<ValueTuple<Checksum, T>>();
            foreach (var checksum in checksums)
            {
                list.Add(ValueTuple.Create(checksum, await GetAssetAsync<T>(checksum, cancellationToken).ConfigureAwait(false)));
            }

            return list;
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

        public async Task SynchronizeProjectAssetsAsync(IEnumerable<Checksum> projectChecksums, CancellationToken cancellationToken)
        {
            // this will pull in assets that belong to the given project checksum to this remote host.
            // this one is not supposed to be used for functionality but only for perf. that is why it doesn't return anything.
            // to get actual data GetAssetAsync should be used. and that will return actual data and if there is any missing data in cache, GetAssetAsync
            // itself will bring that data in from data source (VS)

            // one can call this method to make cache hot for all assets that belong to the project checksum so that GetAssetAsync call will most likely cache hit.
            // it is most likely since we might change cache hueristic in future which make data to live a lot shorter in the cache, and the data might get expired
            // before one actually consume the data. 
            using (Logger.LogBlock(FunctionId.AssetService_SynchronizeProjectAssetsAsync, Checksum.GetChecksumsLogInfo, projectChecksums, cancellationToken))
            {
                var syncer = new ChecksumSynchronizer(this);
                await syncer.SynchronizeProjectAssetsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);
            }
        }

        public bool EnsureCacheEntryIfExists(Checksum checksum)
        {
            // this will check whether checksum exists in the cache and if it does,
            // it will touch the entry so that it doesn't expire right after we checked it.
            //
            // even if it got expired after this for whatever reason, functionality wise everything will still work, 
            // just perf will be impacted since we will fetch it from data source (VS)
            return _assetStorage.TryGetAsset(checksum, out object unused);
        }

        public async Task SynchronizeAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
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
            using var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject();
            pooledObject.Object.Add(checksum);

            var tuple = await RequestAssetsAsync(pooledObject.Object, cancellationToken).ConfigureAwait(false);
            return tuple[0].Item2;
        }

        private async Task<IList<ValueTuple<Checksum, object>>> RequestAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            if (checksums.Count == 0)
            {
                return SpecializedCollections.EmptyList<ValueTuple<Checksum, object>>();
            }

            var source = _assetStorage.AssetSource;
            cancellationToken.ThrowIfCancellationRequested();

            Contract.ThrowIfNull(source);

            try
            {
                // ask one of asset source for data
                return await source.RequestAssetsAsync(_scopeId, checksums, _serializerService, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // object disposed exception can happen if StreamJsonRpc get disconnected
                // in the middle of read/write due to cancellation
                cancellationToken.ThrowIfCancellationRequested();
                throw;
            }
        }
    }
}
