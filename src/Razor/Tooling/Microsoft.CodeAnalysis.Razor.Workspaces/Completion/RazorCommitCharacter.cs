// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal readonly record struct RazorCommitCharacter(string Character, bool Insert = true)
{
    public static ImmutableArray<RazorCommitCharacter> CreateArray(ReadOnlySpan<string> characters, bool insert = true)
    {
        using var converted = new PooledArrayBuilder<RazorCommitCharacter>(capacity: characters.Length);

        foreach (var ch in characters)
        {
            converted.Add(new(ch, insert));
        }

        return converted.ToImmutableAndClear();
    }
}
