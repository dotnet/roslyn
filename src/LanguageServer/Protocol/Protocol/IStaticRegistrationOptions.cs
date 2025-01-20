// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Interface representing the static registration options for options returned in the initialize request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#staticRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal interface IStaticRegistrationOptions
{
    /// <summary>
    /// Gets or sets the id used to register the request. The id can be used to deregister the request again.
    /// </summary>
    // NOTE: these JSON attributes are not inherited, they are here as a reference for implementations
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
}
