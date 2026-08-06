// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace System.Collections.Generic;

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var existingValue))
        {
            return existingValue;
        }
        else
        {
            dictionary.Add(key, value);
            return value;
        }
    }

    public static TValue GetOrAdd<TKey, TValue>(
        this PooledDictionaryBuilder<TKey, TValue> dictionary,
        TKey key,
        TValue value)
        where TKey : notnull
    {
        if (dictionary.ContainsKey(key))
        {
            return dictionary[key];
        }
        else
        {
            dictionary.Add(key, value);
            return value;
        }
    }

    public static TValue GetOrAdd<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> func)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var existingValue))
        {
            return existingValue;
        }
        else
        {
            var value = func(key);
            dictionary.Add(key, value);
            return value;
        }
    }

    public static TValue GetOrAdd<TKey, TValue>(
        this PooledDictionaryBuilder<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> func)
        where TKey : notnull
    {
        if (dictionary.ContainsKey(key))
        {
            return dictionary[key];
        }
        else
        {
            var value = func(key);
            dictionary.Add(key, value);
            return value;
        }
    }
}
