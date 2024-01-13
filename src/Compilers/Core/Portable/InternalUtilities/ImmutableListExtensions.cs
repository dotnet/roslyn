// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class ImmutableListExtensions
    {
        internal static ImmutableList<T> ToImmutableListOrEmpty<T>(this T[]? items)
        {
            if (items == null)
            {
                return ImmutableList.Create<T>();
            }

            return ImmutableList.Create<T>(items);
        }

        internal static ImmutableList<T> ToImmutableListOrEmpty<T>(this IEnumerable<T>? items)
        {
            if (items == null)
            {
                return ImmutableList.Create<T>();
            }

            return ImmutableList.CreateRange<T>(items);
        }
    }
}
