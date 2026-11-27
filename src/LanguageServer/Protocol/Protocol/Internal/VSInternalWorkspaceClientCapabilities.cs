// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Extension class for WorkspaceClientCapabilities with fields specific to Visual Studio.
/// </summary>
internal sealed class VSInternalWorkspaceClientCapabilities : WorkspaceClientCapabilities
{
    /// <summary>
    /// Gets or sets capabilities indicating what support the client has for workspace project contexts.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("_vs_projectContext")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticWorkspaceSetting? ProjectContext { get; set; }
}
