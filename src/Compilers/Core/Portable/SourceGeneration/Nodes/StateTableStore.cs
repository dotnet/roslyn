// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis
{
    internal sealed class StateTableStore
    {
        private readonly ImmutableSegmentedDictionary<object, IStateTable> _tables;

        public static readonly StateTableStore Empty = new StateTableStore(ImmutableSegmentedDictionary<object, IStateTable>.Empty);

        private StateTableStore(ImmutableSegmentedDictionary<object, IStateTable> tables)
        {
            _tables = tables;
        }

        public bool TryGetValue(object key, [NotNullWhen(true)] out IStateTable? table) => _tables.TryGetValue(key, out table);

        public NodeStateTable<T> GetStateTableOrEmpty<T>(object input)
        {
            if (TryGetValue(input, out var output))
            {
                return (NodeStateTable<T>)output;
            }
            return NodeStateTable<T>.Empty;
        }

        public sealed class Builder
        {
            private readonly ImmutableSegmentedDictionary<object, IStateTable>.Builder _tableBuilder = ImmutableSegmentedDictionary.CreateBuilder<object, IStateTable>();

            public bool Contains(object key) => _tableBuilder.ContainsKey(key);

            public bool TryGetTable(object key, [NotNullWhen(true)] out IStateTable? table) => _tableBuilder.TryGetValue(key, out table);

            public void SetTable(object key, IStateTable table) => _tableBuilder[key] = table;

            public StateTableStore ToImmutable()
            {
                // we can cache the tables at this point, as we'll no longer be using them to determine current state
                var keys = _tableBuilder.Keys.ToArray();
                foreach (var key in keys)
                {
                    _tableBuilder[key] = _tableBuilder[key].AsCached();
                }

                return new StateTableStore(_tableBuilder.ToImmutable());
            }
        }
    }
}
