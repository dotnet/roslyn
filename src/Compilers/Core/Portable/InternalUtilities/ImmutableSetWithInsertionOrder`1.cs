// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Roslyn.Utilities
{
    internal sealed class ImmutableSetWithInsertionOrder<T> : IEnumerable<T>
        where T : notnull
    {
        public static readonly ImmutableSetWithInsertionOrder<T> Empty = new ImmutableSetWithInsertionOrder<T>(ImmutableDictionary.Create<T, uint>(), 0u);

        private readonly ImmutableDictionary<T, uint> _map;
        private readonly uint _nextElementValue;

        private ImmutableSetWithInsertionOrder(ImmutableDictionary<T, uint> map, uint nextElementValue)
        {
            _map = map;
            _nextElementValue = nextElementValue;
        }

        public int Count
        {
            get { return _map.Count; }
        }

        public bool Contains(T value)
        {
            return _map.ContainsKey(value);
        }

        public ImmutableSetWithInsertionOrder<T> Add(T value)
        {
            // no reason to cause allocations if value is already in the set
            if (_map.ContainsKey(value))
            {
                return this;
            }

            return new ImmutableSetWithInsertionOrder<T>(_map.Add(value, _nextElementValue), _nextElementValue + 1u);
        }

        public ImmutableSetWithInsertionOrder<T> Remove(T value)
        {
            var modifiedMap = _map.Remove(value);
            if (modifiedMap == _map)
            {
                // no reason to cause allocations if value is missing
                return this;
            }

            return this.Count == 1 ? Empty : new ImmutableSetWithInsertionOrder<T>(modifiedMap, _nextElementValue);
        }

        public IEnumerable<T> InInsertionOrder
        {
            get { return _map.OrderBy(kv => kv.Value).Select(kv => kv.Key); }
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", this) + "}";
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _map.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _map.Keys.GetEnumerator();
        }
    }
}
