// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

internal static class ICollectionExtensions
{
    public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T>? items)
    {
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

    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? values)
    {
        if (values != null)
        {
            foreach (var item in values)
            {
                collection.Add(item);
            }
        }
    }

    public static void AddRange<T>(this ICollection<T> collection, ArrayBuilder<T>? values)
    {
        if (values != null)
        {
            foreach (var item in values)
                collection.Add(item);
        }
    }

    public static void AddRange<T>(this ICollection<T> collection, HashSet<T>? values)
    {
        if (values != null)
        {
            foreach (var item in values)
                collection.Add(item);
        }
    }

    public static void AddRange<TKey, TValue>(this ICollection<TKey> collection, Dictionary<TKey, TValue>.KeyCollection? keyCollection) where TKey : notnull
    {
        if (keyCollection != null)
        {
            foreach (var key in keyCollection)
                collection.Add(key);
        }
    }

    public static void AddRange<T>(this ICollection<T> collection, ImmutableArray<T> values)
    {
        if (!values.IsDefault)
        {
            foreach (var item in values)
            {
                collection.Add(item);
            }
        }
    }
}
