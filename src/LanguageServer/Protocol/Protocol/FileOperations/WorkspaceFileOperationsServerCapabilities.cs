// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The server capabilities specific to workspace file operations.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#serverCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class WorkspaceFileOperationsServerCapabilities
{
    /// <summary>
    /// The server is interested in receiving didCreateFiles notifications.
    /// </summary>
    [JsonPropertyName("didCreate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationRegistrationOptions? DidCreate { get; init; }

    /// <summary>
    /// The server is interested in receiving willCreateFiles requests.
    /// </summary>
    [JsonPropertyName("willCreate")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationRegistrationOptions? WillCreate { get; init; }

    /// <summary>
    /// The server is interested in receiving didRenameFiles notifications.
    /// </summary>
    [JsonPropertyName("didRename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationRegistrationOptions? DidRename { get; init; }

    /// <summary>
    /// The server is interested in receiving willRenameFiles requests.
    /// </summary>
    [JsonPropertyName("willRename")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationRegistrationOptions? WillRename { get; init; }

    /// <summary>
    /// The server is interested in receiving didDeleteFiles file notifications.
    /// </summary>
    [JsonPropertyName("didDelete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationRegistrationOptions? DidDelete { get; init; }

    /// <summary>
    /// The server is interested in receiving willDeleteFiles file requests.
    /// </summary>
    [JsonPropertyName("willDelete")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationRegistrationOptions? WillDelete { get; init; }
}
