// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The params sent in a <c>notebookDocument/didChange</c> notification.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeNotebookDocumentParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class DidChangeNotebookDocumentParams
{
    /// <summary>
    /// The notebook document that did change. The version number points
    /// to the version after all provided changes have been applied.
    /// </summary>
    [JsonPropertyName("notebookDocument")]
    [JsonRequired]
    public VersionedNotebookDocumentIdentifier NotebookDocument { get; init; }

    /// <summary>
    /// The actual changes to the notebook document.
    /// <para>
    /// The change describes single state change to the notebook document.
    /// So it moves a notebook document, its cells and its cell text document
    /// contents from state S to S'.
    /// </para>
    /// <para>
    /// To mirror the content of a notebook using change events use the
    /// following approach:
    /// <list type="bullet">
    /// <item>start with the same initial content</item>
    /// <item>apply the <c>notebookDocument/didChange</c> notifications in the order you receive them.</item>
    /// </list>
    /// </para>
    /// </summary>
    [JsonPropertyName("change")]
    [JsonRequired]
    public NotebookDocumentChangeEvent Change { get; init; }
}
