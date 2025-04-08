// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents the parameter sent with workspace/didChangeConfiguration requests.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeConfigurationParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DidChangeConfigurationParams
{
    /// <summary>
    /// Gets or sets the settings that are applicable to the language server.
    /// </summary>
    [JsonPropertyName("settings")]
    public object Settings
    {
        get;
        set;
    }
}
