// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the data returned by a <c>textDocument/hover</c> request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#hover">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class Hover
{
    /// <summary>
    /// The hover's content
    /// </summary>
    [JsonPropertyName("contents")]
    [JsonRequired]
#pragma warning disable CS0618 // MarkedString is obsolete but this property is not
    public SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent> Contents { get; set; }
#pragma warning restore CS0618

    /// <summary>
    /// An optional range inside a text document that is used to visualize the applicable
    /// range of the hover, e.g. by changing the background color.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Range? Range { get; set; }
}
