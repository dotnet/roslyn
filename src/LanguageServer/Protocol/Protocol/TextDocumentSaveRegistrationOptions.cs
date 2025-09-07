// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Options when registering for document-specific 'textDocument/didSave' notifications
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentSaveRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class TextDocumentSaveRegistrationOptions : TextDocumentRegistrationOptions
{
    /// <summary>
    /// The client is supposed to include the content on save.
    /// </summary>
    [JsonPropertyName("IncludeText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IncludeText { get; init; }
}
