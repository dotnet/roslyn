// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Roslyn.Utilities
{
    internal class BidirectionalMap<TKey, TValue> : IBidirectionalMap<TKey, TValue>
    {
        public static readonly IBidirectionalMap<TKey, TValue> Empty =
            new BidirectionalMap<TKey, TValue>(ImmutableDictionary.Create<TKey, TValue>(), ImmutableDictionary.Create<TValue, TKey>());

        private readonly ImmutableDictionary<TKey, TValue> forwardMap;
        private readonly ImmutableDictionary<TValue, TKey> backwardMap;

        public BidirectionalMap(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            this.forwardMap = ImmutableDictionary.CreateRange<TKey, TValue>(pairs);
            this.backwardMap = ImmutableDictionary.CreateRange<TValue, TKey>(pairs.Select(p => KeyValuePair.Create(p.Value, p.Key)));
        }

        private BidirectionalMap(ImmutableDictionary<TKey, TValue> forwardMap, ImmutableDictionary<TValue, TKey> backwardMap)
        {
            this.forwardMap = forwardMap;
            this.backwardMap = backwardMap;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return forwardMap.TryGetValue(key, out value);
        }

        public bool TryGetKey(TValue value, out TKey key)
        {
            return backwardMap.TryGetValue(value, out key);
        }

        public bool ContainsKey(TKey key)
        {
            return forwardMap.ContainsKey(key);
        }

        public bool ContainsValue(TValue value)
        {
            return backwardMap.ContainsKey(value);
        }

        public IBidirectionalMap<TKey, TValue> RemoveKey(TKey key)
        {
            TValue value;
            if (!forwardMap.TryGetValue(key, out value))
            {
                return this;
            }

            return new BidirectionalMap<TKey, TValue>(
                forwardMap.Remove(key),
                backwardMap.Remove(value));
        }

        public IBidirectionalMap<TKey, TValue> RemoveValue(TValue value)
        {
            TKey key;
            if (!backwardMap.TryGetValue(value, out key))
            {
                return this;
            }

            return new BidirectionalMap<TKey, TValue>(
                forwardMap.Remove(key),
                backwardMap.Remove(value));
        }

        public IBidirectionalMap<TKey, TValue> Add(TKey key, TValue value)
        {
            return new BidirectionalMap<TKey, TValue>(
                forwardMap.Add(key, value),
                backwardMap.Add(value, key));
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                return forwardMap.Keys;
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                return backwardMap.Keys;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return backwardMap.Count == 0;
            }
        }

        public int Count
        {
            get
            {
                Contract.Requires(forwardMap.Count == backwardMap.Count);
                return backwardMap.Count;
            }
        }

        public TValue GetValueOrDefault(TKey key)
        {
            TValue result;
            if (TryGetValue(key, out result))
            {
                return result;
            }

            return default(TValue);
        }

        public TKey GetKeyOrDefault(TValue value)
        {
            TKey result;
            if (TryGetKey(value, out result))
            {
                return result;
            }

            return default(TKey);
        }
    }
}