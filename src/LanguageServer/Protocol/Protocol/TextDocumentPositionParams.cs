// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents a position within a text document.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentPositionParams">Language Server Protocol specification</see> for additional information.
/// </summary>
internal class TextDocumentPositionParams : ITextDocumentPositionParams
{
    /// <summary>
    /// Gets or sets the value which identifies the document.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value which indicates the position within the document.
    /// </summary>
    [JsonPropertyName("position")]
    public Position Position
    {
        get;
        set;
    }
}
