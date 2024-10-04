// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters sent from the client to the server for a textDocument/inlineValue request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlineValueParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class InlineValueParams : ITextDocumentParams, IWorkDoneProgressParams
{
    /// <summary>
    /// The text document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// The document range for which inline values should be computed.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range { get; set; }

    /// <summary>
    /// Additional information about the context in which inline values were
    /// requested.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonRequired]
    public InlineValueContext Context { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
}
