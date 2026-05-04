// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A notebook document.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocument">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class NotebookDocument
{
    /// <summary>
    /// The notebook document's URI.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonRequired]
    // NOTE: not a DocumentURI
    public Uri Uri { get; init; }

    /// <summary>
    /// The type of the notebook.
    /// </summary>
    [JsonPropertyName("notebookType")]
    [JsonRequired]
    public string NotebookType { get; init; }

    /// <summary>
    /// The version number of this document (it will increase after each change, including undo/redo).
    /// </summary>
    [JsonPropertyName("version")]
    [JsonRequired]
    public int Version { get; init; }

    /// <summary>
    /// Additional metadata stored with the notebook document.
    /// </summary>
    [JsonPropertyName("metadata")]
    public object? Metadata { get; init; }

    /// <summary>
    /// The cells of a notebook.
    /// </summary>
    [JsonPropertyName("cells")]
    [JsonRequired]
    public NotebookCell[] Cells { get; init; }
}
