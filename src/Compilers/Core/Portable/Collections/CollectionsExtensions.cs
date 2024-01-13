// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    internal static class CollectionsExtensions
    {
        internal static bool IsNullOrEmpty<T>([NotNullWhen(returnValue: false)] this ICollection<T>? collection)
        {
            return collection == null || collection.Count == 0;
        }

        internal static bool IsNullOrEmpty<T>([NotNullWhen(returnValue: false)] this IReadOnlyCollection<T>? collection)
        {
            return collection == null || collection.Count == 0;
        }

        internal static bool IsNullOrEmpty<T>([NotNullWhen(returnValue: false)] this ImmutableHashSet<T>? hashSet)
        {
            return hashSet == null || hashSet.Count == 0;
        }
    }
}
