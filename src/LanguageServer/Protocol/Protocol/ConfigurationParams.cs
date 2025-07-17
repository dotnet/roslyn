// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameters for the workspace/configuration request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#configurationParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class ConfigurationParams
{
    /// <summary>
    /// Gets or sets the ConfigurationItems being requested.
    /// </summary>
    [JsonPropertyName("items")]
    [JsonRequired]
    public ConfigurationItem[] Items
    {
        get;
        set;
    }
}
