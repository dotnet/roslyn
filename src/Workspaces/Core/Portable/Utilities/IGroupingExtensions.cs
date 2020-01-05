// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
