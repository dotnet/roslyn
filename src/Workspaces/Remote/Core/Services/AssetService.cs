// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
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

        // PREVIEW: unfortunately, I need dummy workspace since workspace services can be workspace specific
        private static readonly Serializer s_serializer = new Serializer(new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: "dummy").Services);

        private readonly ConcurrentDictionary<int, AssetSource> _assetSources =
            new ConcurrentDictionary<int, AssetSource>(concurrencyLevel: 4, capacity: 10);

        private readonly ConcurrentDictionary<Checksum, Tuple<DateTime, object>> _assets =
            new ConcurrentDictionary<Checksum, Tuple<DateTime, object>>(concurrencyLevel: 4, capacity: 10);

        public AssetService()
        {
            Task.Factory.StartNew(CleanAssets, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        private async Task CleanAssets()
        {
            while (true)
            {
                foreach (var kvp in _assets)
                {
                    if (DateTime.UtcNow - kvp.Value.Item1 <= TimeSpan.FromMinutes(PurgeAfter))
                    {
                        continue;
                    }

                    Tuple<DateTime, object> value;
                    // If it fails, we'll just leave it in the asset pool.
                    _assets.TryRemove(kvp.Key, out value);
                }

                await Task.Delay(TimeSpan.FromMinutes(CleanupInterval).Milliseconds).ConfigureAwait(false);
            }
        }

        public void Set(Checksum checksum, object value)
        {
            var tuple = Tuple.Create(DateTime.UtcNow, value);
            _assets.AddOrUpdate(checksum, tuple, (key, oldValue) =>
            {
                Contract.Assert(ReferenceEquals(value, oldValue.Item2));
                return tuple;
            });
        }

        public async Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            Tuple<DateTime, object> tuple;
            if (!_assets.TryGetValue(checksum, out tuple))
            {
                // TODO: what happen if service doesn't come back. timeout?
                await RequestAssetAsync(checksum, cancellationToken).ConfigureAwait(false);

                if (!_assets.TryGetValue(checksum, out tuple))
                {
                    // this can happen if all asset source is released due to cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    Contract.Fail("how this can happen?");
                }
            }

            // Update timestamp
            Set(checksum, tuple.Item2);
            return (T)tuple.Item2;
        }

        public async Task RequestAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
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
                    await source.RequestAssetAsync(serviceId, checksum, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // connection to the asset source has closed.
                    // move to next asset source
                    Contract.ThrowIfFalse(ex is OperationCanceledException || ex is IOException || ex is ObjectDisposedException);

                    // cancellation could be from either caller or asset side when cancelled. we only throw cancellation if
                    // caller side is cancelled. otherwise, we move to next asset source
                    cancellationToken.ThrowIfCancellationRequested();

                    continue;
                }

                break;
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
    }
}

