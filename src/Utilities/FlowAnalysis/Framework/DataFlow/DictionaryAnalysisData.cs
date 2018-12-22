// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    internal sealed class DictionaryAnalysisData<TKey, TValue> : AbstractAnalysisData, IDictionary<TKey, TValue>
    {
        private readonly PooledDictionary<TKey, TValue> _coreAnalysisData;

        public DictionaryAnalysisData()
        {
            _coreAnalysisData = PooledDictionary<TKey, TValue>.GetInstance();
        }

        public DictionaryAnalysisData(IDictionary<TKey, TValue> initializer)
        {
            _coreAnalysisData = PooledDictionary<TKey, TValue>.GetInstance(initializer);
        }

        public ImmutableDictionary<TKey, TValue> ToImmutableDictionary()
        {
            Debug.Assert(!Disposed);
            return _coreAnalysisData.ToImmutableDictionary();
        }

        public TValue this[TKey key]
        {
            get
            {
                Debug.Assert(!Disposed);
                return _coreAnalysisData[key];
            }
            set
            {
                Debug.Assert(!Disposed);
                _coreAnalysisData[key] = value;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                Debug.Assert(!Disposed);
                return _coreAnalysisData.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                Debug.Assert(!Disposed);
                return _coreAnalysisData.Values;
            }
        }

        public int Count
        {
            get
            {
                Debug.Assert(!Disposed);
                return _coreAnalysisData.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                Debug.Assert(!Disposed);
                return ((IDictionary<TKey, TValue>)_coreAnalysisData).IsReadOnly;
            }
        }

        public void Add(TKey key, TValue value)
        {
            Debug.Assert(!Disposed);
            _coreAnalysisData.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Debug.Assert(!Disposed);
            _coreAnalysisData.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Debug.Assert(!Disposed);
            _coreAnalysisData.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            Debug.Assert(!Disposed);
            return ((IDictionary<TKey, TValue>)_coreAnalysisData).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            Debug.Assert(!Disposed);
            return _coreAnalysisData.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Debug.Assert(!Disposed);
            ((IDictionary<TKey, TValue>)_coreAnalysisData).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Debug.Assert(!Disposed);
            return _coreAnalysisData.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            Debug.Assert(!Disposed);
            return _coreAnalysisData.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            Debug.Assert(!Disposed);
            return Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            Debug.Assert(!Disposed);
            return _coreAnalysisData.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Debug.Assert(!Disposed);
            return _coreAnalysisData.GetEnumerator();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _coreAnalysisData.Free();
            }
        }
    }
}
