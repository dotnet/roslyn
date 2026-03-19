// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Represents changes to the notebook structure to add or remove cells in a <see cref="NotebookDocumentChangeCells"/>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentChangeEvent">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class NotebookDocumentChangeCellsStructure
{
    /// <summary>
    /// The change to the cell array.
    /// </summary>
    [JsonPropertyName("array")]
    [JsonRequired]
    public NotebookCellArrayChange Array { get; init; }

    /// <summary>
    /// Additional opened cell text documents.
    /// </summary>
    [JsonPropertyName("didOpen")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextDocumentItem[]? DidOpen { get; init; }

    /// <summary>
    /// Additional closed cell text documents.
    /// </summary>
    [JsonPropertyName("didClose")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextDocumentIdentifier[]? DidClose { get; init; }
}
