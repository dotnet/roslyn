// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace System.Collections.Generic;

internal static class RoslynKeyValuePairExtensions
{
#if NET
    public static void Deconstruct<TKey, TValue>(KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
        => keyValuePair.Deconstruct(out key, out value);
#else
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
    {
        key = keyValuePair.Key;
        value = keyValuePair.Value;
    }
#endif
}

