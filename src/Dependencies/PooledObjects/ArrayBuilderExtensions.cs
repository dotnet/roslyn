// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.PooledObjects;

/// <summary>
/// <see cref="ArrayBuilder{T}"/> methods that can't be defined on the type itself.
/// </summary>
internal static class ArrayBuilderExtensions
{
    public static ImmutableArray<T> ToImmutableOrEmptyAndFree<T>(this ArrayBuilder<T>? builder)
        => builder?.ToImmutableAndFree() ?? [];

    public static void AddIfNotNull<T>(this ArrayBuilder<T> builder, T? value)
        where T : struct
    {
        if (value != null)
        {
            builder.Add(value.Value);
        }
    }

    public static void AddIfNotNull<T>(this ArrayBuilder<T> builder, T? value)
        where T : class
    {
        if (value != null)
        {
            builder.Add(value);
        }
    }
}
