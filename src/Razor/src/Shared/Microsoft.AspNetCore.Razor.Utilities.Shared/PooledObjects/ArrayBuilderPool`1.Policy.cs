// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class ArrayBuilderPool<T>
{
    private sealed class Policy : PooledObjectPolicy
    {
        // This is the default initial capacity for ImmutableArray<T>.Builder.
        private const int DefaultInitialCapacity = 8;

        public static readonly Policy Default = new(DefaultInitialCapacity, DefaultMaximumObjectSize);

        private readonly int _initialCapacity;
        private readonly int _maximumObjectSize;

        private Policy(int initialCapacity, int maximumObjectSize)
        {
            ArgHelper.ThrowIfNegative(initialCapacity);
            ArgHelper.ThrowIfNegative(maximumObjectSize);

            _initialCapacity = initialCapacity;
            _maximumObjectSize = maximumObjectSize;
        }

        public static Policy Create(
            Optional<int> initialCapacity = default,
            Optional<int> maximumObjectSize = default)
        {
            if ((!initialCapacity.HasValue || initialCapacity.Value == Default._initialCapacity) &&
                (!maximumObjectSize.HasValue || maximumObjectSize.Value == Default._maximumObjectSize))
            {
                return Default;
            }

            return new(
                initialCapacity.GetValueOrDefault(DefaultInitialCapacity),
                maximumObjectSize.GetValueOrDefault(DefaultMaximumObjectSize));
        }

        public override ImmutableArray<T>.Builder Create()
            => ImmutableArray.CreateBuilder<T>();

        public override bool Return(ImmutableArray<T>.Builder builder)
        {
            var count = builder.Count;

            builder.Clear();

            if (count > _maximumObjectSize)
            {
                builder.Capacity = _maximumObjectSize;
            }

            return true;
        }
    }
}
