// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.InternalUtilities
{
    /// <summary>
    /// Cache with a fixed size that evicts the least recently used members.
    /// Thread-safe.
    /// </summary>
    internal class ConcurrentLruCache<K, V>
        where K : notnull
        where V : notnull
    {
        private readonly int _capacity;

        private struct CacheValue
        {
            public V Value;
            public LinkedListNode<K> Node;
        }

        private readonly Dictionary<K, CacheValue> _cache;
        private readonly LinkedList<K> _nodeList;
        // This is a naive course-grained lock, it can probably be optimized
        private readonly object _lockObject = new object();

        public ConcurrentLruCache(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _capacity = capacity;
            _cache = new Dictionary<K, CacheValue>(capacity);
            _nodeList = new LinkedList<K>();
        }

        /// <summary>
        /// Create cache from an array. The cache capacity will be the size
        /// of the array. All elements of the array will be added to the 
        /// cache. If any duplicate keys are found in the array a
        /// <see cref="ArgumentException"/> will be thrown.
        /// </summary>
        public ConcurrentLruCache(KeyValuePair<K, V>[] array)
            : this(array.Length)
        {
            foreach (var kvp in array)
            {
                this.UnsafeAdd(kvp.Key, kvp.Value, true);
            }
        }

        /// <summary>
        /// For testing. Very expensive.
        /// </summary>
        internal IEnumerable<KeyValuePair<K, V>> TestingEnumerable
        {
            get
            {
                lock (_lockObject)
                {
                    var copy = new KeyValuePair<K, V>[_cache.Count];
                    int index = 0;
                    foreach (K key in _nodeList)
                    {
                        copy[index++] = new KeyValuePair<K, V>(key,
                                                               _cache[key].Value);
                    }
                    return copy;
                }
            }
        }

        public void Add(K key, V value)
        {
            lock (_lockObject)
            {
                UnsafeAdd(key, value, true);
            }
        }

        private void MoveNodeToTop(LinkedListNode<K> node)
        {
            if (!object.ReferenceEquals(_nodeList.First, node))
            {
                _nodeList.Remove(node);
                _nodeList.AddFirst(node);
            }
        }

        /// <summary>
        /// Expects non-empty cache. Does not lock.
        /// </summary>
        private void UnsafeEvictLastNode()
        {
            Debug.Assert(_capacity > 0);
            var lastNode = _nodeList.Last;
            _nodeList.Remove(lastNode);
            _cache.Remove(lastNode.Value);
        }

        private void UnsafeAddNodeToTop(K key, V value)
        {
            var node = new LinkedListNode<K>(key);
            _cache.Add(key, new CacheValue { Node = node, Value = value });
            _nodeList.AddFirst(node);
        }

        /// <summary>
        /// Doesn't lock.
        /// </summary>
        private void UnsafeAdd(K key, V value, bool throwExceptionIfKeyExists)
        {
            if (_cache.TryGetValue(key, out var result))
            {
                if (throwExceptionIfKeyExists)
                {
                    throw new ArgumentException("Key already exists", nameof(key));
                }
                else if (!result.Value.Equals(value))
                {
                    result.Value = value;
                    _cache[key] = result;
                    MoveNodeToTop(result.Node);
                }
            }
            else
            {
                if (_cache.Count == _capacity)
                {
                    UnsafeEvictLastNode();
                }
                UnsafeAddNodeToTop(key, value);
            }
        }

        public V this[K key]
        {
            get
            {
                if (TryGetValue(key, out var value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException();
                }
            }
            set
            {
                lock (_lockObject)
                {
                    UnsafeAdd(key, value, false);
                }
            }
        }

        public bool TryGetValue(K key, [MaybeNullWhen(returnValue: false)] out V value)
        {
            lock (_lockObject)
            {
                return UnsafeTryGetValue(key, out value);
            }
        }

        /// <summary>
        /// Doesn't lock.
        /// </summary>
        public bool UnsafeTryGetValue(K key, [MaybeNullWhen(returnValue: false)] out V value)
        {
            if (_cache.TryGetValue(key, out var result))
            {
                MoveNodeToTop(result.Node);
                value = result.Value;
                return true;
            }
            else
            {
                value = default!;
                return false;
            }
        }

        public V GetOrAdd(K key, V value)
        {
            lock (_lockObject)
            {
                if (UnsafeTryGetValue(key, out var result))
                {
                    return result;
                }
                else
                {
                    UnsafeAdd(key, value, true);
                    return value;
                }
            }
        }

        public V GetOrAdd(K key, Func<V> creator)
        {
            lock (_lockObject)
            {
                if (UnsafeTryGetValue(key, out var result))
                {
                    return result;
                }
                else
                {
                    var value = creator();
                    UnsafeAdd(key, value, true);
                    return value;
                }
            }
        }

        public V GetOrAdd<T>(K key, T arg, Func<T, V> creator)
        {
            lock (_lockObject)
            {
                if (UnsafeTryGetValue(key, out var result))
                {
                    return result;
                }
                else
                {
                    var value = creator(arg);
                    UnsafeAdd(key, value, true);
                    return value;
                }
            }
        }
    }
}
