// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Immutable;

internal static partial class ImmutableArrayExtensions
{
    /// <summary>
    /// Returns an empty array if the input array is null (default).
    /// </summary>
    public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T> array)
        => array.IsDefault ? ImmutableArray<T>.Empty : array;

    /// <summary>
    /// Returns an empty array if the input nullable value type is null or the underlying array is null (default).
    /// </summary>
    public static ImmutableArray<T> NullToEmpty<T>(this ImmutableArray<T>? array)
        => array switch
        {
            null or { IsDefault: true } => ImmutableArray<T>.Empty,
            { } underlying => underlying
        };
}
