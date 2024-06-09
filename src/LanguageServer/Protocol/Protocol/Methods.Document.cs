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

    /// <summary>
    /// Method name for 'textDocument/semanticTokens'.
    /// <para>
    /// This method name is used only for registering for semantic tokens requests.
    /// </para>
    /// <para>
    /// For actual requests, the specific methods <c>textDocument/semanticTokens/{full,full/delta,range}</c> are used.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokens_fullRequest">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string TextDocumentSemanticTokensName = "textDocument/semanticTokens";

    /// <summary>
    /// Method name for 'textDocument/semanticTokens/full'.
    /// <para>
    /// Returns semantic tokens for the full document.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokens_fullRequest">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string TextDocumentSemanticTokensFullName = "textDocument/semanticTokens/full";

    /// <summary>
    /// Strongly typed message object for 'textDocument/semanticTokens/full'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<SemanticTokensParams, SemanticTokens?> TextDocumentSemanticTokensFull = new(TextDocumentSemanticTokensFullName);

    /// <summary>
    /// Method name for 'textDocument/semanticTokens/full/delta'.
    /// <para>
    /// Returns a delta against a previous set of semantic tokens for the full document.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokens_deltaRequest">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string TextDocumentSemanticTokensFullDeltaName = "textDocument/semanticTokens/full/delta";

    /// <summary>
    /// Strongly typed message object for 'textDocument/semanticTokens/full/delta'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<SemanticTokensDeltaParams, SumType<SemanticTokens, SemanticTokensDelta>?> TextDocumentSemanticTokensFullDelta = new(TextDocumentSemanticTokensFullDeltaName);

    /// <summary>
    /// Method name for 'textDocument/semanticTokens/range'.
    /// <para>
    /// Returns semantic tokens for a visible range of the document.
    /// </para>
    /// <para>
    /// This allows clients to improved rendering performance when opening files
    /// and allow rendering documents that are too large for full semantic coloring.
    /// </para>
    /// <para>
    /// A server is allowed to compute the semantic tokens for a broader range than
    /// requested by the client. However if the server does the semantic tokens
    /// for the broader range must be complete and correct.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokens_rangeRequest">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string TextDocumentSemanticTokensRangeName = "textDocument/semanticTokens/range";

    /// <summary>
    /// Strongly typed message object for 'textDocument/semanticTokens/range'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<SemanticTokensRangeParams, SemanticTokens?> TextDocumentSemanticTokensRange = new(TextDocumentSemanticTokensRangeName);

    /// <summary>
    /// Method name for 'workspace/semanticTokens/refresh'.
    /// <para>
    /// This request is sent from the server to the client. Servers can use it to ask clients to refresh the editors for
    /// which this server provides semantic tokens.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokens_refreshRequest">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public const string WorkspaceSemanticTokensRefreshName = "workspace/semanticTokens/refresh";

    /// <summary>
    /// Strongly typed message object for 'workspace/semanticTokens/refresh'.
    /// </summary>
    /// <remarks>Since LSP 3.16</remarks>
    public static readonly LspRequest<object?, object?> WorkspaceSemanticTokensRefresh = new(WorkspaceSemanticTokensRefreshName);

    /// <summary>
    /// Method name for 'textDocument/inlayHint'.
    /// <para>
    /// The inlay hints request is sent from the client to the server to compute inlay hints
    /// for a given [text document, range] tuple that may be rendered in the editor in place with other text.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_inlayHint">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string TextDocumentInlayHintName = "textDocument/inlayHint";

    /// <summary>
    /// Strongly typed message object for 'textDocument/inlayHint'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<InlayHintParams, InlayHint[]?> TextDocumentInlayHint = new(TextDocumentInlayHintName);

    /// <summary>
    /// Method name for 'inlayHint/resolve'.
    /// <para>
    /// The request is sent from the client to the server to resolve additional information for
    /// a given inlay hint.
    /// </para>
    /// <para>
    /// This is usually used to compute the tooltip, location or command
    /// properties of an inlay hint’s label part to avoid its unnecessary computation during
    /// the <c>textDocument/inlayHint</c> request.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#inlayHint_resolve">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string InlayHintResolveName = "inlayHint/resolve";

    /// <summary>
    /// Strongly typed message object for 'inlayHint/resolve'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<InlayHint, InlayHint> InlayHintResolve = new(InlayHintResolveName);

    /// <summary>
    /// Method name for 'workspace/inlayHint/refresh'.
    /// <para>
    /// This request is sent from the server to ask the client to refresh the inlay hints currently shown in editors.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_inlayHint_refresh">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string WorkspaceInlayHintRefreshName = "workspace/inlayHint/refresh";

    /// <summary>
    /// Strongly typed message object for 'workspace/inlayHint/refresh'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<object?, object?> WorkspaceInlayHintRefresh = new(WorkspaceInlayHintRefreshName);

    /// <summary>
    /// Method name for 'textDocument/inlineValue'.
    /// <para>
    /// The inline value request is sent from the client to the server to compute inline values for
    /// a given text document that may be rendered in the editor at the end of lines..
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_inlineValue">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string TextDocumentInlineValueName = "textDocument/inlineValue";

    /// <summary>
    /// Strongly typed message object for 'textDocument/inlineValue'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<InlineValueParams, SumType<InlineValueText, InlineValueVariableLookup, InlineValueEvaluatableExpression>[]?> TextDocumentInlineValue = new(TextDocumentInlineValueName);

    /// <summary>
    /// Method name for 'workspace/inlineValue/refresh'.
    /// <para>
    /// This request is sent from the server to ask the client to refresh the inline values currently shown in editors.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspace_inlineValue_refresh">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string WorkspaceInlineValueRefreshName = "workspace/inlineValue/refresh";

    /// <summary>
    /// Strongly typed message object for 'workspace/inlineValue/refresh'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<object?, object?> WorkspaceInlineValueRefresh = new(WorkspaceInlineValueRefreshName);

    /// <summary>
    /// Method name for 'textDocument/moniker'.
    /// <para>
    /// Provide the same symbol moniker information used by Language Server Index Format (LSIF) given a text document position.
    /// </para>
    /// <para>
    /// Clients can utilize this method to get the moniker at the current location in a file user is editing and do
    /// further code navigation queries in other services that rely on LSIF indexes and link symbols together.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_moniker">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public const string TextDocumentMonikerName = "textDocument/moniker";

    /// <summary>
    /// Strongly typed message object for 'textDocument/moniker'.
    /// </summary>
    /// <remarks>Since LSP 3.17</remarks>
    public static readonly LspRequest<MonikerParams, Moniker[]?> TextDocumentMoniker = new(TextDocumentMonikerName);

    /// <summary>
    /// Method name for 'textDocument/completion'.
    /// <para>
    /// The Completion request is sent from the client to the server to compute completion items at a given cursor position.
    /// </para>
    /// <para>
    /// If computing full completion items is expensive, servers can additionally provide a handler for the completion
    /// item resolve request (‘completionItem/resolve’), which is sent when a completion item is selected in the user interface. 
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_completion">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentCompletionName = "textDocument/completion";

    /// <summary>
    /// Strongly typed message object for 'textDocument/completion'.
    /// </summary>
    public static readonly LspRequest<CompletionParams, SumType<CompletionItem[], CompletionList>?> TextDocumentCompletion = new(TextDocumentCompletionName);

    /// <summary>
    /// Method name for 'completionItem/resolve'.
    /// <para>
    /// The request is sent from the client to the server to resolve additional information for a given completion item.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionItem_resolve">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentCompletionResolveName = "completionItem/resolve";

    /// <summary>
    /// Strongly typed message object for 'completionItem/resolve'.
    /// </summary>
    public static readonly LspRequest<CompletionItem, CompletionItem> TextDocumentCompletionResolve = new(TextDocumentCompletionResolveName);

    /// <summary>
    /// Method name for 'textDocument/signatureHelp'.
    /// <para>
    /// The signature help request is sent from the client to the server to request signature information at a given cursor position.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_signatureHelp">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string TextDocumentSignatureHelpName = "textDocument/signatureHelp";

    /// <summary>
    /// Strongly typed message object for 'textDocument/signatureHelp'.
    /// </summary>
    public static readonly LspRequest<SignatureHelpParams, SignatureHelp?> TextDocumentSignatureHelp = new(TextDocumentSignatureHelpName);

    /// <summary>
    /// Method name for 'textDocument/codeAction'.
    /// </summary>
    public const string TextDocumentCodeActionName = "textDocument/codeAction";

    /// <summary>
    /// Strongly typed message object for 'textDocument/codeAction'.
    /// <para>
    /// The code action request is sent from the client to the server to compute commands for a
    /// given text document and range. These commands are typically code fixes to either fix
    /// problems or to beautify/refactor code.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_codeAction">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public static readonly LspRequest<CodeActionParams, SumType<Command, CodeAction>[]?> TextDocumentCodeAction = new(TextDocumentCodeActionName);

    /// <summary>
    /// Method name for 'codeAction/resolve'.
    /// <para>
    /// The request is sent from the client to the server to resolve additional information
    /// for a given code action.
    /// </para>
    /// <para>
    /// This is usually used to compute the edit property of a code action to avoid its
    /// unnecessary computation during the textDocument/codeAction request.
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeAction_resolve">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    public const string CodeActionResolveName = "codeAction/resolve";

    /// <summary>
    /// Strongly typed message object for 'codeAction/resolve'.
    /// </summary>
    public static readonly LspRequest<CodeAction, CodeAction> CodeActionResolve = new(CodeActionResolveName);

    /// <summary>
    /// Method name for 'textDocument/documentColor'.
    /// <para>
    /// The document color request is sent from the client to the server to list all color references
    /// found in a given text document. Along with the range, a color value in RGB is returned.
    /// </para>
    /// <para>
    /// Clients can use the result to decorate color references in an editor. For example:
    /// <list type="bullet">
    /// <item>
    /// Color boxes showing the actual color next to the reference
    /// </item>
    /// <item>
    /// Show a color picker when a color reference is edited
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_documentColor">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public const string TextDocumentDocumentColorName = "textDocument/documentColor";

    /// <summary>
    /// Strongly typed message object for 'textDocument/documentColor'.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public static readonly LspRequest<DocumentColorParams, ColorInformation[]> TextDocumentDocumentColor = new(TextDocumentDocumentColorName);

    /// <summary>
    /// Method name for 'textDocument/colorPresentation'.
    /// <para>
    /// The color presentation request is sent from the client to the server to obtain a list of presentations for a color value at a given location.
    /// <para>
    /// </para>
    /// Clients can use the result to:
    /// <list type="bullet">
    /// <item>
    /// modify a color reference.
    /// </item>
    /// <item>
    /// show in a color picker and let users pick one of the presentations
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_colorPresentation">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public const string TextDocumentColorPresentationName = "textDocument/colorPresentation";

    /// <summary>
    /// Strongly typed message object for 'textDocument/colorPresentation'.
    /// </summary>
    /// <remarks>Since LSP 3.6</remarks>
    public static readonly LspRequest<ColorPresentationParams, ColorPresentation[]> TextDocumentColorPresentation = new(TextDocumentColorPresentationName);
}
