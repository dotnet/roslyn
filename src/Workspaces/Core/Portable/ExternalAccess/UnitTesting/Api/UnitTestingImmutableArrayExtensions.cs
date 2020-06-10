// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal static class UnitTestingImmutableArrayExtensions
    {
        public static ImmutableArray<byte> ToImmutable(this MemoryStream stream)
            => ImmutableArrayExtensions.ToImmutable(stream);

        public static bool SetEquals<T>(this ImmutableArray<T> array1, ImmutableArray<T> array2, IEqualityComparer<T> comparer)
            => ImmutableArrayExtensions.SetEquals(array1, array2, comparer);

        public static ImmutableArray<T> AsImmutable<T>(this IEnumerable<T> items)
            => ImmutableArrayExtensions.AsImmutable(items);
    }
}
