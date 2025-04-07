// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class which represents the parameter sent with workspace/didChangeWorkspaceFolders requests.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#didChangeWorkspaceFoldersParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.6</remarks>
internal class DidChangeWorkspaceFoldersParams
{
    /// <summary>
    /// The actual workspace folder change event.
    /// </summary>
    [JsonPropertyName("event")]
    [JsonRequired]
    public WorkspaceFoldersChangeEvent Event { get; init; }
}
