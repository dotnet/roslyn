// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Utilities
{
    internal class BidirectionalMap<TKey, TValue> : IBidirectionalMap<TKey, TValue>
    {
        public static readonly IBidirectionalMap<TKey, TValue> Empty =
            new BidirectionalMap<TKey, TValue>(ImmutableDictionary.Create<TKey, TValue>(), ImmutableDictionary.Create<TValue, TKey>());

        private readonly ImmutableDictionary<TKey, TValue> _forwardMap;
        private readonly ImmutableDictionary<TValue, TKey> _backwardMap;

        public BidirectionalMap(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            _forwardMap = ImmutableDictionary.CreateRange<TKey, TValue>(pairs);
            _backwardMap = ImmutableDictionary.CreateRange<TValue, TKey>(pairs.Select(p => KeyValuePairUtil.Create(p.Value, p.Key)));
        }

        private BidirectionalMap(ImmutableDictionary<TKey, TValue> forwardMap, ImmutableDictionary<TValue, TKey> backwardMap)
        {
            _forwardMap = forwardMap;
            _backwardMap = backwardMap;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _forwardMap.TryGetValue(key, out value);
        }

        public bool TryGetKey(TValue value, out TKey key)
        {
            return _backwardMap.TryGetValue(value, out key);
        }

        public bool ContainsKey(TKey key)
        {
            return _forwardMap.ContainsKey(key);
        }

        public bool ContainsValue(TValue value)
        {
            return _backwardMap.ContainsKey(value);
        }

        public IBidirectionalMap<TKey, TValue> RemoveKey(TKey key)
        {
            if (!_forwardMap.TryGetValue(key, out var value))
            {
                return this;
            }

            return new BidirectionalMap<TKey, TValue>(
                _forwardMap.Remove(key),
                _backwardMap.Remove(value));
        }

        public IBidirectionalMap<TKey, TValue> RemoveValue(TValue value)
        {
            if (!_backwardMap.TryGetValue(value, out var key))
            {
                return this;
            }

            return new BidirectionalMap<TKey, TValue>(
                _forwardMap.Remove(key),
                _backwardMap.Remove(value));
        }

        public IBidirectionalMap<TKey, TValue> Add(TKey key, TValue value)
        {
            return new BidirectionalMap<TKey, TValue>(
                _forwardMap.Add(key, value),
                _backwardMap.Add(value, key));
        }

        public IEnumerable<TKey> Keys => _forwardMap.Keys;

        public IEnumerable<TValue> Values => _backwardMap.Keys;

        public bool IsEmpty
        {
            get
            {
                return _backwardMap.Count == 0;
            }
        }

        public int Count
        {
            get
            {
                Debug.Assert(_forwardMap.Count == _backwardMap.Count);
                return _backwardMap.Count;
            }
        }

        public TValue GetValueOrDefault(TKey key)
        {
            if (TryGetValue(key, out var result))
            {
                return result;
            }

            return default;
        }

        public TKey GetKeyOrDefault(TValue value)
        {
            if (TryGetKey(value, out var result))
            {
                return result;
            }

            return default;
        }
    }
}
