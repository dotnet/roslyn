// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace AnalyzerRunner
{
    internal static class Extensions
    {
        internal static void AddToInnerList<TKey, TValue>(this IDictionary<TKey, ImmutableList<TValue>> dictionary, TKey key, TValue item)
        {
            ImmutableList<TValue> items;

            if (dictionary.TryGetValue(key, out items))
            {
                dictionary[key] = items.Add(item);
            }
            else
            {
                dictionary.Add(key, ImmutableList.Create(item));
            }
        }

        internal static void AddToInnerSet<TKey, TValue>(this IDictionary<TKey, ImmutableHashSet<TValue>> dictionary, TKey key, TValue item)
        {
            ImmutableHashSet<TValue> items;

            if (dictionary.TryGetValue(key, out items))
            {
                dictionary[key] = items.Add(item);
            }
            else
            {
                dictionary.Add(key, ImmutableHashSet.Create(item));
            }
        }
    }
}
