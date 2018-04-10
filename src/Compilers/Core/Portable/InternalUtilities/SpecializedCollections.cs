// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        public static IEnumerator<T> EmptyEnumerator<T>() => EmptyEnumerable<T>().GetEnumerator();

        public static IEnumerable<T> EmptyEnumerable<T>() => Array.Empty<T>();

        public static ICollection<T> EmptyCollection<T>() => Array.Empty<T>();

        public static IList<T> EmptyList<T>() => Array.Empty<T>();

        public static IReadOnlyList<T> EmptyReadOnlyList<T>() => Array.Empty<T>();

        public static ISet<T> EmptySet<T>() => ImmutableHashSet<T>.Empty;

        public static IReadOnlySet<T> EmptyReadOnlySet<T>() => Empty.ReadOnlySet<T>.Instance;

        public static IDictionary<TKey, TValue> EmptyDictionary<TKey, TValue>()
            => ImmutableDictionary<TKey, TValue>.Empty;

        public static IReadOnlyDictionary<TKey, TValue> EmptyReadOnlyDictionary<TKey, TValue>()
            => ImmutableDictionary<TKey, TValue>.Empty;

        public static IEnumerable<T> SingletonEnumerable<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static ICollection<T> SingletonCollection<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static IEnumerator<T> SingletonEnumerator<T>(T value)
        {
            return new Singleton.Enumerator<T>(value);
        }

        public static IList<T> SingletonList<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static IEnumerable<T> ReadOnlyEnumerable<T>(IEnumerable<T> values)
        {
            return new ReadOnly.Enumerable<IEnumerable<T>, T>(values);
        }

        public static ICollection<T> ReadOnlyCollection<T>(ICollection<T> collection)
        {
            return collection == null || collection.Count == 0
                ? EmptyCollection<T>()
                : new ReadOnly.Collection<ICollection<T>, T>(collection);
        }

        public static ISet<T> ReadOnlySet<T>(ISet<T> set)
        {
            return set == null || set.Count == 0
                ? EmptySet<T>()
                : new ReadOnly.Set<ISet<T>, T>(set);
        }

        public static IReadOnlySet<T> StronglyTypedReadOnlySet<T>(ISet<T> set)
        {
            return set == null || set.Count == 0
                ? EmptyReadOnlySet<T>()
                : new ReadOnly.Set<ISet<T>, T>(set);
        }
    }
}
