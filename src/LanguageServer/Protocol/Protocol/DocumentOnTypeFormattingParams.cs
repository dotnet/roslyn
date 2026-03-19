// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters sent for a textDocument/onTypeFormatting request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentOnTypeFormattingParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Do not seal this type! This is extended by Razor</remarks>
internal class DocumentOnTypeFormattingParams : ITextDocumentPositionParams
{
    /// <summary>
    /// The document to format.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; }

    /// <summary>
    /// The position around which the on type formatting should happen.
    /// <para>
    /// This is not necessarily the exact position where the character denoted
    /// by the <see cref="Character"/> property got typed.
    /// </para>
    /// </summary>
    [JsonPropertyName("position")]
    public Position Position { get; set; }

    /// <summary>
    /// The character that has been typed that triggered the formatting
    /// on type request.
    /// <para>
    /// That is not necessarily the last character that
    /// got inserted into the document since the client could auto insert
    /// characters as well (e.g. like automatic brace completion).
    /// </para>
    /// </summary>
    [JsonPropertyName("ch")]
    [JsonRequired]
    public string Character { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="FormattingOptions"/> for the request.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonRequired]
    public FormattingOptions Options { get; set; }
}
