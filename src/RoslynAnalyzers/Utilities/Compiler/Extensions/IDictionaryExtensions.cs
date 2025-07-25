// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Analyzer.Utilities.Extensions
{
    internal static class IDictionaryExtensions
    {
        extension<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary) where TKey : notnull
        {
            public bool IsEqualTo(IReadOnlyDictionary<TKey, TValue> other)
            => dictionary.Count == other.Count &&
                dictionary.Keys.All(key => other.ContainsKey(key) && dictionary[key]?.Equals(other[key]) == true);
        }
    }
}
