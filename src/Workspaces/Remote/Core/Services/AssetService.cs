// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
            using (Logger.LogBlock(FunctionId.AssetService_GetAssetAsync, GetChecksumLogInfo, checksum, cancellationToken))
            {
                Entry entry;
                if (!_assets.TryGetValue(checksum, out entry))
                {
                    // TODO: what happen if service doesn't come back. timeout?
                    var value = await RequestAssetAsync(checksum, cancellationToken).ConfigureAwait(false);

                    _assets.TryAdd(checksum, new Entry(value));

                    return (T)value;
                }

                // Update timestamp
                Update(checksum, entry);

                return (T)entry.Object;
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
                    return await source.RequestAssetAsync(serviceId, checksum, cancellationToken).ConfigureAwait(false);
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

        private static string GetChecksumLogInfo(Checksum checksum)
        {
            return checksum.ToString();
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
    }
}

