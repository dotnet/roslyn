// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Analyzer.Utilities.PooledObjects
{
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not used in this context")]
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "The 'Dictionary' suffix is intentional")]
    internal struct TemporaryDictionary<TKey, TValue>
        where TKey : notnull
    {
#pragma warning disable CS0649 // Field 'TemporaryDictionary<TKey, TValue>.Empty' is never assigned to, and will always have its default value
        public static readonly TemporaryDictionary<TKey, TValue> Empty;
#pragma warning restore CS0649 // Field 'TemporaryDictionary<TKey, TValue>.Empty' is never assigned to, and will always have its default value

        /// <summary>
        /// An empty dictionary used for creating non-null enumerators when no items have been added to the dictionary.
        /// </summary>
        private static readonly Dictionary<TKey, TValue> EmptyDictionary = [];

        // 🐇 PERF: use PooledDictionary<TKey, TValue> instead of PooledConcurrentDictionary<TKey, TValue> due to
        // allocation overhead in clearing the set for returning it to the pool.
        private PooledDictionary<TKey, TValue>? _storage;

        public readonly Enumerable NonConcurrentEnumerable
            => new(_storage ?? EmptyDictionary);

        public void Free(CancellationToken cancellationToken)
        {
            Interlocked.Exchange(ref _storage, null)?.Free(cancellationToken);
        }

        private PooledDictionary<TKey, TValue> GetOrCreateStorage(CancellationToken cancellationToken)
        {
            if (_storage is not { } storage)
            {
                var newStorage = PooledDictionary<TKey, TValue>.GetInstance();
                storage = Interlocked.CompareExchange(ref _storage, newStorage, null) ?? newStorage;
                if (storage != newStorage)
                {
                    // Another thread initialized the value. Make sure to release the unused object.
                    newStorage.Free(cancellationToken);
                }
            }

            return storage;
        }

        internal void Add(TKey key, TValue value, CancellationToken cancellationToken)
        {
            var storage = GetOrCreateStorage(cancellationToken);
            lock (storage)
            {
                storage.Add(key, value);
            }
        }

        public readonly struct Enumerable
        {
            private readonly Dictionary<TKey, TValue> _dictionary;

            public Enumerable(Dictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public Enumerator GetEnumerator()
                => new(_dictionary.GetEnumerator());
        }

        public struct Enumerator
        {
            private Dictionary<TKey, TValue>.Enumerator _enumerator;

            public Enumerator(Dictionary<TKey, TValue>.Enumerator enumerator)
            {
                _enumerator = enumerator;
            }

            public bool MoveNext()
                => _enumerator.MoveNext();

            public KeyValuePair<TKey, TValue> Current
                => _enumerator.Current;
        }
    }
}
