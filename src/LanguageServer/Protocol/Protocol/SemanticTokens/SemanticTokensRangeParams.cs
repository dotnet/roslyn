// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Parameters for 'textDocument/semanticTokens/range' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensRangeParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
/// <remarks>Do not seal this type! This is extended by Razor</remarks>
internal class SemanticTokensRangeParams : ITextDocumentParams, IWorkDoneProgressParams, IPartialResultParams<SemanticTokensPartialResult>
{
    /// <summary>
    /// Gets or sets an identifier for the document to fetch semantic tokens from.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// Gets or sets the range within the document to fetch semantic tokens for.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<SemanticTokensPartialResult>? PartialResultToken { get; set; }
}
