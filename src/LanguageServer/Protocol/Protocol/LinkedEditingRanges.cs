// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the response of an LinkedEditingRanges response.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#linkedEditingRanges">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class LinkedEditingRanges
{
    /// <summary>
    /// A list of ranges that can be renamed together. The ranges must have
    /// identical length and contain identical text content. The ranges cannot
    /// overlap.
    /// </summary>
    [JsonPropertyName("ranges")]
    [JsonRequired]
    public Range[] Ranges
    {
        get;
        set;
    }

    /// <summary>
    /// An optional word pattern (regular expression) that describes valid
    /// contents for the given ranges. If no pattern is provided, the client
    /// configuration's word pattern will be used.
    /// </summary>
    [JsonPropertyName("wordPattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WordPattern
    {
        get;
        set;
    }
}
