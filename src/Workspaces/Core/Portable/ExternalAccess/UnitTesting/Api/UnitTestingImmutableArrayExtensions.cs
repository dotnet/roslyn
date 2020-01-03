// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
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
