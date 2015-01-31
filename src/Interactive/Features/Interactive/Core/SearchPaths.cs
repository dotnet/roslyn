// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Search path list facade that displays nicely in Interactive Window.
    /// </summary>
    public sealed class SearchPaths : IList<string>
    {
        private readonly SynchronizedVersionedList<string> _list = new SynchronizedVersionedList<string>();

        internal SearchPaths()
        {
        }

        internal SynchronizedVersionedList<string> List { get { return _list; } }

        public int IndexOf(string item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public string this[int index]
        {
            get
            {
                return _list[index];
            }

            set
            {
                _list[index] = value;
            }
        }

        public void Add(string item)
        {
            _list.Add(item);
        }

        public void AddRange(IEnumerable<string> collection)
        {
            _list.AddRange(collection);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(string item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _list.Count; }
        }

        public bool IsReadOnly
        {
            get { return _list.IsReadOnly; }
        }

        public bool Remove(string item)
        {
            return _list.Remove(item);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }
    }
}
