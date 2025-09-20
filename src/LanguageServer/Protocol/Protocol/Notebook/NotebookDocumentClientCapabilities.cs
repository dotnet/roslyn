// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Capabilities specific to the notebook document support
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class NotebookDocumentClientCapabilities
{
    /// <summary>
    /// Capabilities specific to notebook document synchronization
    /// </summary>
    [JsonPropertyName("synchronization")]
    [JsonRequired]
    public NotebookDocumentSyncClientCapabilities Synchronization { get; init; }
}
