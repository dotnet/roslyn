// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static partial class SpecializedCollections
    {
        public static readonly byte[] EmptyBytes = EmptyArray<byte>();
        public static readonly object[] EmptyObjects = EmptyArray<object>();

        public static T[] EmptyArray<T>()
        {
            return Empty.Array<T>.Instance;
        }

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

        public static IReadOnlyList<T> EmptyReadOnlyList<T>()
        {
            return Empty.List<T>.Instance;
        }

        public static ISet<T> EmptySet<T>()
        {
            return Empty.Set<T>.Instance;
        }

        public static IDictionary<TKey, TValue> EmptyDictionary<TKey, TValue>()
        {
            return Empty.Dictionary<TKey, TValue>.Instance;
        }

        public static IReadOnlyDictionary<TKey, TValue> EmptyReadOnlyDictionary<TKey, TValue>()
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

        public static ISet<T> ReadOnlySet<T>(IEnumerable<T> values)
        {
            var set = values as ISet<T>;
            if (set != null)
            {
                return ReadOnlySet(set);
            }

            HashSet<T> result = null;
            foreach (var item in values)
            {
                result = result ?? new HashSet<T>();
                result.Add(item);
            }

            return ReadOnlySet(result);
        }
    }
}
