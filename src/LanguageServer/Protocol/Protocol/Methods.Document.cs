// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

// non-navigation methods from https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#languageFeatures
partial class Methods
{
    // NOTE: these are sorted in the order used by the spec

    /// <summary>
    /// Method name for 'textDocument/hover'.
    /// <para>
    /// The hover request is sent from the client to the server to request hover information at a given text document position.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_hover">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentHoverName = "textDocument/hover";

    /// <summary>
    /// Strongly typed message object for 'textDocument/hover'.
    /// </summary>
    public static readonly LspRequest<HoverParams, Hover> TextDocumentHover = new(TextDocumentHoverName);

    /// <summary>
    /// Method name for 'textDocument/codeLens'.
    /// <para>
    /// The code lens request is sent from the client to the server to compute code lenses for a given text document.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_codeLens">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentCodeLensName = "textDocument/codeLens";

    /// <summary>
    /// Strongly typed message object for 'textDocument/codeLens'.
    /// </summary>
    public static readonly LspRequest<CodeLensParams, CodeLens[]?> TextDocumentCodeLens = new(TextDocumentCodeLensName);

    /// <summary>
    /// Method name for 'codeLens/resolve'.
    /// <para>
    /// The code lens resolve request is sent from the client to the server to resolve the command for a given code lens item.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeLens_resolve">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string CodeLensResolveName = "codeLens/resolve";

    /// <summary>
    /// Strongly typed message object for 'codeLens/resolve'.
    /// </summary>
    public static readonly LspRequest<CodeLens, CodeLens> CodeLensResolve = new(CodeLensResolveName);

    /// <summary>
    /// Method name for 'workspace/codeLens/refresh'.
    /// <para>
    /// The workspace/codeLens/refresh request is sent from the server to the client. Servers can use it to ask clients to
    /// refresh the code lenses currently shown in editors. As a result the client should ask the server to recompute the
    /// code lenses for these editors.
    /// <para>
    /// This is useful if a server detects a configuration change which requires a re-calculation of all code lenses. Note
    /// that the client still has the freedom to delay the re-calculation of the code lenses if for example an editor
    /// is currently not visible.
    /// </para>
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeLens_refresh">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceCodeLensRefreshName = "workspace/codeLens/refresh";

    /// <summary>
    /// Strongly typed message object for 'workspace/codeLens/refresh'.
    /// </summary>
    public static readonly LspRequest<object?, object?> WorkspaceCodeLensRefresh = new(WorkspaceCodeLensRefreshName);

    /// <summary>
    /// Method name for 'textDocument/foldingRange'.
    /// <para>
    /// The folding range request is sent from the client to the server to return all folding ranges found in a given text document.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_foldingRange">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.10</remarks>
    public const string TextDocumentFoldingRangeName = "textDocument/foldingRange";

    /// <summary>
    /// Strongly typed message object for 'textDocument/foldingRange'.
    /// </summary>
    public static readonly LspRequest<FoldingRangeParams, FoldingRange[]?> TextDocumentFoldingRange = new(TextDocumentFoldingRangeName);

    /// <summary>
    /// Method name for 'textDocument/selectionRange'.
    /// <para>
    /// The selection range request is sent from the client to the server to return suggested selection ranges at an array of given positions.
    /// </para>
    /// <para>
    /// A selection range is a range around the cursor position which the user might be interested in selecting.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_selectionRange">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    public const string TextDocumentSelectionRangeName = "textDocument/selectionRange";

    /// <summary>
    /// Strongly typed message object for 'textDocument/selectionRange'.
    /// </summary>
    public static readonly LspRequest<SelectionRangeParams, SelectionRange[]?> TextDocumentSelectionRange = new(TextDocumentSelectionRangeName);

    /// <summary>
    /// Method name for 'textDocument/documentSymbol'.
    /// <para>
    /// </para>
    /// The document symbol request is sent from the client to the server to return a collection of symbols in the document.
    /// <para>
    /// The returned result is either:
    /// <list type="bullet">
    /// <item>
    /// An array of <see cref="SymbolInformation"/>, which is a flat list of all symbols found in a given text document. Neither the symbol’s location range nor the symbol’s container name should be used to infer a hierarchy.
    /// </item>
    /// <item>
    /// An array of <see cref="DocumentSymbol"/>, which is a hierarchy of symbols found in a given text document.
    /// </item>
    /// </list>
    /// Servers should whenever possible return <see cref="DocumentSymbol"/> since it is the richer data structure.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_documentSymbol">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentDocumentSymbolName = "textDocument/documentSymbol";

    /// <summary>
    /// Strongly typed message object for 'textDocument/documentSymbol'.
    /// </summary>
    public static readonly LspRequest<DocumentSymbolParams, SumType<SymbolInformation[], DocumentSymbol[]>?> TextDocumentDocumentSymbol = new(TextDocumentDocumentSymbolName);
}
