// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Parameters for 'textDocument/semanticTokens/full/delta' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensDeltaParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class SemanticTokensDeltaParams : ITextDocumentParams, IWorkDoneProgressParams, IPartialResultParams<SemanticTokensDeltaPartialResult>
{
    /// <summary>
    /// Gets or sets an identifier for the document to fetch semantic tokens from.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// The result id of a previous response. The result Id can either point to
    /// a full response or a delta response depending on what was received last.
    /// The delta should be relative to this previous response.
    /// </summary>
    [JsonPropertyName("previousResultId")]
    public string PreviousResultId { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<SemanticTokensDeltaPartialResult>? PartialResultToken { get; set; }
}
