// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

/// <summary>
/// A representation of a Roslyn TextSpan that can be serialized with System.Text.Json
/// </summary>
internal sealed record RazorTextSpan
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    public static RazorTextSpan FromBounds(int start, int end)
    {
        return new RazorTextSpan
        {
            Start = start,
            Length = end - start,
        };
    }
}
