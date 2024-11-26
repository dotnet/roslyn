// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// diagnostics methods from https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#languageFeatures
partial class Methods
{
    // NOTE: these are sorted in the order used by the spec

    /// <summary>
    /// Method name for 'textDocument/publishDiagnostics'.
    /// <para>
    /// Diagnostics notifications are sent from the server to the client to signal results of validation runs.
    /// </para>
    /// <para>
    /// Diagnostics are “owned” by the server so it is the server’s responsibility to clear them if necessary.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_publishDiagnostics">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentPublishDiagnosticsName = "textDocument/publishDiagnostics";

    /// <summary>
    /// Strongly typed message object for 'textDocument/publishDiagnostics'.
    /// </summary>
    public static readonly LspNotification<PublishDiagnosticParams> TextDocumentPublishDiagnostics = new(TextDocumentPublishDiagnosticsName);

    /// <summary>
    /// Method name for 'textDocument/diagnostic'.
    /// <para>
    /// Pull diagnostics are a preferred alternative to 'textDocument/publishDiagnostics' push diagnostics that give
    /// the client more control over the documents for which diagnostics should be computed and at which point in time.
    /// </para>
    /// <para>
    /// The text document diagnostic request is sent from the client to the server to ask the server to compute the
    /// diagnostics for a given document. As with other pull requests the server is asked to compute the diagnostics
    /// for the currently synced version of the document.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_diagnostic">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string TextDocumentDiagnosticName = "textDocument/diagnostic";

    /// <summary>
    /// Strongly typed message object for 'textDocument/diagnostic'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspNotification<DocumentDiagnosticParams> TextDocumentDiagnostic = new(TextDocumentDiagnosticName);

    /// <summary>
    /// Method name for 'workspace/diagnostic'.
    /// <para>
    /// The workspace diagnostic request is sent from the client to the server to ask the server to compute workspace
    /// wide diagnostics which previously were pushed from the server to the client.
    /// </para>
    /// <para>
    /// In contrast to the document diagnostic request the workspace request can be long running and is not bound
    /// to a specific workspace or document state.
    /// </para>
    /// <para>
    /// If the client supports streaming for the workspace diagnostic pull it is legal to provide a document diagnostic
    /// report multiple times for the same document URI. The last one reported will win over previous reports.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_diagnostic">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string WorkspaceDiagnosticName = "workspace/diagnostic";

    /// <summary>
    /// Strongly typed message object for 'workspace/diagnostic'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<WorkspaceDiagnosticParams, WorkspaceDiagnosticReport> WorkspaceDiagnostic = new(WorkspaceDiagnosticName);

    /// <summary>
    /// Method name for 'workspace/diagnostic/refresh'.
    /// <para>
    /// The workspace/diagnostic/refresh request is sent from the server to the client. Servers can use it to ask clients
    /// to refresh all needed document and workspace diagnostics.
    /// </para>
    /// <para>
    /// This is useful if a server detects a project wide configuration change which requires a re-calculation of all diagnostics.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#diagnostic_refresh">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string WorkspaceDiagnosticRefreshName = "workspace/diagnostic/refresh";

    /// <summary>
    /// Strongly typed message object for 'workspace/diagnostic/refresh'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspNotification<object?> WorkspaceDiagnosticRefresh = new(WorkspaceDiagnosticRefreshName);
}
