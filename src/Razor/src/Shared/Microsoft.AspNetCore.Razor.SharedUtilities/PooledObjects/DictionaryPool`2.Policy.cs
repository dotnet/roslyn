// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class DictionaryPool<TKey, TValue>
    where TKey : notnull
{
    private sealed class Policy : PooledObjectPolicy
    {
        public static readonly Policy Default = new(comparer: null, DefaultMaximumObjectSize);

        private readonly IEqualityComparer<TKey>? _comparer;
        private readonly int _maximumObjectSize;

        private Policy(IEqualityComparer<TKey>? comparer, int maximumObjectSize)
        {
            ArgHelper.ThrowIfNegative(maximumObjectSize);

            _comparer = comparer;
            _maximumObjectSize = maximumObjectSize;
        }

        public static Policy Create(
            Optional<IEqualityComparer<TKey>?> comparer = default,
            Optional<int> maximumObjectSize = default)
        {
            if ((!comparer.HasValue || comparer.Value == Default._comparer) &&
                (!maximumObjectSize.HasValue || maximumObjectSize.Value == Default._maximumObjectSize))
            {
                return Default;
            }

            return new(
                comparer.GetValueOrDefault(null),
                maximumObjectSize.GetValueOrDefault(DefaultMaximumObjectSize));
        }

        public override Dictionary<TKey, TValue> Create() => new(_comparer);

        public override bool Return(Dictionary<TKey, TValue> map)
        {
            var count = map.Count;

            map.Clear();

            // If the map grew too large, don't return it to the pool.
            return count <= _maximumObjectSize;
        }
    }
}
