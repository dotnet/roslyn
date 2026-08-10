// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static partial class SpecializedPools
{
    /// <summary>
    /// Pooled <see cref="Dictionary{TKey, TValue}"/> instances when the key is of type <see cref="string"/>.
    /// </summary>
    /// 
    /// <remarks>
    /// Instances originating from this pool are intended to be short-lived and are suitable
    /// for temporary work. Do not return them as the results of methods or store them in fields.
    /// </remarks>
    internal static class StringDictionary<TValue>
    {
        public static readonly DictionaryPool<string, TValue> Ordinal = DictionaryPool<string, TValue>.Create(StringComparer.Ordinal);
        public static readonly DictionaryPool<string, TValue> OrdinalIgnoreCase = DictionaryPool<string, TValue>.Create(StringComparer.OrdinalIgnoreCase);

        public static PooledObject<Dictionary<string, TValue>> GetPooledObject()
            => Ordinal.GetPooledObject();

        public static PooledObject<Dictionary<string, TValue>> GetPooledObject(out Dictionary<string, TValue> map)
            => Ordinal.GetPooledObject(out map);

        public static PooledObject<Dictionary<string, TValue>> GetPooledObject(bool ignoreCase)
            => ignoreCase
                ? OrdinalIgnoreCase.GetPooledObject()
                : Ordinal.GetPooledObject();

        public static PooledObject<Dictionary<string, TValue>> GetPooledObject(bool ignoreCase, out Dictionary<string, TValue> map)
            => ignoreCase
                ? OrdinalIgnoreCase.GetPooledObject(out map)
                : Ordinal.GetPooledObject(out map);
    }
}
