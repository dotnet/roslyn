// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The params sent in a <c>notebookDocument/didSave</c> notification.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didSaveNotebookDocumentParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class DidSaveNotebookDocumentParams
{
    /// <summary>
    /// The notebook document that got saved.
    /// </summary>
    [JsonPropertyName("notebookDocument")]
    [JsonRequired]
    public NotebookDocumentIdentifier NotebookDocument { get; init; }
}
