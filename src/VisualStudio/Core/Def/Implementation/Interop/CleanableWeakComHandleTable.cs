// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Interop
{
    /// <summary>
    /// Special collection for storing a table of COM objects weakly that provides
    /// logic for cleaning up dead references in a time-sliced way. Public members of this
    /// collection are affinitized to the foreground thread.
    /// </summary>
    internal class CleanableWeakComHandleTable<TKey, TValue> : ForegroundThreadAffinitizedObject
        where TValue : class
    {
        private const int DefaultCleanUpThreshold = 25;
        private static readonly TimeSpan s_defaultCleanUpTimeSlice = TimeSpan.FromMilliseconds(15);

        private readonly Dictionary<TKey, WeakComHandle<TValue, TValue>> _table;
        private readonly HashSet<TKey> _deadKeySet;

        /// <summary>
        /// The upper limit of items that the collection will store before clean up is recommended.
        /// </summary>
        public int CleanUpThreshold { get; }

        /// <summary>
        /// The amount of time that can pass during clean up it returns.
        /// </summary>
        public TimeSpan CleanUpTimeSlice { get; }

        private int _itemsAddedSinceLastCleanUp;
        private bool _needsCleanUp;

        public bool NeedsCleanUp => _needsCleanUp;

        public CleanableWeakComHandleTable(IThreadingContext threadingContext, int? cleanUpThreshold = null, TimeSpan? cleanUpTimeSlice = null)
            : base(threadingContext)
        {
            _table = new Dictionary<TKey, WeakComHandle<TValue, TValue>>();
            _deadKeySet = new HashSet<TKey>();

            CleanUpThreshold = cleanUpThreshold ?? DefaultCleanUpThreshold;
            CleanUpTimeSlice = cleanUpTimeSlice ?? s_defaultCleanUpTimeSlice;
        }

        /// <summary>
        /// Cleans up references to dead objects in the table. This operation will yield to other foreground operations
        /// any time execution exceeds <see cref="CleanUpTimeSlice"/>.
        /// </summary>
        public async Task CleanUpDeadObjectsAsync(IAsynchronousOperationListener listener)
        {
            using var _ = listener.BeginAsyncOperation(nameof(CleanUpDeadObjectsAsync));

            Debug.Assert(ThreadingContext.JoinableTaskContext.IsOnMainThread, "This method is optimized for cases where calls do not yield before checking _needsCleanUp.");

            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(ThreadingContext.DisposalToken);

            if (!_needsCleanUp)
            {
                return;
            }

            // Immediately mark as not needing cleanup; this operation will clean up the table by the time it returns.
            _needsCleanUp = false;

            var timeSlice = new TimeSlice(CleanUpTimeSlice);

            await CollectDeadKeysAsync().ConfigureAwait(true);
            await RemoveDeadKeysAsync().ConfigureAwait(true);
            return;

            // Local functions
            async Task CollectDeadKeysAsync()
            {
                // This method returns after making a complete pass enumerating the elements of _table without finding
                // any entries that are not alive. If a pass exceeds the allowed time slice after finding one or more
                // dead entries, the pass yields before processing the elements found so far and restarting the
                // enumeration.
                //
                // ⚠ This method may interleave with other asynchronous calls to CleanUpDeadObjectsAsync.
                var cleanUpEnumerator = _table.GetEnumerator();
                while (cleanUpEnumerator.MoveNext())
                {
                    var pair = cleanUpEnumerator.Current;
                    if (!pair.Value.IsAlive())
                    {
                        _deadKeySet.Add(pair.Key);

                        if (timeSlice.IsOver)
                        {
                            // Yield before processing items found so far.
                            await ResetTimeSliceAsync().ConfigureAwait(true);

                            // Process items found prior to exceeding the time slice. Due to interleaving, it is
                            // possible for this call to process items found by another asynchronous call to
                            // CollectDeadKeysAsync, or for another asynchronous call to RemoveDeadKeysAsync to process
                            // all items prior to this call.
                            await RemoveDeadKeysAsync().ConfigureAwait(true);

                            // Obtain a new enumerator since the previous one may be invalidated.
                            cleanUpEnumerator = _table.GetEnumerator();
                        }
                    }
                }
            }

            async Task RemoveDeadKeysAsync()
            {
                while (_deadKeySet.Count > 0)
                {
                    // Fully process one item from _deadKeySet before the possibility of yielding
                    var key = _deadKeySet.First();

                    _deadKeySet.Remove(key);

                    Debug.Assert(_table.ContainsKey(key), "Key not found in table.");
                    _table.Remove(key);

                    if (timeSlice.IsOver)
                    {
                        await ResetTimeSliceAsync().ConfigureAwait(true);
                    }
                }
            }

            async Task ResetTimeSliceAsync()
            {
                await listener.Delay(TimeSpan.FromMilliseconds(50), ThreadingContext.DisposalToken).ConfigureAwait(true);
                timeSlice = new TimeSlice(CleanUpTimeSlice);
            }
        }

        public void Add(TKey key, TValue value)
        {
            this.AssertIsForeground();

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (_table.ContainsKey(key))
            {
                throw new InvalidOperationException($"Key already exists in table: {(key != null ? key.ToString() : "<null>")}.");
            }

            _itemsAddedSinceLastCleanUp++;
            if (_itemsAddedSinceLastCleanUp >= CleanUpThreshold)
            {
                _needsCleanUp = true;
                _itemsAddedSinceLastCleanUp = 0;
            }

            _table.Add(key, new WeakComHandle<TValue, TValue>(value));
        }

        public TValue Remove(TKey key)
        {
            this.AssertIsForeground();

            if (_deadKeySet.Contains(key))
            {
                _deadKeySet.Remove(key);
            }

            if (_table.TryGetValue(key, out var handle))
            {
                _table.Remove(key);
                return handle.ComAggregateObject;
            }

            return null;
        }

        public bool ContainsKey(TKey key)
        {
            this.AssertIsForeground();

            return _table.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            this.AssertIsForeground();
            if (_table.TryGetValue(key, out var handle))
            {
                value = handle.ComAggregateObject;
                return value != null;
            }

            value = null;
            return false;
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var keyValuePair in _table)
                {
                    yield return keyValuePair.Value.ComAggregateObject;
                }
            }
        }
    }
}
