// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing the workspace project context client capabilities.
/// </summary>
internal sealed class VSInternalProjectContextWorkspaceSetting
{
    /// <summary>
    /// Whether the client supports a refresh request sent from the server to the client.
    /// <para>
    /// Note that this event is global and will force the client to refresh all
    /// project contexts currently shown. It should be used with absolute care
    /// and is useful for situation where a server for example detects a project
    /// wide change that requires such a calculation.
    /// </para>
    /// </summary>
    [JsonPropertyName("refreshSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RefreshSupport { get; set; }
}
