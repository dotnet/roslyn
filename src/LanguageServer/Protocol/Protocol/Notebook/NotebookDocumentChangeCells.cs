// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Represents changes to the notebook cells in a <see cref="NotebookDocumentChangeEvent"/>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentChangeEvent">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class NotebookDocumentChangeCells
{
    /// <summary>
    /// Changes to the cell structure to add or remove cells.
    /// </summary>
    [JsonPropertyName("structure")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotebookDocumentChangeCellsStructure? Structure { get; init; }

    /// <summary>
    /// Changes to notebook cells properties like its
    /// kind, execution summary or metadata.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotebookCell[]? Data { get; init; }

    /// <summary>
    /// Changes to the text content of notebook cells.
    /// </summary>
    [JsonPropertyName("textContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotebookDocumentChangeCellsText[]? TextContent { get; init; }
}
