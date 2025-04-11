// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the parameter sent for the client/unregisterCapability request.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#unregistrationParams">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class UnregistrationParams
{
    /// <summary>
    /// Gets or sets the capabilities to unregister.
    /// </summary>
    // NOTE: the 'unregisterations' typo is noted in the spec but cannot be changed until LSP 4.x for compat reasons
    [JsonPropertyName("unregisterations")]
    [JsonRequired]
    public Unregistration[] Unregistrations
    {
        get;
        set;
    }
}
