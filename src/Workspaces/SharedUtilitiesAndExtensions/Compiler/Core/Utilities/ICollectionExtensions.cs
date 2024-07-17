// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

internal static class ICollectionExtensions
{
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T>? values)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

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
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        if (values != null)
        {
            foreach (var item in values)
                collection.Add(item);
        }
    }

    public static void AddRange<T>(this ICollection<T> collection, HashSet<T>? values)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        if (values != null)
        {
            foreach (var item in values)
                collection.Add(item);
        }
    }

    public static void AddRange<TKey, TValue>(this ICollection<TKey> collection, Dictionary<TKey, TValue>.KeyCollection? keyCollection) where TKey : notnull
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        if (keyCollection != null)
        {
            foreach (var key in keyCollection)
                collection.Add(key);
        }
    }

    public static void AddRange<T>(this ICollection<T> collection, ImmutableArray<T> values)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (!values.IsDefault)
        {
            foreach (var item in values)
            {
                collection.Add(item);
            }
        }
    }
}
