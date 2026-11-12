// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class which represents the parameter that is sent with textDocument/rangesFormatting message.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#documentRangesFormattingParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class DocumentRangesFormattingParams : ITextDocumentParams, IWorkDoneProgressParams
{
    /// <summary>
    /// Gets or sets the identifier for the text document to be formatted.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// Gets or sets the ranges to format.
    /// </summary>
    [JsonPropertyName("ranges")]
    [JsonRequired]
    public Range[] Ranges { get; set; }

    /// <summary>
    /// Gets or sets the formatting options.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonRequired]
    public FormattingOptions Options { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
}
