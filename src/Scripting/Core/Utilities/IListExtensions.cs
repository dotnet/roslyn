// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class IListExtensions
    {
        public static void AddRange<T>(this IList<T> list, ImmutableArray<T> items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }

        public static void AddRange<T>(this IList<T> list, T[] items)
        {
            foreach (var item in items)
            {
                list.Add(item);
            }
        }
    }
}
