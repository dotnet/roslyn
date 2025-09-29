// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

#pragma warning disable CA1710 // Rename DictionaryAnalysisData to end in 'Dictionary'

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public sealed class DictionaryAnalysisData<TKey, TValue> : AbstractAnalysisData, IDictionary<TKey, TValue>
        where TKey : notnull
    {
#pragma warning disable CA2213 // Disposable fields should be disposed
        private PooledDictionary<TKey, TValue> _coreAnalysisData;
#pragma warning restore

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
            Debug.Assert(!IsDisposed);
            return _coreAnalysisData.ToImmutableDictionary();
        }

        public TValue this[TKey key]
        {
            get
            {
                Debug.Assert(!IsDisposed);
                return _coreAnalysisData[key];
            }
            set
            {
                Debug.Assert(!IsDisposed);
                _coreAnalysisData[key] = value;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                Debug.Assert(!IsDisposed);
                return _coreAnalysisData.Keys;
            }
        }

        public ICollection<TValue> Values =>
            // "Values" might be accessed during dispose.
            //Debug.Assert(!IsDisposed);
            _coreAnalysisData.Values;

        public int Count
        {
            get
            {
                Debug.Assert(!IsDisposed);
                return _coreAnalysisData.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                Debug.Assert(!IsDisposed);
                return ((IDictionary<TKey, TValue>)_coreAnalysisData).IsReadOnly;
            }
        }

        public void Add(TKey key, TValue value)
        {
            Debug.Assert(!IsDisposed);
            _coreAnalysisData.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Debug.Assert(!IsDisposed);
            _coreAnalysisData.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Debug.Assert(!IsDisposed);
            _coreAnalysisData.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            Debug.Assert(!IsDisposed);
            return ((IDictionary<TKey, TValue>)_coreAnalysisData).Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            Debug.Assert(!IsDisposed);
            return _coreAnalysisData.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Debug.Assert(!IsDisposed);
            ((IDictionary<TKey, TValue>)_coreAnalysisData).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Debug.Assert(!IsDisposed);
            return _coreAnalysisData.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            Debug.Assert(!IsDisposed);
            return _coreAnalysisData.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            Debug.Assert(!IsDisposed);
            return Remove(item.Key);
        }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member because of nullability attributes. https://github.com/dotnet/roslyn/issues/42552
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member because of nullability attributes.
        {
            Debug.Assert(!IsDisposed);
            return _coreAnalysisData.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Debug.Assert(!IsDisposed);
            return _coreAnalysisData.GetEnumerator();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                return;
            }

            if (disposing)
            {
                _coreAnalysisData.Free();
                _coreAnalysisData = null!;
            }

            base.Dispose(disposing);
        }
    }
}
