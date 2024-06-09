// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceFeatures
partial class Methods
{
    // NOTE: these are sorted/grouped in the order used by the spec

    /// <summary>
    /// Method name for 'workspace/symbol'.
    /// <para>
    /// The workspace symbol request is sent from the client to the server to list project-wide symbols matching the query string.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_symbol">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string WorkspaceSymbolName = "workspace/symbol";

    /// <summary>
    /// Strongly typed message object for 'workspace/symbol'.
    /// </summary>
    public static readonly LspRequest<WorkspaceSymbolParams, SumType<SymbolInformation[], WorkspaceSymbol[]>?> WorkspaceSymbol = new(WorkspaceSymbolName);

    /// <summary>
    /// Method name for 'workspaceSymbol/resolve'.
    /// <para>
    /// The request is sent from the client to the server to resolve additional information for a given workspace symbol.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_symbolResolve">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string WorkspaceSymbolResolveName = "workspaceSymbol/resolve";

    /// <summary>
    /// Strongly typed message object for 'workspaceSymbol/resolve'.
    /// </summary>
    public static readonly LspRequest<WorkspaceSymbol, WorkspaceSymbol> WorkspaceSymbolResolve = new(WorkspaceSymbolResolveName);

    /// <summary>
    /// Method name for 'workspace/configuration'.
    /// <para>
    /// The workspace/configuration request is sent from the server to the client to fetch configuration
    /// settings from the client. The request can fetch several configuration settings in one roundtrip.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_configuration">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public const string WorkspaceConfigurationName = "workspace/configuration";

    /// <summary>
    /// Strongly typed message object for 'workspace/configuration'.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public static readonly LspRequest<ConfigurationParams, object?[]> WorkspaceConfiguration = new(WorkspaceConfigurationName);

    /// <summary>
    /// Method name for 'workspace/didChangeConfiguration'.
    /// <para>
    /// A notification sent from the client to the server to signal the change of configuration settings.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_didChangeConfiguration">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string WorkspaceDidChangeConfigurationName = "workspace/didChangeConfiguration";

    /// <summary>
    /// Strongly typed message object for 'workspace/didChangeConfiguration'.
    /// </summary>
    public static readonly LspNotification<DidChangeConfigurationParams> WorkspaceDidChangeConfiguration = new(WorkspaceDidChangeConfigurationName);

    /// <summary>
    /// Method name for 'workspace/workspaceFolders'.
    /// <para>
    /// The workspace/workspaceFolders request is sent from the server to the client to fetch the current open
    /// list of workspace folders. Returns <see langword="null"/> in the response if only a single file is open in the tool.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_workspaceFolders">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public const string WorkspaceFoldersName = "workspace/workspaceFolders";

    /// <summary>
    /// Strongly typed message object for 'workspace/workspaceFolders'.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public static readonly LspRequest<object?, WorkspaceFolder?[]> WorkspaceFolders = new(WorkspaceFoldersName);

    /// <summary>
    /// Method name for 'workspace/didChangeWorkspaceFolders'.
    /// <para>
    /// The <c>workspace/didChangeWorkspaceFolders</c> notification is sent from the client to the server
    /// to inform the server about workspace folder configuration changes.
    /// </para>
    /// <para>
    /// A server can register for this notification by using either the server capability
    /// <see cref="WorkspaceFoldersServerCapabilities.ChangeNotifications"/> or by using the dynamic capability registration mechanism.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_didChangeWorkspaceFolders">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public const string WorkspaceDidChangeWorkspaceFoldersName = "workspace/didChangeWorkspaceFolders";

    /// <summary>
    /// Strongly typed message object for 'workspace/didChangeWorkspaceFolders'.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public static readonly LspNotification<DidChangeWorkspaceFoldersParams> WorkspaceDidChangeWorkspaceFolders = new(WorkspaceDidChangeWorkspaceFoldersName);

    /// <summary>
    /// Method name for 'workspace/willCreateFiles'.
    /// <para>
    /// The will create files request is sent from the client to the server before files are actually created as long
    /// as the creation is triggered from within the client either by a user action or by applying a workspace edit.
    /// </para>
    /// <para>
    /// The request can return a <see cref="WorkspaceEdit"/> which will be applied to the workspace before the files are created
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_willCreateFiles">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceWillCreateFilesName = "workspace/willCreateFiles";

    /// <summary>
    /// Strongly typed message object for 'workspace/willCreateFiles'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<CreateFilesParams?, WorkspaceEdit?> WorkspaceWillCreateFiles = new(WorkspaceWillCreateFilesName);

    /// <summary>
    /// Method name for 'workspace/didCreateFiles'.
    /// <para>
    /// The did create files notification is sent from the client to the server when files were created from within the client.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_didCreateFiles">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceDidCreateFilesName = "workspace/didCreateFiles";

    /// <summary>
    /// Strongly typed message object for 'workspace/didCreateFiles'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspNotification<CreateFilesParams> WorkspaceDidCreateFiles = new(WorkspaceDidCreateFilesName);

    /// <summary>
    /// Method name for 'workspace/willRenameFiles'.
    /// <para>
    /// The will rename files request is sent from the client to the server before files are actually renamed as long as the
    /// rename is triggered from within the client either by a user action or by applying a workspace edit.
    /// </para>
    /// <para>
    /// The request can return a <see cref="WorkspaceEdit"/> which will be applied to the workspace before the files are renamed.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_willRenameFiles">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceWillRenameFilesName = "workspace/willRenameFiles";

    /// <summary>
    /// Strongly typed message object for 'workspace/willRenameFiles'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<RenameFilesParams?, WorkspaceEdit?> WorkspaceWillRenameFiles = new(WorkspaceWillRenameFilesName);

    /// <summary>
    /// Method name for 'workspace/didRenameFiles'.
    /// <para>
    /// The did rename files notification is sent from the client to the server when files were renamed from within the client.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_didRenameFiles">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceDidRenameFilesName = "workspace/didRenameFiles";

    /// <summary>
    /// Strongly typed message object for 'workspace/didRenameFiles'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspNotification<RenameFilesParams> WorkspaceDidRenameFiles = new(WorkspaceDidRenameFilesName);

    /// <summary>
    /// Method name for 'workspace/willDeleteFiles'.
    /// <para>
    /// The will delete files request is sent from the client to the server before files are actually deleted as
    /// long as the deletion is triggered from within the client either by a user action or by applying a workspace edit.
    /// </para>
    /// <para>
    /// The request can return a <see cref="WorkspaceEdit"/> which will be applied to workspace before the files are deleted.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_willDeleteFiles">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceWillDeleteFilesName = "workspace/willDeleteFiles";

    /// <summary>
    /// Strongly typed message object for 'workspace/willDeleteFiles'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<DeleteFilesParams?, WorkspaceEdit?> WorkspaceWillDeleteFiles = new(WorkspaceWillDeleteFilesName);

    /// <summary>
    /// Method name for 'workspace/didDeleteFiles'.
    /// <para>
    /// The did delete files notification is sent from the client to the server when files were deleted from within the client.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_didDeleteFiles">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceDidDeleteFilesName = "workspace/didDeleteFiles";

    /// <summary>
    /// Strongly typed message object for 'workspace/didDeleteFiles'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspNotification<DeleteFilesParams> WorkspaceDidDeleteFiles = new(WorkspaceDidDeleteFilesName);

    /// <summary>
    /// Method name for 'workspace/didChangeWatchedFiles'.
    /// <para>
    /// The watched files notification is sent from the client to the server when the client detects changes to files
    /// and folders watched by the language client.
    /// </para>
    /// <para>
    /// Note that although the name suggest that only file events are sent, it is about file system events, which includes folders as well.
    /// It is recommended that servers register for these file system events using the registration mechanism.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_didChangeWatchedFiles">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string WorkspaceDidChangeWatchedFilesName = "workspace/didChangeWatchedFiles";

    /// <summary>
    /// Strongly typed message object for 'workspace/didChangeWatchedFiles'.
    /// </summary>
    public static readonly LspNotification<DidChangeWatchedFilesParams> WorkspaceDidChangeWatchedFiles = new(WorkspaceDidChangeWatchedFilesName);
}
