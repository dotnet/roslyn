// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents a partial (range-based) text document change event.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#textDocumentContentChangePartial">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class TextDocumentContentChangePartial
{
    /// <summary>
    /// Gets or sets the range of the document that changed.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the optional length of the range that got replaced.
    /// </summary>
    [JsonPropertyName("rangeLength")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RangeLength
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the new text for the range.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonRequired]
    public string Text
    {
        get;
        set;
    }
}
