// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Workspace specific server capabilities.
/// </summary>
internal sealed class WorkspaceServerCapabilities
{
    /// <summary>
    /// The server supports workspace folder.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    [JsonPropertyName("workspaceFolders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WorkspaceFoldersServerCapabilities? WorkspaceFolders { get; init; }

    /// <summary>
    /// The server is interested in file notifications/requests.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("fileOperations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WorkspaceFileOperationsServerCapabilities? FileOperations { get; init; }
}
