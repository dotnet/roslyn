// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static class ImmutableSegmentedDictionaryBuilderExtensions
    {
        public static bool TryAdd<T>(
            this ImmutableSegmentedDictionary<T, VoidResult>.Builder dictionary,
            T value)
            where T : notnull
        {
#if NETCOREAPP
            return dictionary.TryAdd(value, default);
#else
            if (dictionary.ContainsKey(value))
                return false;

            dictionary[value] = default;
            return true;
#endif
        }
    }
}
