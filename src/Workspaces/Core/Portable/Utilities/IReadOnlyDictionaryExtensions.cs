// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class IReadOnlyDictionaryExtensions
    {
        [return: MaybeNull]
        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
            where TKey : notnull
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return default!;
        }

        public static IEnumerable<T> GetEnumerableMetadata<T>(this IReadOnlyDictionary<string, object> metadata, string name)
        {
            switch (metadata.GetValueOrDefault(name))
            {
                case IEnumerable<T> enumerable: return enumerable;
                case T s: return SpecializedCollections.SingletonEnumerable(s);
                default: return SpecializedCollections.EmptyEnumerable<T>();
            }
        }
    }
}
