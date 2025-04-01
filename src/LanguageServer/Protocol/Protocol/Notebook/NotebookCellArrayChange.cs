// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A change describing how to move a <see cref="NotebookCell"/> array from state S to S'.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookCellArrayChange">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class NotebookCellArrayChange
{
    /// <summary>
    /// The start offset of the cell that changed.
    /// </summary>
    [JsonPropertyName("start")]
    [JsonRequired]
    public int Start { get; set; }

    /// <summary>
    /// The deleted cells
    /// </summary>
    [JsonPropertyName("deleteCount")]
    [JsonRequired]
    public int DeleteCount { get; init; }

    /// <summary>
    /// The new cells, if any
    /// </summary>
    [JsonPropertyName("cells")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NotebookCell[]? Cells { get; init; }
}
