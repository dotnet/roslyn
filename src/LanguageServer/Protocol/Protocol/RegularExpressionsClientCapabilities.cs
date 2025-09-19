// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the client's regular expression engine
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#regExp">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class RegularExpressionsClientCapabilities
{
    /// <summary>
    /// The name of the regular expression engine.
    /// </summary>
    [JsonPropertyName("engine")]
    [JsonRequired]
    public string Engine { get; init; }

    /// <summary>
    /// The version of the regular expression engine.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; init; }

}
