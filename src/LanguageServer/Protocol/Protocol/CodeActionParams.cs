// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters sent from the client to the server for the textDocument/codeAction request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class CodeActionParams : ITextDocumentParams, IWorkDoneProgressParams, IPartialResultParams<SumType<Command, CodeAction>[]>
{
    /// <summary>
    /// Gets or sets the document identifier indicating where the command was invoked.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the range in the document for which the command was invoked.
    /// </summary>
    [JsonPropertyName("range")]
    [JsonRequired]
    public Range Range
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the additional diagnostic information about the code action context.
    /// </summary>
    [JsonPropertyName("context")]
    [JsonRequired]
    public CodeActionContext Context
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
    public IProgress<SumType<Command, CodeAction>[]>? PartialResultToken { get; set; }
}
