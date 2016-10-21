// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This service provide a way to get roslyn objects from checksum
    /// 
    /// TODO: change this service to workspace service
    /// </summary>
    internal class AssetStorage
    {
        public static readonly AssetStorage Default = new AssetStorage();

        private const int CleanupInterval = 3; // 3 minutes
        private const int PurgeAfter = 30; // 30 minutes

        private static readonly TimeSpan s_purgeAfterTimeSpan = TimeSpan.FromMinutes(PurgeAfter);
        private static readonly TimeSpan s_cleanupIntervalTimeSpan = TimeSpan.FromMinutes(CleanupInterval);

        private readonly ConcurrentDictionary<int, AssetSource> _assetSources =
            new ConcurrentDictionary<int, AssetSource>(concurrencyLevel: 4, capacity: 10);

        private readonly ConcurrentDictionary<Checksum, Entry> _assets =
            new ConcurrentDictionary<Checksum, Entry>(concurrencyLevel: 4, capacity: 10);

        public AssetStorage()
        {
            Task.Run(CleanAssetsAsync, CancellationToken.None);
        }

        public AssetSource GetAssetSource(int sessionId)
        {
            return _assetSources[sessionId];
        }

        public void RegisterAssetSource(int sessionId, AssetSource assetSource)
        {
            Contract.ThrowIfFalse(_assetSources.TryAdd(sessionId, assetSource));
        }

        public void UnregisterAssetSource(int sessionId)
        {
            AssetSource dummy;
            _assetSources.TryRemove(sessionId, out dummy);
        }

        public bool TryAddAsset(Checksum checksum, object value)
        {
            return _assets.TryAdd(checksum, new Entry(value));
        }

        public bool TryGetAsset<T>(Checksum checksum, out T value)
        {
            value = default(T);
            using (Logger.LogBlock(FunctionId.AssetStorage_TryGetAsset, Checksum.GetChecksumLogInfo, checksum, CancellationToken.None))
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

        private void Update(Checksum checksum, Entry entry)
        {
            // entry is reference type. we update it directly. 
            // we don't care about race.
            entry.LastAccessed = DateTime.UtcNow;
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

            using (Logger.LogBlock(FunctionId.AssetStorage_CleanAssets, CancellationToken.None))
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

