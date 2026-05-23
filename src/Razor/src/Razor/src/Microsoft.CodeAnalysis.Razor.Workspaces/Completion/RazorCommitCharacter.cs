// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal readonly record struct RazorCommitCharacter(string Character, bool Insert = true)
{
    // Cache keyed by ImmutableArray<RazorCommitCharacter>, which uses reference equality
    // on its underlying array. Since all providers use static ImmutableArray fields,
    // every call with the same field hits the cache and returns the same output array.
    // Shared array instances enable CompletionListOptimizer's ReferenceEquals-based grouping.
    // Both representations are cached independently since a source may be requested as
    // string[] (non-VS clients) or VSInternalCommitCharacter[] (VS clients) depending on capabilities.
    private static readonly Dictionary<ImmutableArray<RazorCommitCharacter>, (string[]? Strings, VSInternalCommitCharacter[]? VsChars)> s_cache = [];

    public static ImmutableArray<RazorCommitCharacter> CreateArray(ReadOnlySpan<string> characters, bool insert = true)
    {
        var array = new RazorCommitCharacter[characters.Length];

        for (var i = 0; i < characters.Length; i++)
        {
            array[i] = new(characters[i], insert);
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(array);
    }

    /// <summary>
    /// Converts to a <see cref="SumType{T1, T2}"/> of <c>string[]</c> or <c>VSInternalCommitCharacter[]</c>,
    /// suitable for assigning to <see cref="VSInternalCompletionItem.VsCommitCharacters"/>.
    /// Returns <c>string[]</c> when all items have the default <see cref="Insert"/> value (<see langword="true"/>),
    /// avoiding SumType deserialization exceptions. Returns a cached array when the same
    /// <paramref name="source"/> is passed (by reference identity of the underlying array).
    /// </summary>
    public static SumType<string[], VSInternalCommitCharacter[]> ToVsCommitCharacters(ImmutableArray<RazorCommitCharacter> source)
    {
        lock (s_cache)
        {
            if (s_cache.TryGetValue(source, out var entry))
            {
                return entry.VsChars is not null
                    ? entry.VsChars
                    : entry.Strings!;
            }

            foreach (var ch in source)
            {
                if (!ch.Insert)
                {
                    var vsChars = source.SelectAsPlainArray(static c => new VSInternalCommitCharacter() { Character = c.Character, Insert = c.Insert });
                    s_cache[source] = (null, vsChars);

                    return vsChars;
                }
            }

            return ToStringCommitCharacters(source);
        }
    }

    /// <summary>
    /// Converts to string[], returning a cached array when the same <paramref name="source"/>
    /// is passed (by reference identity of the underlying array).
    /// </summary>
    public static string[] ToStringCommitCharacters(ImmutableArray<RazorCommitCharacter> source)
    {
        lock (s_cache)
        {
            if (s_cache.TryGetValue(source, out var entry) && entry.Strings is not null)
            {
                return entry.Strings;
            }

            var result = source.SelectAsPlainArray(static c => c.Character);
            s_cache[source] = (result, entry.VsChars);

            return result;
        }
    }
}
