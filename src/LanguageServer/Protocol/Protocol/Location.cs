// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Class representing a location in a document.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#location">Language Server Protocol specification</see> for additional information.
/// </summary>
internal class Location : IEquatable<Location>
{
    /// <summary>
    /// Gets or sets the URI for the document the location belongs to.
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
    /// Gets or sets the range of the location in the document.
    /// </summary>
    [JsonPropertyName("range")]
    public Range Range
    {
        get;
        set;
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as Location);
    }

    /// <inheritdoc/>
    public bool Equals(Location? other)

    {
        return other != null && this.DocumentUri != null && other.DocumentUri != null &&
               this.DocumentUri.Equals(other.DocumentUri) &&
               EqualityComparer<Range>.Default.Equals(this.Range, other.Range);
    }
    public override int GetHashCode()
    {
        var hashCode = 1486144663;
        hashCode = (hashCode * -1521134295) + this.DocumentUri.GetHashCode();
        hashCode = (hashCode * -1521134295) + EqualityComparer<Range>.Default.GetHashCode(this.Range);
        return hashCode;
    }
}
