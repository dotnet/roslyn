// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents the parameter that is sent with textDocument/didChange message.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeTextDocumentParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DidChangeTextDocumentParams : ITextDocumentParams
{
    /// <summary>
    /// The document that did change. The version number points
    /// to the version after all provided content changes have
    /// been applied.
    /// </summary>
    [JsonPropertyName("textDocument")]
    [JsonRequired]
    public VersionedTextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// The actual content changes. The content changes describe single state
    /// changes to the document. So if there are two content changes c1 (at
    /// array index 0) and c2(at array index 1) for a document in state S then
    /// c1 moves the document from S to S' and c2 from S' to S''. So c1 is
    /// computed on the state S and c2 is computed on the state S'.
    /// <para>
    /// To mirror the content of a document using change events use the following
    /// approach:
    /// <list type="bullet">
    /// <item><description>Start with the same initial content</description></item>
    /// <item><description>Apply the 'textDocument/didChange' notifications in the order you receive them.</description></item>
    /// <item><description>Apply the `TextDocumentContentChangeEvent`s in a single notification in the order you receive them.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    [JsonPropertyName("contentChanges")]
    [JsonRequired]
    public TextDocumentContentChangeEvent[] ContentChanges
    {
        get;
        set;
    }

    TextDocumentIdentifier ITextDocumentParams.TextDocument
    {
        get => this.TextDocument;
    }
}
