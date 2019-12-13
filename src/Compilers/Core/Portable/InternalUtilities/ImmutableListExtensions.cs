// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
