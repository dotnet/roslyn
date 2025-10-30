// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Class which identifies a text document.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentIdentifier">Language Server Protocol specification</see> for additional information.
/// </summary>
internal class TextDocumentIdentifier : IEquatable<TextDocumentIdentifier>
{
    /// <summary>
    /// Gets or sets the URI of the text document.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public DocumentUri DocumentUri { get; set; }

    [Obsolete("Use DocumentUri instead. This property will be removed in a future version.")]
    [JsonIgnore]
    public Uri Uri
    {
        get => DocumentUri.GetRequiredParsedUri();
        set => DocumentUri = new DocumentUri(value);
    }

    public static bool operator ==(TextDocumentIdentifier? value1, TextDocumentIdentifier? value2)
    {
        if (ReferenceEquals(value1, value2))
        {
            return true;
        }

        // Is null?
        if (ReferenceEquals(null, value2))
        {
            return false;
        }

        return value1?.Equals(value2) ?? false;
    }

    public static bool operator !=(TextDocumentIdentifier? value1, TextDocumentIdentifier? value2)
    {
        return !(value1 == value2);
    }

    /// <inheritdoc/>
    public bool Equals(TextDocumentIdentifier other)
    {
        return other is not null
            && this.DocumentUri == other.DocumentUri;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is TextDocumentIdentifier other)
        {
            return this.Equals(other);
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return this.DocumentUri == null ? 89 : this.DocumentUri.GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return this.DocumentUri == null ? string.Empty : this.DocumentUri.ToString();
    }
}
