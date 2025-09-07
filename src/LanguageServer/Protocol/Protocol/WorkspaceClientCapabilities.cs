// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents workspace capabilities.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#clientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class WorkspaceClientCapabilities
{
    // NOTE: these are kept in the same order as the spec to make them easier to update

    /// <summary>
    /// Whether the client supports applying batch edits to the workspace by supporting the request 'workspace/applyEdit'
    /// </summary>
    [JsonPropertyName("applyEdit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ApplyEdit { get; set; }

    /// <summary>
    /// Capabilities specific to <see cref="Protocol.WorkspaceEdit"/>
    /// </summary>
    [JsonPropertyName("workspaceEdit")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WorkspaceEditSetting? WorkspaceEdit { get; set; }

    /// <summary>
    /// Capabilities specific to the `workspace/didChangeConfiguration` notification.
    /// </summary>
    [JsonPropertyName("didChangeConfiguration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DidChangeConfigurationClientCapabilities? DidChangeConfiguration { get; set; }

    /// <summary>
    /// Capabilities specific to the `workspace/didChangeWatchedFiles` notification.
    /// </summary>
    [JsonPropertyName("didChangeWatchedFiles")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DidChangeWatchedFilesClientCapabilities? DidChangeWatchedFiles { get; set; }

    /// <summary>
    /// Capabilities specific to the `workspace/symbol` request.
    /// </summary>
    [JsonPropertyName("symbol")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SymbolSetting? Symbol { get; set; }

    /// <summary>
    /// Capabilities specific to the `workspace/executeCommand` request.
    /// </summary>
    [JsonPropertyName("executeCommand")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExecuteCommandClientCapabilities? ExecuteCommand { get; set; }

    /// <summary>
    /// The client has support for workspace folders.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    [JsonPropertyName("workspaceFolders")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WorkspaceFolders { get; init; }

    /// <summary>
    /// The client supports `workspace/configuration` requests.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    [JsonPropertyName("configuration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Configuration { get; set; }

    /// <summary>
    /// Capabilities specific to the semantic token requests scoped to the workspace.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("semanticTokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SemanticTokensWorkspaceSetting? SemanticTokens { get; set; }

    /// <summary>
    /// Capabilities specific to the code lens requests scoped to the workspace.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("codeLens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeLensWorkspaceSetting? CodeLens { get; set; }

    /// <summary>
    /// The client's capabilities for file requests/notifications.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    [JsonPropertyName("fileOperations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileOperationsWorkspaceClientCapabilities? FileOperations { get; init; }

    /// <summary>
    /// Client workspace capabilities specific to inline values.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("inlineValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InlineValueWorkspaceClientCapabilities? InlineValue { get; init; }

    /// <summary>
    /// Gets of sets capabilities specific to the inlay hint requests scoped to the workspace.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("inlayHint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public InlayHintWorkspaceSetting? InlayHint { get; set; }

    /// <summary>
    /// Gets or sets capabilities indicating what support the client has for workspace pull diagnostics.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    [JsonPropertyName("diagnostics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticWorkspaceSetting? Diagnostics { get; set; }
}
