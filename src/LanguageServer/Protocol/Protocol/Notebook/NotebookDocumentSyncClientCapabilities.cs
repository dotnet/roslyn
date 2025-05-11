// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Notebook specific client capabilities
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentSyncClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class NotebookDocumentSyncClientCapabilities : IDynamicRegistrationSetting
{
    /// <summary>
    /// The client supports sending execution summary data per cell.
    /// </summary>
    [JsonPropertyName("executionSummarySupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ExecutionSummarySupport { get; init; }

    /// <summary>
    /// Whether the implementation supports dynamic registration. If this is set to <see langword="true"/>
    /// the client supports the new <see cref="NotebookDocumentSyncRegistrationOptions"/> and <see cref="NotebookDocumentSyncOptions"/>
    /// return values for the corresponding server capabilities.
    /// </summary>
    [JsonPropertyName("dynamicRegistration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DynamicRegistration { get; set; }
}
