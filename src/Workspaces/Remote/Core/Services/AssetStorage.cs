// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        // TODO: think of a way to use roslyn option service in OOP
        public static readonly AssetStorage Default = new AssetStorage(cleanupInterval: TimeSpan.FromMinutes(1), purgeAfter: TimeSpan.FromMinutes(3));

        private readonly TimeSpan _cleanupIntervalTimeSpan;
        private readonly TimeSpan _purgeAfterTimeSpan;

        private readonly ConcurrentDictionary<Checksum, Entry> _globalAssets =
            new ConcurrentDictionary<Checksum, Entry>(concurrencyLevel: 4, capacity: 10);

        private readonly ConcurrentDictionary<Checksum, Entry> _assets =
            new ConcurrentDictionary<Checksum, Entry>(concurrencyLevel: 4, capacity: 10);

        private volatile AssetSource _assetSource;

        public AssetStorage()
        {
            // constructor for testing
        }

        public AssetStorage(TimeSpan cleanupInterval, TimeSpan purgeAfter)
        {
            _cleanupIntervalTimeSpan = cleanupInterval;
            _purgeAfterTimeSpan = purgeAfter;

            Task.Run(CleanAssetsAsync, CancellationToken.None);
        }

        public AssetSource AssetSource
        {
            get { return _assetSource; }
        }

        public void SetAssetSource(AssetSource assetSource)
        {
            _assetSource = assetSource;
        }

        public bool TryAddGlobalAsset(Checksum checksum, object value)
        {
            return _globalAssets.TryAdd(checksum, new Entry(value));
        }

        public bool TryAddAsset(Checksum checksum, object value)
        {
            return _assets.TryAdd(checksum, new Entry(value));
        }

        public IEnumerable<T> GetGlobalAssetsOfType<T>(CancellationToken cancellationToken)
        {
            foreach (var asset in _globalAssets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var value = asset.Value.Object;
                if (value is T tValue)
                {
                    yield return tValue;
                }
            }
        }

        public bool TryGetAsset<T>(Checksum checksum, out T value)
        {
            value = default(T);
            using (Logger.LogBlock(FunctionId.AssetStorage_TryGetAsset, Checksum.GetChecksumLogInfo, checksum, CancellationToken.None))
            {
                Entry entry;
                if (!_globalAssets.TryGetValue(checksum, out entry) &&
                    !_assets.TryGetValue(checksum, out entry))
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

                await Task.Delay(_cleanupIntervalTimeSpan).ConfigureAwait(false);
            }
        }

        private void CleanAssets()
        {
            var current = DateTime.UtcNow;

            using (Logger.LogBlock(FunctionId.AssetStorage_CleanAssets, CancellationToken.None))
            {
                foreach (var kvp in _assets.ToArray())
                {
                    if (current - kvp.Value.LastAccessed <= _purgeAfterTimeSpan)
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

