// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Class which represents a text document.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentItem">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class TextDocumentItem
{
    /// <summary>
    /// Gets or sets the document URI.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public DocumentUri DocumentUri
    {
        get;
        set;
    }

    [Obsolete("Use DocumentUri instead. This property will be removed in a future version.")]
    [JsonIgnore]
    public Uri Uri
    {
        get => DocumentUri.GetRequiredParsedUri();
        set => DocumentUri = new DocumentUri(value);
    }

    /// <summary>
    /// Gets or sets the document language identifier.
    /// </summary>
    [JsonPropertyName("languageId")]
    public string LanguageId
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the document version.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the content of the opened text document.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text
    {
        get;
        set;
    }
}
