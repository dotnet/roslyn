// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal static class ImmutableDictionaryExtensions
{
    public static bool KeysEqual<TKey, TValue>(this ImmutableDictionary<TKey, TValue> self, ImmutableDictionary<TKey, TValue> other)
        where TKey : notnull
    {
        if (self.Count != other.Count)
        {
            return false;
        }

        if (self.IsEmpty)
        {
            return true;
        }

        foreach (var (key, _) in self)
        {
            if (!other.ContainsKey(key))
            {
                return false;
            }
        }

        if (self.KeyComparer != other.KeyComparer)
        {
            foreach (var (key, _) in other)
            {
                if (!self.ContainsKey(key))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
