// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Utilities to aid work with the LSP Extensions.
/// </summary>
internal static class VSInternalExtensionUtilities
{
    /// <summary>
    /// Returns a string read from the <see cref="Utf8JsonReader"/>. If the string is small enough
    /// to fit into the provided <paramref name="scratchChars"/>, no allocations will be needed.
    /// </summary>
    internal static ReadOnlySpan<char> GetStringSpan(this ref readonly Utf8JsonReader reader, Span<char> scratchChars)
    {
        var valueLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;

        if (valueLength <= scratchChars.Length)
        {
            // If the value fits into the scratch buffer, copy it there, and return a span from that.
            var actualLength = reader.CopyString(scratchChars);

            return scratchChars.Slice(0, actualLength);
        }
        else
        {
            // Otherwise, ask the reader to allocate a string and return a span from that.
            return reader.GetString().AsSpan();
        }
    }
}
