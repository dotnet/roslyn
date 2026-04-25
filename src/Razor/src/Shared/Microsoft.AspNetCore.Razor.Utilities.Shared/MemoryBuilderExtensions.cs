// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor;

internal static class MemoryBuilderExtensions
{
    public static void Append(this ref MemoryBuilder<ReadOnlyMemory<char>> builder, string value)
    {
        builder.Append(value.AsMemory());
    }

    /// <summary>
    ///  Builds a string from the contents of a <see cref="MemoryBuilder{T}"/> where T is <see cref="ReadOnlyMemory{T}"/> of <see cref="char"/>.
    /// </summary>
    /// <param name="builder">
    ///  The memory builder containing <see cref="ReadOnlyMemory{T}"/> of <see cref="char"/> chunks.
    /// </param>
    /// <returns>
    ///  A string created by concatenating all chunks in the builder. Returns <see cref="string.Empty"/> if the builder is empty.
    ///  If the builder contains only one chunk, returns that chunk directly to avoid allocations when possible.
    /// </returns>
    /// <remarks>
    ///  This method is optimized for the common case where the builder contains only one chunk,
    ///  in which case no additional allocation is made if the chunk represents an entire string.
    /// </remarks>
    public static string CreateString(this ref MemoryBuilder<ReadOnlyMemory<char>> builder)
    {
        if (builder.Length == 0)
        {
            return string.Empty;
        }

        if (builder.Length == 1)
        {
            // If we only have one chunk, we can return it directly.
            // It is guaranteed to be the same as the content that was originally
            // passed in. Calling ToString() on this will not allocate if the
            // original content represented an entire string.

            return builder.AsMemory().Span[0].ToString();
        }

        var chunks = builder.AsMemory();
        var length = 0;

        foreach (var chunk in chunks.Span)
        {
            length += chunk.Length;
        }

        return string.Create(length, chunks, (destination, chunks) =>
        {
            foreach (var chunk in chunks.Span)
            {
                chunk.Span.CopyTo(destination);
                destination = destination[chunk.Length..];
            }
        });
    }
}
