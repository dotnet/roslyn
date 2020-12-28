// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Roslyn.Utilities
{
    internal static class IGroupingExtensions
    {
        public static void Deconstruct<TKey, TElement>(this IGrouping<TKey, TElement> grouping,
            out TKey key, out IEnumerable<TElement> values)
        {
            key = grouping.Key;
            values = grouping;
        }
    }
}
