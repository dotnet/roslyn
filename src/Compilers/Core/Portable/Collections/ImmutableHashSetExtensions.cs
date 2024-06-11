// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis;

internal static class ImmutableHashSetExtensions
{
    /// <summary>
    /// Performs a <see cref="ImmutableHashSet{T}.SetEquals"/> comparison without allocating an intermediate
    /// <see cref="HashSet{T}"/>.
    /// </summary>
    /// <seealso href="https://github.com/dotnet/runtime/issues/90986"/>
    public static bool SetEqualsWithoutIntermediateHashSet<T>(this ImmutableHashSet<T> set, ImmutableHashSet<T> other)
    {
        if (set is null)
            throw new ArgumentNullException(nameof(set));
        if (other is null)
            throw new ArgumentNullException(nameof(other));

        if (ReferenceEquals(set, other))
            return true;

        var otherSet = other.WithComparer(set.KeyComparer);
        if (set.Count != otherSet.Count)
            return false;

        foreach (var item in other)
        {
            if (!set.Contains(item))
                return false;
        }

        return true;
    }
}
