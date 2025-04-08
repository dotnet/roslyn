// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Additional information that describes document changes
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#changeAnnotation">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>
/// Since LSP 3.16
/// </remarks>
internal sealed class ChangeAnnotation
{
    /// <summary>
    /// Human-readable string describing the change, rendered in the UI.
    /// </summary>
    [JsonPropertyName("label")]
    [JsonRequired]
    public string Label { get; init; }

    /// <summary>
    /// Indicates whether user confirmation is needed before applying the change.
    /// </summary>
    [JsonPropertyName("needsConfirmation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? NeedsConfirmation { get; init; }

    /// <summary>
    /// Human-readable string describing the change, rendered in the UI less prominently than the <see cref="Label"/>.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }
}
