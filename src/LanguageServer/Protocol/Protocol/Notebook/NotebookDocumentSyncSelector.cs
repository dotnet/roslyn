// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Matches notebooks and cells to be synced
/// <para>
/// NOTE: either one or both of the Notebook and Cells properties must be non-null.
/// </para>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentSyncOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class NotebookDocumentSyncSelector
{
    /// <summary>
    /// The notebook to be synced. If a string
    /// value is provided it matches against the
    /// notebook type. '*' matches every notebook.
    /// </summary>
    [JsonPropertyName("notebook")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<string, NotebookDocumentFilter>? Notebook { get; init; }

    /// <summary>
    /// The cells of the matching notebook to be synced.
    /// </summary>
    [JsonPropertyName("cells")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotebookDocumentSyncCellSelector[]? Cells { get; init; }
}
