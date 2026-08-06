// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal static partial class SpecializedPools
{
    public static PooledObject<HashSet<T>> GetPooledReferenceEqualityHashSet<T>()
        where T : class
        => ReferenceEqualityHashSet<T>.GetPooledObject();

    public static PooledObject<HashSet<T>> GetPooledReferenceEqualityHashSet<T>(out HashSet<T> set)
        where T : class
        => ReferenceEqualityHashSet<T>.GetPooledObject(out set);

    public static PooledObject<HashSet<string>> GetPooledStringHashSet()
        => StringHashSet.GetPooledObject();

    public static PooledObject<HashSet<string>> GetPooledStringHashSet(out HashSet<string> set)
        => StringHashSet.GetPooledObject(out set);

    public static PooledObject<HashSet<string>> GetPooledStringHashSet(bool ignoreCase)
        => StringHashSet.GetPooledObject(ignoreCase);

    public static PooledObject<HashSet<string>> GetPooledStringHashSet(bool ignoreCase, out HashSet<string> set)
        => StringHashSet.GetPooledObject(ignoreCase, out set);

    public static PooledObject<Dictionary<string, TValue>> GetPooledStringDictionary<TValue>()
        => StringDictionary<TValue>.GetPooledObject();

    public static PooledObject<Dictionary<string, TValue>> GetPooledStringDictionary<TValue>(
        out Dictionary<string, TValue> map)
        => StringDictionary<TValue>.GetPooledObject(out map);

    public static PooledObject<Dictionary<string, TValue>> GetPooledStringDictionary<TValue>(bool ignoreCase)
        => StringDictionary<TValue>.GetPooledObject(ignoreCase);

    public static PooledObject<Dictionary<string, TValue>> GetPooledStringDictionary<TValue>(
        bool ignoreCase, out Dictionary<string, TValue> map)
        => StringDictionary<TValue>.GetPooledObject(ignoreCase, out map);
}
