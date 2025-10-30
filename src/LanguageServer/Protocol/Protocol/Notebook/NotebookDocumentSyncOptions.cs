// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Options specific to a notebook plus its cells
/// to be synced to the server.
/// <para>
/// If a selector provides a notebook document
/// filter but no cell selector all cells of a
/// matching notebook document will be synced.
/// </para>
/// <para>
/// If a selector provides no notebook document
/// filter but only a cell selector all notebook
/// documents that contain at least one matching
/// cell will be synced.
/// </para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentSyncOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class NotebookDocumentSyncOptions
{
    /// <summary>
    /// The notebooks to be synced
    /// </summary>
    [JsonPropertyName("notebookSelector")]
    [JsonRequired]
    public NotebookDocumentSyncSelector[] NotebookSelector { get; init; }

    /// <summary>
    /// Whether save notification should be forwarded to
    /// the server. Will only be honored if mode === `notebook`.
    /// </summary>
    [JsonPropertyName("save")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Save { get; init; }
}
