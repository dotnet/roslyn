// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal static class KeyValuePairUtil
    {
        public static KeyValuePair<TKey, TValue> ToKeyValuePair<TKey, TValue>(this (TKey, TValue) tuple)
            => KeyValuePair.Create(tuple.Item1, tuple.Item2);
    }
}

#if NETFRAMEWORK || NETSTANDARD2_0
namespace System.Collections.Generic
{
    // In netcoreapp3.1 non-generic KeyValuePair type is defined in System.Collections.Generic
    internal static class KeyValuePair
    {
        public static KeyValuePair<K, V> Create<K, V>(K key, V value)
            => new KeyValuePair<K, V>(key, value);
    }
}

// In netcoreapp3.1 Deconstruct is defined on KeyValuePair<K, V>
internal static class KeyValuePairExtensions
{
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
    {
        key = keyValuePair.Key;
        value = keyValuePair.Value;
    }
}
#endif
