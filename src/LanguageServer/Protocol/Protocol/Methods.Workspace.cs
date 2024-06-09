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
}
