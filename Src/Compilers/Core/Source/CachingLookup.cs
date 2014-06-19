using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers
{
    /// <summary>
    /// The CachingLookup class provides a convenient representation of an ILookup that 
    /// is based upon a potentially slow lookup, and caches lookup results so that subsequent lookups
    /// are fast. Internally a ConcurrentDictionary is used to cache lookup results. The client
    /// provides two delegate to perform lookups: One that maps a key to a IEnumerable of values, and
    /// one that provides all keys.
    /// 
    /// The client almost must provide an IEqualityComparer used for comparing keys, and can select whether
    /// to cache failed lookups (keys with no values). Caching failed lookups has the disadvantage that
    /// every different failed lookup will consume a small amount of extra memory.
    /// 
    /// Thread safe.
    /// </summary>
    internal class CachingLookup<TKey, TElement> : ILookup<TKey, TElement>
    {
        private readonly Func<TKey, IEnumerable<TElement>> getElementsOfKey;
        private readonly Func<IEnumerable<TKey>> getKeys;
        private readonly IEqualityComparer<TKey> comparer;
        private readonly bool cacheNonExistentKeys;

        private ConcurrentDictionary<TKey, IEnumerable<TElement>> map;  // underlying dictionary.
        private int allCached;  // non-zero means everything is now cached, so no need to look up new keys.

        // This is a special sentinel value that is placed inside the map to indicate that a key was looked
        // up, but not found.
        private static readonly IEnumerable<TElement> keyNotPresent = new TElement[0];

        /// <summary>
        /// Create a CachingLookup.
        /// </summary>
        /// <param name="getElementsOfKey">A function that takes a key, and returns an IEnumerable of values that
        /// correspond to that key. If no values correspond, the function may either return null or an empty
        /// IEnumerable.</param>
        /// <param name="getKeys">A function that returns an IEnumerable of all keys that have associated values.</param>
        /// <param name="comparer">A IEqualityComparer used to compare keys.</param>
        /// <param name="cacheNonExistentKeys">If true, the results of lookups that have no values associated with them
        /// are cached. If false, only the results of successful lookups are cached.</param>
        public CachingLookup(Func<TKey, IEnumerable<TElement>> getElementsOfKey,
                             Func<IEnumerable<TKey>> getKeys,
                             IEqualityComparer<TKey> comparer,
                             bool cacheNonExistentKeys)
        {
            this.getElementsOfKey = getElementsOfKey;
            this.getKeys = getKeys;
            this.comparer = comparer;
            this.cacheNonExistentKeys = cacheNonExistentKeys;
            this.allCached = 0;
        }

        /// <summary>
        /// Does this key have one or more associated values?
        /// </summary>
        public bool Contains(TKey key)
        {
            IEnumerable<TElement> elements = this[key];
            return elements != keyNotPresent;
        }

        /// <summary>
        /// Get the values associated with a key. 
        /// </summary>
        /// <param name="key">Key to look up.</param>
        /// <returns>All values associated with key. Returns an empty IEnumerable if
        /// no values are associated. Never returns null.</returns>
        public IEnumerable<TElement> this[TKey key]
        {
            get
            {
                IEnumerable<TElement> elements;

                if (Map.TryGetValue(key, out elements))
                {
                    // We've looked up this key before, and found it.
                    return elements;
                }
                else if (allCached != 0)
                {
                    // We know all keys, so this key can't be present.
                    return keyNotPresent;
                }
                else
                {
                    IEnumerable<TElement> result;
                    if (TryGetValue(key, out result))
                    {
                        return result;
                    }
                    else
                    {
                        return keyNotPresent;
                    }
                }
            }
        }

        /// <summary>
        /// Get the number of distinct keys.
        /// </summary>
        public int Count
        {
            get
            {
                FullyPopulate();
                return Map.Count;
            }
        }

        /// <summary>
        /// Enumerate all the keys.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                FullyPopulate();
                return Map.Keys;
            }
        }

        /// <summary>
        /// Get all keys and values, as IGroupings.
        /// </summary>
        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            FullyPopulate();
            return (from pair in Map select new Grouping<TKey, TElement>(pair)).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get the concurrent dictionary, creating it if needed.
        /// </summary>
        private ConcurrentDictionary<TKey, IEnumerable<TElement>> Map
        {
            get
            {
                if (map == null)
                {
                    Interlocked.CompareExchange(ref map, new ConcurrentDictionary<TKey, IEnumerable<TElement>>(comparer), null);
                }
                
                return map;
            }
        }

        /// <summary>
        /// Use the underlying (possibly slow) functions to get the values associated with a key.
        /// </summary>
        private bool TryGetValue(TKey key, out IEnumerable<TElement> elements)
        {
            IEnumerable<TElement> returnedElements = getElementsOfKey(key);

            if (returnedElements != null)
            {
                TElement[] elementArray = returnedElements.ToArray();
                if (elementArray.Length > 0)
                {
                    elements = new ReadOnlyCollection<TElement>(elementArray);
                    Map.TryAdd(key, elements);
                    return true;
                }
            }

            // The returned elements were null or empty.
            if (cacheNonExistentKeys)
            {
                Map.TryAdd(key, keyNotPresent);
            }

            elements = keyNotPresent;
            return false;
        }

        /// <summary>
        /// Fully populate the underlying dictionary. Once this returns, the dictionary is guaranteed 
        /// to have every key in it. The field allCached is set to non-zero to indicate this, so that 
        /// we don't have to lookup keys that aren't in the dictionary.
        /// </summary>
        private void FullyPopulate()
        {
            IEnumerable<TElement> elements;

            if (allCached != 0)
            {
                return;
            }

            foreach (TKey key in getKeys())
            {
                if (!Map.TryGetValue(key, out elements))
                {
                    TryGetValue(key, out elements);
                }
            }

            if (cacheNonExistentKeys)
            {
                // Remove any keys that have no values, since they have no use any more
                // once the map is fully populated.
                foreach (TKey key in Map.Keys)
                {
                    if (Map[key] == keyNotPresent)
                    {
                        Map.TryRemove(key, out elements);
                    }
                }
            }

            Interlocked.Exchange(ref allCached, 1);
        }
    }
}