// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
