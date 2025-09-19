// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.PooledObjects.Extensions
{
    internal static class PooledHashSetExtensions
    {
        public static void AddRange<T>(this PooledHashSet<T> builder, IEnumerable<T> set2)
        {
            foreach (var item in set2)
            {
                builder.Add(item);
            }
        }
    }
}

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal sealed partial class PooledHashSet<T>
    {
        public ImmutableHashSet<T> ToImmutableAndFree()
        {
            ImmutableHashSet<T> result;
            if (Count == 0)
            {
                result = ImmutableHashSet<T>.Empty;
            }
            else
            {
                result = this.ToImmutableHashSet(Comparer);
                this.Clear();
            }

            _pool?.Free(this);
            return result;
        }

        public ImmutableHashSet<T> ToImmutable()
            => Count == 0 ? ImmutableHashSet<T>.Empty : this.ToImmutableHashSet(Comparer);
    }
}
