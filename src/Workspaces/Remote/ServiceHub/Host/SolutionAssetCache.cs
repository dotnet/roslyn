// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class SolutionAssetCache
    {
        static SolutionAssetCache()
        {
            // CRITICAL: The size SharedStopwatch is the size of a TimeSpan (which itself is the size of a long).  This
            // allows stopwatches to be atomically overwritten, without a concern for torn writes, as long as we're
            // running on 64bit machines.  Make sure this value doesn't change as that will cause these current
            // consumers to be invalid.
            RoslynDebug.Assert(Marshal.SizeOf(typeof(SharedStopwatch)) == 8);
        }

        /// <summary>
        /// Workspace we are associated with.  When we purge items from teh cache, we will avoid any items associated
        /// with the items in its 'CurrentSolution'.
        /// </summary>
        private readonly RemoteWorkspace? _remoteWorkspace;

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

        private readonly ConcurrentDictionary<Checksum, Entry> _assets = new(concurrencyLevel: 4, capacity: 10);

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
        public SolutionAssetCache(RemoteWorkspace? remoteWorkspace, TimeSpan cleanupInterval, TimeSpan purgeAfter, TimeSpan gcAfter)
        {
            _remoteWorkspace = remoteWorkspace;
            _cleanupIntervalTimeSpan = cleanupInterval;
            _purgeAfterTimeSpan = purgeAfter;
            _gcAfterTimeSpan = gcAfter;

            _lastActivityTime = DateTime.UtcNow;
            _lastGCRun = DateTime.UtcNow;

            Task.Run(CleanAssetsAsync, CancellationToken.None);
        }

        public object GetOrAdd(Checksum checksum, object value)
        {
            UpdateLastActivityTime();

            var entry = _assets.GetOrAdd(checksum, new Entry(value));
            Update(entry);
            return entry.Object;
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
            // Stopwatch wraps a TimeSpan (which is only 64bits) (asserted in our shared constructor). so this
            // assignment can be done safely without a concern for torn writes on 64 systems.
            //
            // Note: on 32 bit systems there could be an issue here both with a torn write/read or torn write/write. We
            // think that's probably ok as a torn read only leads to suboptimal behavior (dropping something early, or
            // keeping something around till the next purge), and a torn write should likely still lead to reasonable
            // data being written as both writers will likely still write something reasonable once both writes go
            // through.  e.g. if you have a writer writing 1234-5678 and one writing 1235-0000, then getting 1235-5678
            // or 1234-0000 is still fine as a final outcome.
            entry.Stopwatch = SharedStopwatch.StartNew();
        }

        private async Task CleanAssetsAsync()
        {
            // Todo: associate this with a real CancellationToken that can shutdown this work.
            var cancellationToken = CancellationToken.None;
            while (!cancellationToken.IsCancellationRequested)
            {
                await CleanAssetsWorkerAsync(cancellationToken).ConfigureAwait(false);

                ForceGC();

                await Task.Delay(_cleanupIntervalTimeSpan, cancellationToken).ConfigureAwait(false);
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

        private async ValueTask CleanAssetsWorkerAsync(CancellationToken cancellationToken)
        {
            if (_assets.IsEmpty)
            {
                // no asset, nothing to do.
                return;
            }

            using (Logger.LogBlock(FunctionId.AssetStorage_CleanAssets, cancellationToken))
            {
                // Ensure that if our remote workspace has a current solution, that we don't purge any items associated
                // with that solution.
                PooledHashSet<Checksum>? pinnedChecksums = null;
                try
                {
                    foreach (var (checksum, entry) in _assets)
                    {
                        // If not enough time has passed, keep in the cache.
                        if (entry.Stopwatch.Elapsed <= _purgeAfterTimeSpan)
                            continue;

                        // If this is a checksum we want to pin, do not remove it.
                        if (pinnedChecksums == null)
                        {
                            pinnedChecksums = PooledHashSet<Checksum>.GetInstance();
                            await AddPinnedChecksumsAsync(pinnedChecksums, cancellationToken).ConfigureAwait(false);
                        }

                        if (pinnedChecksums.Contains(checksum))
                            continue;

                        _assets.TryRemove(checksum, out _);
                    }
                }
                finally
                {
                    pinnedChecksums?.Free();
                }
            }
        }

        private async ValueTask AddPinnedChecksumsAsync(HashSet<Checksum> pinnedChecksums, CancellationToken cancellationToken)
        {
            if (_remoteWorkspace is null)
                return;

            var checksums = await _remoteWorkspace.CurrentSolution.CompilationState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            checksums.AddAllTo(pinnedChecksums);
        }

        private sealed class Entry
        {
            public SharedStopwatch Stopwatch = SharedStopwatch.StartNew();

            // This can't change for same checksum
            public readonly object Object;

            public Entry(object @object)
            {
                Object = @object;
            }
        }
    }
}

