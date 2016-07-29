// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Roslyn.Utilities
{
    internal static class IReadOnlyDictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return default(TValue);
        }

        public static IEnumerable<T> GetEnumerableMetadata<T>(this IReadOnlyDictionary<string, object> metadata, string name)
        {
            object value = metadata.GetValueOrDefault(name);

            return value.TypeSwitch((IEnumerable<T> enumerable) => enumerable,
                                    (T s) => SpecializedCollections.SingletonEnumerable(s),
                                    _ => SpecializedCollections.EmptyEnumerable<T>());
        }
    }
}
