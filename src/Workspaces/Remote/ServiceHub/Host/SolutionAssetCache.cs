// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class SolutionAssetCache
    {
        /// <summary>
        /// Time interval we check storage for cleanup
        /// </summary>
        private readonly TimeSpan _cleanupIntervalTimeSpan;

        /// <summary>
        /// Time span data can sit inside of cache (<see cref="_assets"/>) without being used.
        /// after that, it will be removed from the cache.
        /// </summary>
        private readonly TimeSpan _purgeAfterTimeSpan;

        /// <summary>
        /// Time we will wait after the last activity before doing explicit GC cleanup.
        /// We monitor all resource access and service call to track last activity time.
        /// 
        /// We do this since 64bit process can hold onto quite big unused memory when
        /// OOP is running as AnyCpu
        /// </summary>
        private readonly TimeSpan _gcAfterTimeSpan;

        private readonly ConcurrentDictionary<Checksum, Entry> _assets =
            new ConcurrentDictionary<Checksum, Entry>(concurrencyLevel: 4, capacity: 10);

        private DateTime _lastGCRun;
        private DateTime _lastActivityTime;

        // constructor for testing
        public SolutionAssetCache()
        {
        }

        /// <summary>
        /// Create central data cache
        /// </summary>
        /// <param name="cleanupInterval">time interval to clean up</param>
        /// <param name="purgeAfter">time unused data can sit in the cache</param>
        /// <param name="gcAfter">time we wait before it call GC since last activity</param>
        public SolutionAssetCache(TimeSpan cleanupInterval, TimeSpan purgeAfter, TimeSpan gcAfter)
        {
            _cleanupIntervalTimeSpan = cleanupInterval;
            _purgeAfterTimeSpan = purgeAfter;
            _gcAfterTimeSpan = gcAfter;

            _lastActivityTime = DateTime.UtcNow;
            _lastGCRun = DateTime.UtcNow;

            Task.Run(CleanAssetsAsync, CancellationToken.None);
        }

        public bool TryAddAsset(Checksum checksum, object value)
        {
            UpdateLastActivityTime();

            return _assets.TryAdd(checksum, new Entry(value));
        }

        public bool TryGetAsset<T>(Checksum checksum, [MaybeNullWhen(false)] out T value)
        {
            UpdateLastActivityTime();

            using (Logger.LogBlock(FunctionId.AssetStorage_TryGetAsset, Checksum.GetChecksumLogInfo, checksum, CancellationToken.None))
            {
                if (!_assets.TryGetValue(checksum, out var entry))
                {
                    value = default;
                    return false;
                }

                // Update timestamp
                Update(entry);

                value = (T)entry.Object;
                return true;
            }
        }

        public void UpdateLastActivityTime()
            => _lastActivityTime = DateTime.UtcNow;

        private static void Update(Entry entry)
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

                ForceGC();

                await Task.Delay(_cleanupIntervalTimeSpan).ConfigureAwait(false);
            }
        }

        private void ForceGC()
        {
            // if there was no activity since last GC run. we don't have anything to do
            if (_lastGCRun >= _lastActivityTime)
            {
                return;
            }

            var current = DateTime.UtcNow;
            if (current - _lastActivityTime < _gcAfterTimeSpan)
            {
                // we are having activities.
                return;
            }

            using (Logger.LogBlock(FunctionId.AssetStorage_ForceGC, CancellationToken.None))
            {
                // we didn't have activity for 5 min. spend some time to drop 
                // unused memory
                for (var i = 0; i < 3; i++)
                {
                    GC.Collect();
                }
            }

            // update gc run time
            _lastGCRun = current;
        }

        private void CleanAssets()
        {
            if (_assets.Count == 0)
            {
                // no asset, nothing to do.
                return;
            }

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
                    _assets.TryRemove(kvp.Key, out var _);
                }
            }
        }

        private sealed class Entry
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

