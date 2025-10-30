// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Specifies languages of cells to be matched in a notebook sync.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentSyncOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// <remarks>Since LSP 3.17</remarks>
/// </summary>
internal sealed class NotebookDocumentSyncCellSelector
{
    [JsonPropertyName("language")]
    [JsonRequired]
    public string Language { get; init; }
}
