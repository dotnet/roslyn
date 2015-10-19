// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        private IEnumerator<KeyValuePair<TKey, WeakComHandle<TValue, TValue>>> _cleanUpEnumerator;

        private enum CleanUpState { Initial, CollectingDeadKeys, RemovingDeadKeys }
        private CleanUpState _cleanUpState;

        public bool NeedsCleanUp => _needsCleanUp;

        public CleanableWeakComHandleTable(int? cleanUpThreshold = null, TimeSpan? cleanUpTimeSlice = null)
        {
            _table = new Dictionary<TKey, WeakComHandle<TValue, TValue>>();
            _deadKeySet = new HashSet<TKey>();

            CleanUpThreshold = cleanUpThreshold ?? DefaultCleanUpThreshold;
            CleanUpTimeSlice = cleanUpTimeSlice ?? s_defaultCleanUpTimeSlice;
            _cleanUpState = CleanUpState.Initial;
        }

        private void InvalidateEnumerator()
        {
            if (_cleanUpEnumerator != null)
            {
                _cleanUpEnumerator.Dispose();
                _cleanUpEnumerator = null;
            }
        }

        private bool CollectDeadKeys(TimeSlice timeSlice)
        {
            Debug.Assert(_cleanUpState == CleanUpState.CollectingDeadKeys);
            Debug.Assert(_cleanUpEnumerator != null);

            while (_cleanUpEnumerator.MoveNext())
            {
                var pair = _cleanUpEnumerator.Current;

                if (!pair.Value.IsAlive())
                {
                    _deadKeySet.Add(pair.Key);
                }

                if (timeSlice.IsOver)
                {
                    return false;
                }
            }

            return true;
        }

        private bool RemoveDeadKeys(TimeSlice timeSlice)
        {
            Debug.Assert(_cleanUpEnumerator == null);

            while (_deadKeySet.Count > 0)
            {
                var key = _deadKeySet.First();

                _deadKeySet.Remove(key);

                Debug.Assert(_table.ContainsKey(key), "Key not found in table.");
                _table.Remove(key);

                if (timeSlice.IsOver)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Cleans up references to dead objects in the table. This operation will return if it takes
        /// longer than <see cref="CleanUpTimeSlice"/>. Calling <see cref="CleanUpDeadObjects"/> further
        /// times will continue the process.
        /// </summary>
        public void CleanUpDeadObjects()
        {
            this.AssertIsForeground();

            if (!_needsCleanUp)
            {
                return;
            }

            var timeSlice = new TimeSlice(CleanUpTimeSlice);

            if (_cleanUpState == CleanUpState.Initial)
            {
                _cleanUpEnumerator = _table.GetEnumerator();
                _cleanUpState = CleanUpState.CollectingDeadKeys;
            }

            if (_cleanUpState == CleanUpState.CollectingDeadKeys)
            {
                if (_cleanUpEnumerator == null)
                {
                    // The enumerator got reset while we were collecting dead keys.
                    // Go ahead and remove the dead keys that were already collected before
                    // collecting more.
                    if (!RemoveDeadKeys(timeSlice))
                    {
                        return;
                    }

                    _cleanUpEnumerator = _table.GetEnumerator();
                }

                if (!CollectDeadKeys(timeSlice))
                {
                    return;
                }

                InvalidateEnumerator();
                _cleanUpState = CleanUpState.RemovingDeadKeys;
            }

            if (_cleanUpState == CleanUpState.RemovingDeadKeys)
            {
                if (!RemoveDeadKeys(timeSlice))
                {
                    return;
                }

                _cleanUpState = CleanUpState.Initial;
            }

            _needsCleanUp = false;
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
                throw new InvalidOperationException("Key already exists in table.");
            }

            _itemsAddedSinceLastCleanUp++;
            if (_itemsAddedSinceLastCleanUp >= CleanUpThreshold)
            {
                _needsCleanUp = true;
                _itemsAddedSinceLastCleanUp = 0;
            }

            InvalidateEnumerator();

            this._table.Add(key, new WeakComHandle<TValue, TValue>(value));
        }

        public TValue Remove(TKey key)
        {
            this.AssertIsForeground();

            InvalidateEnumerator();

            if (_deadKeySet.Contains(key))
            {
                _deadKeySet.Remove(key);
            }

            WeakComHandle<TValue, TValue> handle;
            if (_table.TryGetValue(key, out handle))
            {
                _table.Remove(key);
                return handle.ComAggregateObject;
            }

            return null;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            this.AssertIsForeground();

            WeakComHandle<TValue, TValue> handle;
            if (_table.TryGetValue(key, out handle))
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
