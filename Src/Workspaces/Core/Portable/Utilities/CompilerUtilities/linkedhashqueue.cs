// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// This class is not threadsafe.  If you need it to be, wrap it within your own lock.
    /// </summary>
    internal class LinkedHashQueue<T> : IEnumerable<T>
    {
        private readonly LinkedList<T> list;
        private readonly Dictionary<T, LinkedListNode<T>> map;
        private int insertionIndex = 0;

        public LinkedHashQueue()
            : this(null)
        {
        }

        public LinkedHashQueue(IEqualityComparer<T> comparer)
        {
            this.list = new LinkedList<T>();
            this.map = new Dictionary<T, LinkedListNode<T>>(comparer);
        }

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public void Clear()
        {
            this.list.Clear();
            this.map.Clear();
            this.insertionIndex = 0;
        }

        public T First
        {
            get
            {
                return this.list.First.Value;
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
            if (map.TryGetValue(value, out node))
            {
                // Already had this in the list.  Return 'false'.  
                result = false;
                list.Remove(node);
            }

            node = list.AddLast(value);
            insertionIndex++;
            map[value] = node;

            return result;
        }

        public T Dequeue()
        {
            if (this.Count == 0)
            {
                throw new InvalidOperationException();
            }

            var node = list.First;
            list.RemoveFirst();
            map.Remove(node.Value);

            return node.Value;
        }

        public bool Contains(T value)
        {
            LinkedListNode<T> node;
            return map.TryGetValue(value, out node);
        }

        public void Remove(T value)
        {
            LinkedListNode<T> node;
            map.TryGetValue(value, out node);
            if (map.Remove(value))
            {
                list.Remove(node);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
