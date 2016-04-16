// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A queue that will only allow one instance of an object inside of it at a time.  When an
    /// object is enqueued that is already in the list, it is removed from its location and placed
    /// at the end of the queue.  These aspects make the queue useful for LRU caches.
    /// 
    /// This class is not thread-safe.  If you need it to be, wrap it within your own lock.
    /// </summary>
    internal class LinkedHashQueue<T> : IEnumerable<T>
    {
        private readonly LinkedList<T> _list;
        private readonly Dictionary<T, LinkedListNode<T>> _map;
        private int _insertionIndex;

        public LinkedHashQueue()
            : this(null)
        {
        }

        public LinkedHashQueue(IEqualityComparer<T> comparer)
        {
            _list = new LinkedList<T>();
            _map = new Dictionary<T, LinkedListNode<T>>(comparer);
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public void Clear()
        {
            _list.Clear();
            _map.Clear();
            _insertionIndex = 0;
        }

        public T First
        {
            get
            {
                return _list.First.Value;
            }
        }

        /// <summary>
        /// Adds this item (or moves it if it's already in the queue) to the end.  If the item is not
        /// in the list, 'true' is returned, otherwise 'false'.
        /// </summary>
        public bool Enqueue(T value)
        {
            var result = true;

            LinkedListNode<T> node;
            if (_map.TryGetValue(value, out node))
            {
                // Already had this in the list.  Return 'false'.  
                result = false;
                _list.Remove(node);
            }

            node = _list.AddLast(value);
            _insertionIndex++;
            _map[value] = node;

            return result;
        }

        public T Dequeue()
        {
            if (this.Count == 0)
            {
                throw new InvalidOperationException();
            }

            var node = _list.First;
            _list.RemoveFirst();
            _map.Remove(node.Value);

            return node.Value;
        }

        public bool Contains(T value)
        {
            LinkedListNode<T> node;
            return _map.TryGetValue(value, out node);
        }

        public void Remove(T value)
        {
            LinkedListNode<T> node;
            _map.TryGetValue(value, out node);
            if (_map.Remove(value))
            {
                _list.Remove(node);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
