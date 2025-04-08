// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents workspace symbols capabilities.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceSymbolOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class WorkspaceSymbolOptions : IWorkDoneProgressOptions
{
    /// <summary>
    /// The server provides support to resolve additional
    /// information for a workspace symbol.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("resolveProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ResolveProvider { get; init; }

    /// <inheritdoc/>
    [JsonPropertyName("workDoneProgress")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkDoneProgress { get; init; }
}
