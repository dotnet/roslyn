// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Helpers used for public API argument validation.
/// </summary>
internal static class PublicContract
{
    // Guidance on inlining:
    // Inline implementation of condition checking but don't inline the code that is only executed on failure.
    // This approach makes the common path efficient (both execution time and code size) 
    // while keeping the rarely executed code in a separate method.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IEnumerable<T> RequireNonNullItems<T>([NotNull] IEnumerable<T>? sequence, string argumentName) where T : class
    {
        if (sequence == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (sequence.Contains((T)null!))
        {
            ThrowArgumentItemNullException(sequence, argumentName);
        }

        return sequence;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RequireUniqueNonNullItems<T>([NotNull] IEnumerable<T>? sequence, string argumentName) where T : class
    {
        if (sequence == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        if (sequence.IndexOfNullOrDuplicateItem() >= 0)
        {
            ThrowArgumentItemNullOrDuplicateException(sequence, argumentName);
        }
    }

    /// <summary>
    /// Use to validate public API input for properties that are exposed as <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IReadOnlyList<T> ToBoxedImmutableArrayWithNonNullItems<T>(IEnumerable<T>? sequence, string argumentName) where T : class
    {
        var list = sequence.ToBoxedImmutableArray();

        if (list.Contains((T)null!))
        {
            ThrowArgumentItemNullException(list, argumentName);
        }

        return list;
    }

    /// <summary>
    /// Use to validate public API input for properties that are exposed as <see cref="IReadOnlyList{T}"/> and 
    /// whose items should be unique.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IReadOnlyList<T> ToBoxedImmutableArrayWithDistinctNonNullItems<T>(IEnumerable<T>? sequence, string argumentName) where T : class
    {
        var list = sequence.ToBoxedImmutableArray();

        if (list.IndexOfNullOrDuplicateItem() >= 0)
        {
            ThrowArgumentItemNullOrDuplicateException(list, argumentName);
        }

        return list;
    }

    private static int IndexOfNullOrDuplicateItem<T>(this IEnumerable<T> sequence) where T : class
        => (sequence is IReadOnlyList<T> list) ? IndexOfNullOrDuplicateItem(list) : EnumeratingIndexOfNullOrDuplicateItem(sequence);

    private static int EnumeratingIndexOfNullOrDuplicateItem<T>(IEnumerable<T> sequence) where T : class
    {
        using var _ = PooledHashSet<T>.GetInstance(out var set);

        var i = 0;
        foreach (var item in sequence)
        {
            if (item is null || !set.Add(item))
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    private static int IndexOfNullOrDuplicateItem<T>(this IReadOnlyList<T> list) where T : class
    {
        var length = list.Count;

        if (length == 0)
        {
            return -1;
        }

        if (length == 1)
        {
            return (list[0] is null) ? 0 : -1;
        }

        using var _ = PooledHashSet<T>.GetInstance(out var set);

        for (var i = 0; i < length; i++)
        {
            var item = list[i];
            if (item is null || !set.Add(item))
            {
                return i;
            }
        }

        return -1;
    }

    private static string MakeIndexedArgumentName(string argumentName, int index)
        => $"{argumentName}[{index}]";

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentItemNullOrDuplicateException<T>(IEnumerable<T> sequence, string argumentName) where T : class
    {
        var list = sequence.ToList();
        var index = list.IndexOfNullOrDuplicateItem();

        argumentName = MakeIndexedArgumentName(argumentName, index);

        throw (list[index] is null)
             ? new ArgumentNullException(argumentName)
             : new ArgumentException(CompilerExtensionsResources.Specified_sequence_has_duplicate_items, argumentName);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentItemNullException<T>(IEnumerable<T> sequence, string argumentName) where T : class
        => throw new ArgumentNullException(MakeIndexedArgumentName(argumentName, sequence.IndexOf(null)));
}
