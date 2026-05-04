// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Describes capabilities of the client's markdown parser
/// </summary>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#markdownClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class MarkdownClientCapabilities
{
    /// <summary>
    /// The name of the parser.
    /// </summary>
    [JsonPropertyName("parser")]
    [JsonRequired]
    public string Parser { get; init; }

    /// <summary>
    /// The version of the parser.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; init; }

    /// <summary>
    /// A list of HTML tags that the client allows/support in markdown
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("allowedTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? AllowedTags { get; init; }
}
