// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static class IReadOnlyListExtensions
    {
        public static bool Contains<T>(this IReadOnlyList<T> list, T item, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            for (int i = 0; i < list.Count; i++)
            {
                if (comparer.Equals(item, list[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
