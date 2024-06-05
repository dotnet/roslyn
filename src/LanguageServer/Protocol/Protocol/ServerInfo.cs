// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Information about the server name and version
/// </summary>
/// <remarks>Since LSP 3.15</remarks>
internal class ServerInfo
{

    /// <summary>
    /// The server name
    /// </summary>
    [JsonPropertyName("name")]
    [JsonRequired]
    public string Name { get; init; }



    /// <summary>
    /// The server version
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; init; }
}
