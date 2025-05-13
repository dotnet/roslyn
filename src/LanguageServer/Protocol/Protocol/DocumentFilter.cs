// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a filter over certain types of documents
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentFilter">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class DocumentFilter
{
    /// <summary>
    /// Gets or sets a language id for the filter (e.g. 'typescript').
    /// </summary>
    [JsonPropertyName("language")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Language
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a Uri scheme (e.g. 'file' or 'untitled').
    /// </summary>
    [JsonPropertyName("scheme")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Scheme
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a glob pattern (e.g. '*.cs').
    /// </summary>
    [JsonPropertyName("pattern")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Pattern
    {
        get;
        set;
    }
}
