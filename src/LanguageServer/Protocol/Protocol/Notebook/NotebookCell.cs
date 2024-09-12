// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A notebook cell.
/// <para>
/// A cell's document URI must be unique across ALL notebook
/// cells and can therefore be used to uniquely identify a
/// notebook cell or the cell's text document.
/// </para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookCell">Language Server Protocol specification</see> for additional information.
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class NotebookCell
{

    /// <summary>
    /// The cell's kind
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonRequired]
    public NotebookCellKind Kind { get; init; }

    /// <summary>
    /// The URI of the cell's text document content.
    /// </summary>
    [JsonPropertyName("document")]
    [JsonConverter(typeof(DocumentUriConverter))]
    [JsonRequired]
    public Uri Document { get; init; }

    /// <summary>
    /// Additional metadata stored with the cell.
    /// </summary>
    [JsonPropertyName("metadata")]
    public object? Metadata { get; init; }

    /// <summary>
    /// Additional execution summary information if supported by the client.
    /// </summary>
    [JsonPropertyName("executionSummary")]
    public ExecutionSummary? ExecutionSummary { get; init; }
}
