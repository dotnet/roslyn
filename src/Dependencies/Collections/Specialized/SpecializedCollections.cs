// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static partial class SpecializedCollections
    {
        public static IEnumerator<T> EmptyEnumerator<T>()
        {
            return Empty.Enumerator<T>.Instance;
        }

        public static IEnumerable<T> EmptyEnumerable<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static ICollection<T> EmptyCollection<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static IList<T> EmptyList<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static IReadOnlyList<T> EmptyBoxedImmutableArray<T>()
        {
            return Empty.BoxedImmutableArray<T>.Instance;
        }

        public static IReadOnlyList<T> EmptyReadOnlyList<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static ISet<T> EmptySet<T>()
        {
            return Empty.Set<T>.Instance;
        }

        public static IReadOnlySet<T> EmptyReadOnlySet<T>()
        {
            return Empty.Set<T>.Instance;
        }

        public static IDictionary<TKey, TValue> EmptyDictionary<TKey, TValue>()
            where TKey : notnull
        {
            return Empty.Dictionary<TKey, TValue>.Instance;
        }

        public static IReadOnlyDictionary<TKey, TValue> EmptyReadOnlyDictionary<TKey, TValue>()
            where TKey : notnull
        {
            return Empty.Dictionary<TKey, TValue>.Instance;
        }

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

        public static IReadOnlyList<T> SingletonReadOnlyList<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static IList<T> SingletonList<T>(T value)
        {
            return new Singleton.List<T>(value);
        }

        public static IEnumerable<T> ReadOnlyEnumerable<T>(IEnumerable<T> values)
        {
            return new ReadOnly.Enumerable<IEnumerable<T>, T>(values);
        }

        public static ICollection<T> ReadOnlyCollection<T>(ICollection<T>? collection)
        {
            return collection == null || collection.Count == 0
                ? EmptyCollection<T>()
                : new ReadOnly.Collection<ICollection<T>, T>(collection);
        }

        public static ISet<T> ReadOnlySet<T>(ISet<T>? set)
        {
            return set == null || set.Count == 0
                ? EmptySet<T>()
                : new ReadOnly.Set<ISet<T>, T>(set);
        }

        public static IReadOnlySet<T> StronglyTypedReadOnlySet<T>(ISet<T>? set)
        {
            return set == null || set.Count == 0
                ? EmptyReadOnlySet<T>()
                : new ReadOnly.Set<ISet<T>, T>(set);
        }
    }
}
