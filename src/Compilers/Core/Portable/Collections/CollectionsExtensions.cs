// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
