// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The workspace folder change event provided by the <see cref="DidChangeWorkspaceFoldersParams"/>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceFoldersChangeEvent">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.6</remarks>
internal sealed class WorkspaceFoldersChangeEvent
{
    /// <summary>
    /// The array of added workspace folders
    /// </summary>
    [JsonPropertyName("added")]
    [JsonRequired]
    public WorkspaceFolder[] Added { get; init; }

    /// <summary>
    /// The array of the removed workspace folders
    /// </summary>
    [JsonPropertyName("removed")]
    [JsonRequired]
    public WorkspaceFolder[] Removed { get; init; }
}
