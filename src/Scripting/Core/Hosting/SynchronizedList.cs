// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal class SynchronizedList<T> : IList<T>
    {
        private readonly object _guard = new object();
        private readonly List<T> _list = new List<T>();

        public T this[int index]
        {
            get
            {
                lock (_guard)
                {
                    return _list[index];
                }
            }

            set
            {
                lock (_guard)
                {
                    _list[index] = value;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (_guard)
                {
                    return _list.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public void Add(T item)
        {
            lock (_guard)
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            lock (_guard)
            {
                _list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_guard)
            {
                return _list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_guard)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            lock (_guard)
            {
                // make a copy to ensure thread-safe enumeration
                return ((IEnumerable<T>)_list.ToArray()).GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(T item)
        {
            lock (_guard)
            {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_guard)
            {
                _list.Insert(index, item);
            }
        }

        public bool Remove(T item)
        {
            lock (_guard)
            {
                return _list.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_guard)
            {
                _list.RemoveAt(index);
            }
        }
    }
}
