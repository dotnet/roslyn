// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Server capabilities for the 'workspace/textDocumentContent' request
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#workspace_textDocumentContent">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal class TextDocumentContentOptions
{
    /// <summary>
    /// Gets or sets the schemes that the server supports for text document content requests.
    /// </summary>
    [JsonPropertyName("schemes")]
    public string[] Schemes
    {
        get;
        init;
    }
}
