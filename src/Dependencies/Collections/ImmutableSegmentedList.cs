// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static class ImmutableSegmentedList
    {
        public static ImmutableSegmentedList<T> Create<T>() => throw null!;

        public static ImmutableSegmentedList<T> Create<T>(T item) => throw null!;

        public static ImmutableSegmentedList<T> Create<T>(params T[] items) => throw null!;

        public static ImmutableSegmentedList<T>.Builder CreateBuilder<T>() => throw null!;

        public static ImmutableSegmentedList<T> CreateRange<T>(IEnumerable<T> items) => throw null!;

        public static ImmutableSegmentedList<T> ToImmutableSegmentedList<T>(this IEnumerable<T> source) => throw null!;
    }
}
