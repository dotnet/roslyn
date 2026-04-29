// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class HashSetPool<T>
{
    private sealed class Policy : PooledObjectPolicy
    {
        public static readonly Policy Default = new(comparer: EqualityComparer<T>.Default, DefaultMaximumObjectSize);

        public IEqualityComparer<T> Comparer { get; }

        private readonly int _maximumObjectSize;

        private Policy(IEqualityComparer<T>? comparer, int maximumObjectSize)
        {
            ArgHelper.ThrowIfNegative(maximumObjectSize);

            Comparer = comparer ?? EqualityComparer<T>.Default;
            _maximumObjectSize = maximumObjectSize;
        }

        public static Policy Create(
            Optional<IEqualityComparer<T>?> comparer = default,
            Optional<int> maximumObjectSize = default)
        {
            if ((!comparer.HasValue || comparer.Value is null || comparer.Value == Default.Comparer) &&
                (!maximumObjectSize.HasValue || maximumObjectSize.Value == Default._maximumObjectSize))
            {
                return Default;
            }

            return new(
                comparer.GetValueOrDefault(EqualityComparer<T>.Default) ?? EqualityComparer<T>.Default,
                maximumObjectSize.GetValueOrDefault(DefaultMaximumObjectSize));
        }

        public override HashSet<T> Create() => new(Comparer);

        public override bool Return(HashSet<T> set)
        {
            var count = set.Count;

            set.Clear();

            if (count > _maximumObjectSize)
            {
                set.TrimExcess();
            }

            return true;
        }
    }
}
