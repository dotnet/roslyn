// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal static class IDictionaryExtensions
{
    /// <summary>
    /// Removes entries from a dictionary based on a specified condition. The condition is defined by a function that
    /// evaluates each key-value pair.
    /// </summary>
    public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<TKey, TValue, bool> predicate)
        where TKey : notnull
        => RemoveAll(dictionary, static (key, value, predicate) => predicate(key, value), predicate);

    /// <summary>
    /// Removes entries from a dictionary based on a specified condition. The condition is defined by a function that
    /// evaluates each key-value pair.
    /// </summary>
    public static void RemoveAll<TKey, TValue, TArg>(this IDictionary<TKey, TValue> dictionary, Func<TKey, TValue, TArg, bool> predicate, TArg arg)
        where TKey : notnull
    {
        if (dictionary.Count == 0)
        {
            return;
        }
#if NET
        // .NET implementation of Dictionary<,> supports removing while enumerating:
        if (dictionary is Dictionary<TKey, TValue> dict)
        {
            foreach (var (key, value) in dict)
            {
                if (predicate(key, value, arg))
                {
                    dict.Remove(key);
                }
            }

            return;
        }
#endif
        var keysToRemove = ArrayBuilder<TKey>.GetInstance();
        foreach (var (key, value) in dictionary)
        {
            if (predicate(key, value, arg))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            dictionary.Remove(key);
        }

        keysToRemove.Free();
    }
}
