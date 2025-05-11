// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents the parameter that is sent with a textDocument/didSave message.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didSaveTextDocumentParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DidSaveTextDocumentParams : ITextDocumentParams
{
    /// <summary>
    /// Gets or sets the <see cref="TextDocumentIdentifier"/> which represents the text document that was saved.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// Optional the content when saved. Depends on the <see cref="SaveOptions.IncludeText"/> value
    /// or the <see cref="TextDocumentSaveRegistrationOptions.IncludeText"/> when the save
    /// notification was requested.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text
    {
        get;
        set;
    }
}
