// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ICollectionExtensions
{
    public static ImmutableArray<T> WhereAsArray<T, TState>(this IEnumerable<T> values, Func<T, TState, bool> predicate, TState state)
    {
        using var _ = ArrayBuilder<T>.GetInstance(out var result);

        foreach (var value in values)
        {
            if (predicate(value, state))
                result.Add(value);
        }

        return result.ToImmutable();
    }

    public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T>? items)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (items != null)
        {
            foreach (var item in items)
            {
                collection.Remove(item);
            }
        }
    }

    public static void AddIfNotNull<T>(this ICollection<T> collection, T? value) where T : struct
    {
        if (value != null)
            collection.Add(value.Value);
    }

    public static void AddIfNotNull<T>(this ICollection<T> collection, T? value) where T : class
    {
        if (value != null)
            collection.Add(value);
    }
}
