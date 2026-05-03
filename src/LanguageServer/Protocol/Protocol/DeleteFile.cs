// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Class representing a delete file operation.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#deleteFile">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.13</remarks>
[Kind("delete")]
internal sealed class DeleteFile : IAnnotatedChange
{
    /// <summary>
    /// Gets the kind value.
    /// </summary>
    [JsonPropertyName("kind")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Member can't be static since it's part of the protocol")]
    public string Kind => "delete";

    /// <summary>
    /// Gets or sets the file to delete.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonRequired]
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
    /// Gets or sets the additional options.
    /// </summary>
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DeleteFileOptions? Options
    {
        get;
        set;
    }

    /// <inheritdoc/>
    [JsonPropertyName("annotationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChangeAnnotationIdentifier? AnnotationId { get; init; }
}
