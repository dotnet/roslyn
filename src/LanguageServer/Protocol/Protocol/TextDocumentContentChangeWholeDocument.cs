// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents a whole-document text content change event (full document replacement).
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.18/specification/#textDocumentContentChangeWholeDocument">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.18</remarks>
internal sealed class TextDocumentContentChangeWholeDocument
{
    /// <summary>
    /// Gets or sets the new text of the whole document.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonRequired]
    public string Text
    {
        get;
        set;
    }
}
