// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The client's capabilities for file requests/notifications.
/// </summary>
internal sealed class FileOperationsWorkspaceClientCapabilities
{
    /// <summary>
    /// The client has support for sending didCreateFiles notifications.
    /// </summary>
    [JsonPropertyName("didCreate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DidCreate { get; init; }

    /// <summary>
    /// The client has support for sending willCreateFiles requests.
    /// </summary>
    [JsonPropertyName("willCreate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WillCreate { get; init; }

    /// <summary>
    /// The client has support for sending didRenameFiles notifications.
    /// </summary>
    [JsonPropertyName("didRename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DidRename { get; init; }

    /// <summary>
    /// The client has support for sending willRenameFiles requests.
    /// </summary>
    [JsonPropertyName("willRename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WillRename { get; init; }

    /// <summary>
    /// The client has support for sending didDeleteFiles notifications.
    /// </summary>
    [JsonPropertyName("didDelete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DidDelete { get; init; }

    /// <summary>
    /// The client has support for sending willDeleteFiles requests.
    /// /// </summary>
    [JsonPropertyName("willDelete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WillDelete { get; init; }
}
