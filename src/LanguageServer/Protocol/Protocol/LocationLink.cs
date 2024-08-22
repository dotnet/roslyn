// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

using Roslyn.Utilities;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Represents a link between a source and a target location.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#locationLink">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.14</remarks>
internal class LocationLink : IEquatable<LocationLink>
{
    /// <summary>
    /// Span of the origin of this link.
    /// <para>
    /// Used as the underlined span for mouse interaction. Defaults to the word
    /// range at the mouse position.
    /// </para>
    /// </summary>
    [JsonPropertyName("originSelectionRange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Range? OriginSelectionRange { get; init; }

    /// <summary>
    /// The URI for the target document.
    /// </summary>
    [JsonPropertyName("targetUri")]
    [JsonRequired]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri TargetUri { get; init; }

    /// <summary>
    /// The full target range of the linked location in the target document, which includes
    /// the <see cref="TargetSelectionRange"/> and additional context such as comments (but
    /// not leading/trailing whitespace). This information is typically used to highlight
    /// the range in the editor.
    /// </summary>
    [JsonPropertyName("targetRange")]
    [JsonRequired]
    public Range TargetRange { get; init; }

    /// <summary>
    /// Gets or sets the range to be selected and revealed in the target document e.g. the
    /// name of the linked symbol. Must be contained by the <see cref="TargetRange"/>.
    /// </summary>
    [JsonPropertyName("targetSelectionRange")]
    [JsonRequired]
    public Range TargetSelectionRange { get; init; }

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as LocationLink);

    /// <inheritdoc/>
    public bool Equals(LocationLink? other) =>
        other != null
            && EqualityComparer<Range>.Default.Equals(this.OriginSelectionRange, other.OriginSelectionRange)
            && this.TargetUri != null && other.TargetUri != null && this.TargetUri.Equals(other.TargetUri)
            && EqualityComparer<Range>.Default.Equals(this.TargetRange, other.TargetRange)
            && EqualityComparer<Range>.Default.Equals(this.TargetSelectionRange, other.TargetSelectionRange);

    /// <inheritdoc/>
    public override int GetHashCode() =>
#if NETCOREAPP
        HashCode.Combine(OriginSelectionRange, TargetUri, TargetRange, TargetSelectionRange);
#else
        Hash.Combine(OriginSelectionRange,
        Hash.Combine(TargetUri,
        Hash.Combine(TargetRange, TargetSelectionRange.GetHashCode())));
#endif
}
