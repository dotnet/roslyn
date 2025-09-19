// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Changes to the text content of a notebook cell in a <see cref="NotebookDocumentChangeCells"/>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentChangeEvent">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class NotebookDocumentChangeCellsText
{
    /// <summary>
    /// Identifier for the document representing the cell
    /// </summary>
    [JsonPropertyName("document")]
    [JsonRequired]
    public VersionedTextDocumentIdentifier Document { get; init; }

    /// <summary>
    /// The changes to the document representing the cell
    /// </summary>
    [JsonPropertyName("changes")]
    [JsonRequired]
    public TextDocumentContentChangeEvent[] Changes { get; init; }
}
