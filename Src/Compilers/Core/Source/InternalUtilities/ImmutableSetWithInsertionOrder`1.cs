// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Roslyn.Utilities
{
    internal sealed class ImmutableSetWithInsertionOrder<T> : IEnumerable<T>
    {
        public static readonly ImmutableSetWithInsertionOrder<T> Empty = new ImmutableSetWithInsertionOrder<T>(ImmutableDictionary.Create<T, uint>(), 0u);

        private readonly ImmutableDictionary<T, uint> map;
        private uint nextElementValue;

        private ImmutableSetWithInsertionOrder(ImmutableDictionary<T, uint> map, uint nextElementValue)
        {
            this.map = map;
            this.nextElementValue = nextElementValue;
        }

        public int Count
        {
            get { return map.Count; }
        }

        public bool Contains(T value)
        {
            return this.map.ContainsKey(value);
        }

        public ImmutableSetWithInsertionOrder<T> Add(T value)
        {
            // no reason to cause allocations if value is already in the set
            if (map.ContainsKey(value))
            {
                return this;
            }

            return new ImmutableSetWithInsertionOrder<T>(map.Add(value, nextElementValue), nextElementValue + 1u);
        }

        public ImmutableSetWithInsertionOrder<T> Remove(T value)
        {
            // no reason to cause allocations if value is missing
            if (!map.ContainsKey(value))
            {
                return this;
            }

            return this.Count == 1 ? Empty : new ImmutableSetWithInsertionOrder<T>(map.Remove(value), nextElementValue);
        }

        public IEnumerable<T> InInsertionOrder
        {
            get { return this.map.OrderBy(kv => kv.Value).Select(kv => kv.Key); }
        }

        public override string ToString()
        {
            return "{" + string.Join(", ", this) + "}";
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.map.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.map.Keys.GetEnumerator();
        }
    }
}
