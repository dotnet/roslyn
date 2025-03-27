// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents the parameter that is sent with textDocument/rangeFormatting message.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentRangeFormattingParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DocumentRangeFormattingParams : ITextDocumentParams
{
    /// <summary>
    /// Gets or sets the identifier for the text document to be formatted.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the selection range to be formatted.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the formatting options.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonRequired]
    public FormattingOptions Options
    {
        get;
        set;
    }
}
