// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the information needed for unregistering a capability.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#unregistration">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class Unregistration
{
    /// <summary>
    /// Gets or sets the id of the unregistration.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonRequired]
    public string Id
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the method to unregister.
    /// </summary>
    [JsonPropertyName("method")]
    [JsonRequired]
    public string Method
    {
        get;
        set;
    }
}
