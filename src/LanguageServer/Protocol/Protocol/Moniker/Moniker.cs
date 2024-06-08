// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Moniker definition to match LSIF 0.5 moniker definition.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#moniker">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class Moniker
{
    /// <summary>
    /// The scheme of the moniker. For example tsc or .Net
    /// </summary>
    [JsonPropertyName("scheme")]
    [JsonRequired]
    public string Scheme { get; init; }

    /// <summary>
    /// The identifier of the moniker. The value is opaque in LSIF however
    /// schema owners are allowed to define the structure if they want.
    /// </summary>
    [JsonPropertyName("identifier")]
    [JsonRequired]
    public string Identifier { get; init; }

    /// <summary>
    /// The scope in which the moniker is unique
    /// </summary>
    [JsonPropertyName("unique")]
    [JsonRequired]
    public UniquenessLevel Unique { get; init; }

    /// <summary>
    /// The moniker kind if known.
    /// </summary>
    [JsonPropertyName("kind")]
    [JsonIgnore(Condition =JsonIgnoreCondition.WhenWritingNull)]
    public MonikerKind? Kind { get; init; }
}
