// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;

namespace Roslyn.Utilities;

internal static class IReadOnlyDictionaryExtensions
{
    extension<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary) where TKey : notnull
    {
        public TValue? GetValueOrDefault(TKey key)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return default!;
        }
    }

    extension(IReadOnlyDictionary<string, object> metadata)
    {
        public IEnumerable<T> GetEnumerableMetadata<T>(string name)
        {
            switch (metadata.GetValueOrDefault(name))
            {
                case IEnumerable<T> enumerable: return enumerable;
                case T s: return SpecializedCollections.SingletonEnumerable(s);
                default: return [];
            }
        }
    }

    extension<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary)
        where TKey : notnull
        where TValue : class
    {
        public IReadOnlyDictionary<TKey, TValue?> AsNullable()
        {
            // this is a safe cast, even though the language doesn't allow the interface to be 'out TValue'
            return dictionary!;
        }
    }
}
