// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities;

internal static partial class ImmutableArrayExtensions
{
    extension<T>(HashSet<T> set)
    {
        public ImmutableArray<T> ToImmutableArray()
        {
            // [.. set] currently allocates, even for the empty case.  Workaround that until that is solved by the compiler.
            if (set.Count == 0)
                return [];

            return [.. set];
        }
    }

    extension<T>(ImmutableArray<T> items)
    {
        public bool Contains(T item, IEqualityComparer<T>? equalityComparer)
        => items.IndexOf(item, 0, equalityComparer) >= 0;
    }

    extension<T>(T[]? items)
    {
        public ImmutableArray<T> ToImmutableArrayOrEmpty()
        {
            if (items == null)
            {
                return [];
            }

            return ImmutableArray.Create<T>(items);
        }
    }

    extension<T>(ImmutableArray<T>.Builder builder)
    {
        public ImmutableArray<T> ToImmutableAndClear()
        {
            if (builder.Count == 0)
                return [];

            if (builder.Count == builder.Capacity)
                return builder.MoveToImmutable();

            var result = builder.ToImmutable();
            builder.Clear();
            return result;
        }
    }
}
