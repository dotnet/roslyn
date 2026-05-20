// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal partial class ListPool<T>
{
    private sealed class Policy : PooledObjectPolicy
    {
        public static readonly Policy Default = new(DefaultMaximumObjectSize);

        private readonly int _maximumObjectSize;

        private Policy(int maximumObjectSize)
        {
            _maximumObjectSize = maximumObjectSize;
        }

        public static Policy Create(Optional<int> maximumObjectSize = default)
        {
            if (!maximumObjectSize.HasValue || maximumObjectSize.Value == Default._maximumObjectSize)
            {
                return Default;
            }

            return new(maximumObjectSize.GetValueOrDefault(DefaultMaximumObjectSize));
        }

        public override List<T> Create() => [];

        public override bool Return(List<T> list)
        {
            var count = list.Count;

            list.Clear();

            if (count > _maximumObjectSize)
            {
                list.TrimExcess();
            }

            return true;
        }
    }
}
