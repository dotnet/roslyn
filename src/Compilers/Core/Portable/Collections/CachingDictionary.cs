// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// The CachingLookup class provides a convenient representation of an ILookup that is based
    /// upon a potentially slow lookup, and caches lookup results so that subsequent lookups are
    /// fast. Internally a ConcurrentDictionary is used to cache lookup results. The client provides
    /// two delegates to perform lookups: One that maps a key to a IEnumerable of values, and one
    /// that provides all keys.
    /// 
    /// The client must provide an IEqualityComparer used for comparing keys. Failed lookups are
    /// cached, but that has the disadvantage that every different failed lookup will consume a
    /// small amount of extra memory. However, that memory can be reclaimed by forcing a full
    /// population of the cache.
    /// 
    /// Thread safe.
    /// </summary>
    internal class CachingDictionary<TKey, TElement>
        where TKey : notnull
    {
        private readonly Func<TKey, ImmutableArray<TElement>> _getElementsOfKey;
        private readonly Func<IEqualityComparer<TKey>, SegmentedHashSet<TKey>> _getKeys;
        private readonly IEqualityComparer<TKey> _comparer;

        // The underlying dictionary. It may be null (indicating that nothing is cached), a ConcurrentDictionary
        // or something frozen (usually a regular Dictionary). The frozen Dictionary is used only once the collection
        // is fully populated. This is a memory optimization so that we don't hold onto relatively ConcurrentDictionary
        // instances once the cache is fully populated.
        private IDictionary<TKey, ImmutableArray<TElement>>? _map;

        // This is a special sentinel value that is placed inside the map to indicate that a key was looked
        // up, but not found.
        private static readonly ImmutableArray<TElement> s_emptySentinel = ImmutableArray<TElement>.Empty;

        /// <summary>
        /// Create a CachingLookup.
        /// </summary>
        /// <param name="getElementsOfKey">A function that takes a key, and returns an IEnumerable of values that
        /// correspond to that key. If no values correspond, the function may either return null or an empty
        /// IEnumerable.</param>
        /// <param name="getKeys">A function that returns an IEnumerable of all keys that have associated values.</param>
        /// <param name="comparer">A IEqualityComparer used to compare keys.</param>
        public CachingDictionary(
            Func<TKey, ImmutableArray<TElement>> getElementsOfKey,
            Func<IEqualityComparer<TKey>, SegmentedHashSet<TKey>> getKeys,
            IEqualityComparer<TKey> comparer)
        {
            _getElementsOfKey = getElementsOfKey;
            _getKeys = getKeys;
            _comparer = comparer;
        }

        /// <summary>
        /// Does this key have one or more associated values?
        /// </summary>
        public bool Contains(TKey key)
        {
            return this[key].Length != 0;
        }

        /// <summary>
        /// Get the values associated with a key. 
        /// </summary>
        /// <param name="key">Key to look up.</param>
        /// <returns>All values associated with key. Returns an empty IEnumerable if
        /// no values are associated. Never returns null.</returns>
        public ImmutableArray<TElement> this[TKey key]
        {
            get
            {
                return this.GetOrCreateValue(key);
            }
        }

        /// <summary>
        /// Get the number of distinct keys.
        /// Forces a full population of the cache.
        /// </summary>
        public int Count
        {
            get
            {
                return this.EnsureFullyPopulated().Count;
            }
        }

        /// <summary>
        /// Enumerate all the keys.
        /// Forces a full population of the cache.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                return this.EnsureFullyPopulated().Keys;
            }
        }

        /// <summary>
        /// Add the values from all keys to a flat array.
        /// Forces a full population of the cache.
        /// </summary>
        /// <param name="array"></param>
        public void AddValues(ArrayBuilder<TElement> array)
        {
            foreach (var kvp in this.EnsureFullyPopulated())
            {
                array.AddRange(kvp.Value);
            }
        }

        /// <summary>
        /// Create an instance of the concurrent dictionary.
        /// </summary>
        /// <returns>The concurrent dictionary</returns>
        private ConcurrentDictionary<TKey, ImmutableArray<TElement>> CreateConcurrentDictionary()
        {
            return new ConcurrentDictionary<TKey, ImmutableArray<TElement>>(concurrencyLevel: 2, capacity: 0, comparer: _comparer);
        }

        /// <summary>
        /// Create a dictionary instance suitable for use as the fully populated map.
        /// </summary>
        /// <returns>A new, empty dictionary, suitable for use as the fully populated map.</returns>
        private IDictionary<TKey, ImmutableArray<TElement>> CreateDictionaryForFullyPopulatedMap(int capacity)
        {
            // CONSIDER: If capacity is small, consider using a more frugal data structure.
            return new Dictionary<TKey, ImmutableArray<TElement>>(capacity, _comparer);
        }

        /// <summary>
        /// Use the underlying (possibly slow) functions to get the values associated with a key.
        /// </summary>
        private ImmutableArray<TElement> GetOrCreateValue(TKey key)
        {
            ImmutableArray<TElement> elements;
            ConcurrentDictionary<TKey, ImmutableArray<TElement>>? concurrentMap;

            // Check if we're fully populated before trying to retrieve the elements.  If we are
            // and we don't get any elements back, then we don't have to go any further.
            var localMap = _map;

            if (localMap == null)
            {
                concurrentMap = CreateConcurrentDictionary();
                localMap = Interlocked.CompareExchange(ref _map, concurrentMap, null);
                if (localMap == null)
                {
                    return AddToConcurrentMap(concurrentMap, key);
                }
                // Some other thread beat us to the initial population
            }

            // first check to see if they are already cached
            if (localMap.TryGetValue(key, out elements))
            {
                return elements;
            }

            // How we proceed depends on whether we're fully populated.
            concurrentMap = localMap as ConcurrentDictionary<TKey, ImmutableArray<TElement>>;

            // If we're fully populated, the value wasn't found. Otherwise, lookup the new value and add it to the concurrent map.
            return concurrentMap == null ? s_emptySentinel : AddToConcurrentMap(concurrentMap, key);
        }

        /// <summary>
        /// Add a new value with the given key to the given concurrent map.
        /// </summary>
        /// <param name="map">The concurrent map to augment.</param>
        /// <param name="key">The key of the new entry.</param>
        /// <returns>The added entry. If there was a race, and another thread beat this one, then this returns the previously added entry.</returns>
        private ImmutableArray<TElement> AddToConcurrentMap(ConcurrentDictionary<TKey, ImmutableArray<TElement>> map, TKey key)
        {
            var elements = _getElementsOfKey(key);

            if (elements.IsDefaultOrEmpty)
            {
                // In this case, we're not fully populated, so remember that this was a failed
                // lookup.
                elements = s_emptySentinel;
            }

            return map.GetOrAdd(key, elements);
        }

        /// <summary>
        /// Determines if the given map is fully populated.
        /// </summary>
        /// <param name="existingMap">The map to test.</param>
        /// <returns>true if the map is fully populated.</returns>
        private static bool IsNotFullyPopulatedMap([NotNullWhen(returnValue: false)] IDictionary<TKey, ImmutableArray<TElement>>? existingMap)
        {
            return existingMap == null || existingMap is ConcurrentDictionary<TKey, ImmutableArray<TElement>>;
        }

        /// <summary>
        /// Create the fully populated map from an existing map and the key generator.
        /// </summary>
        /// <param name="existingMap">The existing map which may be null or a ConcurrentDictionary.</param>
        /// <returns></returns>
        private IDictionary<TKey, ImmutableArray<TElement>> CreateFullyPopulatedMap(ConcurrentDictionary<TKey, ImmutableArray<TElement>>? existingMap)
        {
            Debug.Assert(IsNotFullyPopulatedMap(existingMap));

            // Enumerate all the keys and attempt to generate values for all of them.
            var allKeys = _getKeys(_comparer);
            Debug.Assert(_comparer == allKeys.Comparer);

            var fullyPopulatedMap = CreateDictionaryForFullyPopulatedMap(capacity: allKeys.Count);
            if (existingMap == null)
            {
                // The concurrent map has never been created.
                foreach (var key in allKeys)
                {
                    fullyPopulatedMap.Add(key, _getElementsOfKey(key));
                }
            }
            else
            {
                foreach (var key in allKeys)
                {
                    // Copy non-empty values from the existing map
                    ImmutableArray<TElement> elements = existingMap.GetOrAdd(key, _getElementsOfKey);
                    Debug.Assert(elements != s_emptySentinel);
                    fullyPopulatedMap.Add(key, elements);
                }
            }

            return fullyPopulatedMap;
        }

        /// <summary>
        /// Fully populate the underlying dictionary. Once this returns, the dictionary is guaranteed 
        /// to have every key in it.
        /// </summary>
        private IDictionary<TKey, ImmutableArray<TElement>> EnsureFullyPopulated()
        {
            var currentMap = _map;
            while (IsNotFullyPopulatedMap(currentMap))
            {
                IDictionary<TKey, ImmutableArray<TElement>> fullyPopulatedMap = CreateFullyPopulatedMap((ConcurrentDictionary<TKey, ImmutableArray<TElement>>?)currentMap);

                var replacedMap = Interlocked.CompareExchange(ref _map, fullyPopulatedMap, currentMap);
                if (replacedMap == currentMap)
                {
                    // Normal exit.
                    return fullyPopulatedMap;
                }

                // Another thread either initialized a new ConcurrentMap or a fully-populated map.
                currentMap = replacedMap;
            }

            // The map is already fully populated
            return currentMap;
        }
    }
}
