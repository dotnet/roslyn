// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This service provide a way to get roslyn objects from checksum
    /// </summary>
    internal sealed class AssetProvider : AbstractAssetProvider
    {
        private readonly Checksum _solutionChecksum;
        private readonly ISerializerService _serializerService;
        private readonly SolutionAssetCache _assetCache;
        private readonly IAssetSource _assetSource;

        public AssetProvider(Checksum solutionChecksum, SolutionAssetCache assetCache, IAssetSource assetSource, ISerializerService serializerService)
        {
            _solutionChecksum = solutionChecksum;
            _assetCache = assetCache;
            _assetSource = assetSource;
            _serializerService = serializerService;
        }

        public override async Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            Debug.Assert(checksum != Checksum.Null);

            if (_assetCache.TryGetAsset<T>(checksum, out var asset))
            {
                return asset;
            }

            using (Logger.LogBlock(FunctionId.AssetService_GetAssetAsync, Checksum.GetChecksumLogInfo, checksum, cancellationToken))
            {
                // TODO: what happen if service doesn't come back. timeout?
                var value = await RequestAssetAsync(checksum, cancellationToken).ConfigureAwait(false);

                _assetCache.TryAddAsset(checksum, value);
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
            var timer = new Stopwatch();
            timer.Start();

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

            timer.Stop();

            // report telemetry to help correlate slow solution sync with UI delays
            if (timer.ElapsedMilliseconds > 1000)
            {
                Logger.Log(FunctionId.AssetService_Perf, KeyValueLogMessage.Create(map => map["SolutionSyncTime"] = timer.ElapsedMilliseconds));
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
            return _assetCache.TryGetAsset<object>(checksum, out _);
        }

        public async Task SynchronizeAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            Debug.Assert(!checksums.Contains(Checksum.Null));

            using (Logger.LogBlock(FunctionId.AssetService_SynchronizeAssetsAsync, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
            {
                var assets = await RequestAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);

                foreach (var (checksum, value) in assets)
                {
                    _assetCache.TryAddAsset(checksum, value);
                }
            }
        }

        private async Task<object> RequestAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            Debug.Assert(checksum != Checksum.Null);

            using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);
            checksums.Add(checksum);

            var assets = await RequestAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);
            return assets.Single().value;
        }

        private async Task<ImmutableArray<(Checksum checksum, object value)>> RequestAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            Debug.Assert(!checksums.Contains(Checksum.Null));

            if (checksums.Count == 0)
            {
                return ImmutableArray<(Checksum, object)>.Empty;
            }

            return await _assetSource.GetAssetsAsync(_solutionChecksum, checksums, _serializerService, cancellationToken).ConfigureAwait(false);
        }
    }
}
