// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters sent for a textDocument/documentLink request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentLinkParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DocumentLinkParams : ITextDocumentParams, IWorkDoneProgressParams, IPartialResultParams<DocumentLink[]>
{
    /// <summary>
    /// The <see cref="TextDocumentIdentifier"/> to provide links for.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.WorkDoneTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }

    /// <inheritdoc/>
    [JsonPropertyName(Methods.PartialResultTokenName)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IProgress<DocumentLink[]>? PartialResultToken { get; set; }
}
