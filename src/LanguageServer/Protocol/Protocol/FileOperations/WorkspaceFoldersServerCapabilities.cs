// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The server capabilities specific to workspace folders
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceFoldersServerCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.6</remarks>
internal class WorkspaceFoldersServerCapabilities
{
    /// <summary>
    /// The server has support for workspace folders
    /// </summary>
    [JsonPropertyName("supported")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? Supported { get; init; }

    /// <summary>
    /// Whether the server wants to receive workspace folder
    /// change notifications.
    /// <para>
    /// If a string is provided, the string is treated as an ID
    /// under which the notification is registered on the client
    /// side. The ID can be used to unregister for these events
    /// using the <see cref="Methods.ClientUnregisterCapabilityName"/> request.
    /// </para>
    /// </summary>
    [JsonPropertyName("changeNotifications")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<string, bool>? ChangeNotifications { get; init; }
}
