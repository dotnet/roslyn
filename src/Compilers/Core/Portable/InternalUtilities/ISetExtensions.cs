// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class ISetExtensions
    {
        public static bool AddAll<T>(this ISet<T> set, IEnumerable<T> values)
        {
            var result = false;
            foreach (var v in values)
            {
                result |= set.Add(v);
            }

            return result;
        }

        public static bool AddAll<T>(this ISet<T> set, ImmutableArray<T> values)
        {
            var result = false;
            foreach (var v in values)
            {
                result |= set.Add(v);
            }

            return result;
        }

        public static bool RemoveAll<T>(this ISet<T> set, IEnumerable<T> values)
        {
            var result = false;
            foreach (var v in values)
            {
                result |= set.Remove(v);
            }

            return result;
        }

        public static bool RemoveAll<T>(this ISet<T> set, ImmutableArray<T> values)
        {
            var result = false;
            foreach (var v in values)
            {
                result |= set.Remove(v);
            }

            return result;
        }
    }
}
