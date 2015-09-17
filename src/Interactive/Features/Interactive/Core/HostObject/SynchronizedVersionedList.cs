// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Synchronized list that tracks its version.
    /// </summary>
    internal sealed class SynchronizedVersionedList<T> : IList<T>
    {
        private readonly List<T> _list = new List<T>();

        // version of the last snapshot we took:
        private int _lastSnapshotVersion = -1;

        // the current version, each modification increments it:
        private int _version;

        internal int Version
        {
            get { return _version; }
        }

        private void Mutated()
        {
            _version++;
        }

        internal T[] ToArray()
        {
            lock (_list)
            {
                return _list.ToArray();
            }
        }

        internal T[] GetNewContent()
        {
            lock (_list)
            {
                if (_lastSnapshotVersion == _version)
                {
                    return null;
                }

                _lastSnapshotVersion = _version;
                return _list.ToArray();
            }
        }

        public int IndexOf(T item)
        {
            lock (_list)
            {
                return _list.IndexOf(item);
            }
        }

        public void Insert(int index, T item)
        {
            lock (_list)
            {
                _list.Insert(index, item);
                Mutated();
            }
        }

        public void RemoveAt(int index)
        {
            lock (_list)
            {
                _list.RemoveAt(index);
                Mutated();
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_list)
                {
                    return _list[index];
                }
            }

            set
            {
                lock (_list)
                {
                    Mutated();
                    _list[index] = value;
                }
            }
        }

        public void Add(T item)
        {
            lock (_list)
            {
                _list.Add(item);
                Mutated();
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            lock (_list)
            {
                _list.AddRange(collection);
                Mutated();
            }
        }

        public void Clear()
        {
            lock (_list)
            {
                if (_list.Count > 0)
                {
                    _list.Clear();
                    Mutated();
                }
            }
        }

        public bool Contains(T item)
        {
            lock (_list)
            {
                return _list.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_list)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (_list)
                {
                    return _list.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            lock (_list)
            {
                var removed = _list.Remove(item);
                if (removed)
                {
                    Mutated();
                }

                return removed;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            T[] snapshot;
            lock (_list)
            {
                snapshot = _list.ToArray();
            }

            return ((IEnumerable<T>)snapshot).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
