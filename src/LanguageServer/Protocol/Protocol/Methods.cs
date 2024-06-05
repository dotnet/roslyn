// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Class which contains the string values for all common language protocol methods.
    /// </summary>
    internal static partial class Methods
    {
        // NOTE: these are sorted/grouped in the order used by the spec

        /// <summary>
        /// Method name for '$/progress' notifications.
        /// </summary>
        public const string ProgressNotificationName = "$/progress";

        /// <summary>
        /// Name of the progress token in the request.
        /// </summary>
        public const string PartialResultTokenName = "partialResultToken";

        /// <summary>
        /// Name of the work done token in the request.
        /// </summary>
        public const string WorkDoneTokenName = "workDoneToken";

        /// <summary>
        /// Name of the progress token in the $/progress notification.
        /// </summary>
        public const string ProgressNotificationTokenName = "token";

        /// <summary>
        /// Method name for 'textDocument/codeAction'.
        /// </summary>
        public const string TextDocumentCodeActionName = "textDocument/codeAction";

        /// <summary>
        /// Method name for 'textDocument/codeLens'.
        /// </summary>
        public const string TextDocumentCodeLensName = "textDocument/codeLens";

        /// <summary>
        /// Method name for 'codeAction/resolve'.
        /// </summary>
        public const string CodeActionResolveName = "codeAction/resolve";

        /// <summary>
        /// Method name for 'codeLens/resolve'.
        /// </summary>
        public const string CodeLensResolveName = "codeLens/resolve";

        /// <summary>
        /// Method name for 'textDocument/completion'.
        /// </summary>
        public const string TextDocumentCompletionName = "textDocument/completion";

        /// <summary>
        /// Method name for 'completionItem/resolve'.
        /// </summary>
        public const string TextDocumentCompletionResolveName = "completionItem/resolve";

        /// <summary>
        /// Method name for 'textDocument/definition'.
        /// </summary>
        public const string TextDocumentDefinitionName = "textDocument/definition";

        /// <summary>
        /// Method name for 'textDocument/diagnostic'.
        /// </summary>
        public const string TextDocumentDiagnosticName = "textDocument/diagnostic";

        /// <summary>
        /// Method name for 'textDocument/didOpen'.
        /// </summary>
        public const string TextDocumentDidOpenName = "textDocument/didOpen";

        /// <summary>
        /// Method name for 'textDocument/didClose'.
        /// </summary>
        public const string TextDocumentDidCloseName = "textDocument/didClose";

        /// <summary>
        /// Method name for 'textDocument/didChange'.
        /// </summary>
        public const string TextDocumentDidChangeName = "textDocument/didChange";

        /// <summary>
        /// Method name for 'textDocument/didSave'.
        /// </summary>
        public const string TextDocumentDidSaveName = "textDocument/didSave";

        /// <summary>
        /// Method name for 'textDocument/documentHighlight'.
        /// </summary>
        public const string TextDocumentDocumentHighlightName = "textDocument/documentHighlight";

        /// <summary>
        /// Method name for 'textDocument/documentLink'.
        /// </summary>
        public const string TextDocumentDocumentLinkName = "textDocument/documentLink";

        /// <summary>
        /// Method name for 'documentLink/resolve'.
        /// </summary>
        public const string DocumentLinkResolveName = "documentLink/resolve";

        /// <summary>
        /// Method name for 'textDocument/documentColor'.
        /// </summary>
        public const string TextDocumentDocumentColorName = "textDocument/documentColor";

        /// <summary>
        /// Method name for 'textDocument/documentSymbol'.
        /// </summary>
        public const string TextDocumentDocumentSymbolName = "textDocument/documentSymbol";

        /// <summary>
        /// Method name for 'textDocument/foldingRange'.
        /// </summary>
        public const string TextDocumentFoldingRangeName = "textDocument/foldingRange";

        /// <summary>
        /// Method name for 'textDocument/formatting'.
        /// </summary>
        public const string TextDocumentFormattingName = "textDocument/formatting";

        /// <summary>
        /// Method name for 'textDocument/hover'.
        /// </summary>
        public const string TextDocumentHoverName = "textDocument/hover";

        /// <summary>
        /// Method name for 'textDocument/onTypeFormatting'.
        /// </summary>
        public const string TextDocumentOnTypeFormattingName = "textDocument/onTypeFormatting";

        /// <summary>
        /// Method name for 'textDocument/rangeFormatting'.
        /// </summary>
        public const string TextDocumentRangeFormattingName = "textDocument/rangeFormatting";

        /// <summary>
        /// Method name for 'textDocument/publishDiagnostics'.
        /// </summary>
        public const string TextDocumentPublishDiagnosticsName = "textDocument/publishDiagnostics";

        /// <summary>
        /// Method name for 'textDocument/implementation'.
        /// </summary>
        public const string TextDocumentImplementationName = "textDocument/implementation";

        /// <summary>
        /// Method name for 'textDocument/inlayHint'.
        /// </summary>
        public const string TextDocumentInlayHintName = "textDocument/inlayHint";

        /// <summary>
        /// Method name for 'inlayHint/resolve'.
        /// </summary>
        public const string InlayHintResolveName = "inlayHint/resolve";

        /// <summary>
        /// Method name for 'textDocument/typeDefinition'.
        /// </summary>
        public const string TextDocumentTypeDefinitionName = "textDocument/typeDefinition";

        /// <summary>
        /// Method name for 'textDocument/references'.
        /// </summary>
        public const string TextDocumentReferencesName = "textDocument/references";

        /// <summary>
        /// Method name for 'textDocument/rename'.
        /// </summary>
        public const string TextDocumentRenameName = "textDocument/rename";

        /// <summary>
        /// Method name for 'textDocument/prepareRename'.
        /// </summary>
        public const string TextDocumentPrepareRenameName = "textDocument/prepareRename";

        /// <summary>
        /// Method name for 'textDocument/semanticTokens/full'.
        /// </summary>
        public const string TextDocumentSemanticTokensFullName = "textDocument/semanticTokens/full";

        /// <summary>
        /// Method name for 'textDocument/semanticTokens/range'.
        /// </summary>
        public const string TextDocumentSemanticTokensRangeName = "textDocument/semanticTokens/range";

        /// <summary>
        /// Method name for 'textDocument/semanticTokens/full/delta'.
        /// </summary>
        public const string TextDocumentSemanticTokensFullDeltaName = "textDocument/semanticTokens/full/delta";

        /// <summary>
        /// Method name for 'textDocument/signatureHelp'.
        /// </summary>
        public const string TextDocumentSignatureHelpName = "textDocument/signatureHelp";

        /// <summary>
        /// Method name for 'textDocument/willSave'.
        /// </summary>
        public const string TextDocumentWillSaveName = "textDocument/willSave";

        /// <summary>
        /// Method name for 'textDocument/willSaveWaitUntil'.
        /// </summary>
        public const string TextDocumentWillSaveWaitUntilName = "textDocument/willSaveWaitUntil";

        /// <summary>
        /// Method name for 'textDocument/linkedEditingRange'.
        /// </summary>
        public const string TextDocumentLinkedEditingRangeName = "textDocument/linkedEditingRange";

        /// <summary>
        /// Method name for 'window/logMessage'.
        /// </summary>
        public const string WindowLogMessageName = "window/logMessage";

        /// <summary>
        /// Method name for 'window/showMessage'.
        /// </summary>
        public const string WindowShowMessageName = "window/showMessage";

        /// <summary>
        /// Method name for 'window/showMessageRequest'.
        /// </summary>
        public const string WindowShowMessageRequestName = "window/showMessageRequest";

        /// <summary>
        /// Method name for 'workspace/applyEdit'.
        /// </summary>
        public const string WorkspaceApplyEditName = "workspace/applyEdit";

        /// <summary>
        /// Method name for 'workspace/semanticTokens/refresh'.
        /// </summary>
        public const string WorkspaceSemanticTokensRefreshName = "workspace/semanticTokens/refresh";

        /// <summary>
        /// Method name for 'workspace/configuration'.
        /// </summary>
        public const string WorkspaceConfigurationName = "workspace/configuration";

        /// <summary>
        /// Method name for 'workspace/diagnostic'.
        /// </summary>
        public const string WorkspaceDiagnosticName = "workspace/diagnostic";

        /// <summary>
        /// Method name for 'workspace/diagnostic/refresh'.
        /// </summary>
        public const string WorkspaceDiagnosticRefreshName = "workspace/diagnostic/refresh";

        /// <summary>
        /// Method name for 'workspace/didChangeConfiguration'.
        /// </summary>
        public const string WorkspaceDidChangeConfigurationName = "workspace/didChangeConfiguration";

        /// <summary>
        /// Method name for 'workspace/executeCommand'.
        /// </summary>
        public const string WorkspaceExecuteCommandName = "workspace/executeCommand";

        /// <summary>
        /// Method name for 'workspace/symbol'.
        /// </summary>
        public const string WorkspaceSymbolName = "workspace/symbol";

        /// <summary>
        /// Method name for 'workspace/didChangeWatchedFiles'.
        /// </summary>
        public const string WorkspaceDidChangeWatchedFilesName = "workspace/didChangeWatchedFiles";

        /// <summary>
        /// Method name for 'workspace/codeLens/refresh'.
        /// </summary>
        public const string WorkspaceCodeLensRefreshName = "workspace/codeLens/refresh";

        /// <summary>
        /// Method name for 'workspace/inlayHint/refresh'.
        /// </summary>
        public const string WorkspaceInlayHintRefreshName = "workspace/inlayHint/refresh";

        /// <summary>
        /// Method name for 'telemetry/event'.
        /// </summary>
        public const string TelemetryEventName = "telemetry/event";

        /// <summary>
        /// Strongly typed message object for 'textDocument/codeAction'.
        /// </summary>
        public static readonly LspRequest<CodeActionParams, SumType<Command, CodeAction>[]?> TextDocumentCodeAction = new LspRequest<CodeActionParams, SumType<Command, CodeAction>[]?>(TextDocumentCodeActionName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/codeLens'.
        /// </summary>
        public static readonly LspRequest<CodeLensParams, CodeLens[]?> TextDocumentCodeLens = new LspRequest<CodeLensParams, CodeLens[]?>(TextDocumentCodeLensName);

        /// <summary>
        /// Strongly typed message object for 'codeAction/resolve'.
        /// </summary>
        public static readonly LspRequest<CodeAction, CodeAction> CodeActionResolve = new LspRequest<CodeAction, CodeAction>(CodeActionResolveName);

        /// <summary>
        /// Strongly typed message object for 'codeLens/resolve'.
        /// </summary>
        public static readonly LspRequest<CodeLens, CodeLens> CodeLensResolve = new LspRequest<CodeLens, CodeLens>(CodeLensResolveName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/completion'.
        /// </summary>
        public static readonly LspRequest<CompletionParams, SumType<CompletionItem[], CompletionList>?> TextDocumentCompletion = new LspRequest<CompletionParams, SumType<CompletionItem[], CompletionList>?>(TextDocumentCompletionName);

        /// <summary>
        /// Strongly typed message object for 'completionItem/resolve'.
        /// </summary>
        public static readonly LspRequest<CompletionItem, CompletionItem> TextDocumentCompletionResolve = new LspRequest<CompletionItem, CompletionItem>(TextDocumentCompletionResolveName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/definition'.
        /// </summary>
        public static readonly LspRequest<TextDocumentPositionParams, SumType<Location, Location[]>?> TextDocumentDefinition = new LspRequest<TextDocumentPositionParams, SumType<Location, Location[]>?>(TextDocumentDefinitionName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/didOpen'.
        /// </summary>
        public static readonly LspNotification<DidOpenTextDocumentParams> TextDocumentDidOpen = new LspNotification<DidOpenTextDocumentParams>(TextDocumentDidOpenName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/didClose'.
        /// </summary>
        public static readonly LspNotification<DidCloseTextDocumentParams> TextDocumentDidClose = new LspNotification<DidCloseTextDocumentParams>(TextDocumentDidCloseName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/didChange'.
        /// </summary>
        public static readonly LspNotification<DidChangeTextDocumentParams> TextDocumentDidChange = new LspNotification<DidChangeTextDocumentParams>(TextDocumentDidChangeName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/didSave'.
        /// </summary>
        public static readonly LspNotification<DidSaveTextDocumentParams> TextDocumentDidSave = new LspNotification<DidSaveTextDocumentParams>(TextDocumentDidSaveName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/documentHighlight'.
        /// </summary>
        public static readonly LspRequest<DocumentHighlightParams, DocumentHighlight[]?> TextDocumentDocumentHighlight = new LspRequest<DocumentHighlightParams, DocumentHighlight[]?>(TextDocumentDocumentHighlightName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/documentLink'.
        /// </summary>
        public static readonly LspRequest<DocumentLinkParams, DocumentLink[]?> TextDocumentDocumentLink = new LspRequest<DocumentLinkParams, DocumentLink[]?>(TextDocumentDocumentLinkName);

        /// <summary>
        /// Strongly typed message object for 'documentLink/resolve'.
        /// </summary>
        public static readonly LspRequest<DocumentLink, DocumentLink> DocumentLinkResolve = new LspRequest<DocumentLink, DocumentLink>(DocumentLinkResolveName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/documentColor'.
        /// </summary>
        public static readonly LspRequest<DocumentColorParams, ColorInformation[]> DocumentColorRequest = new LspRequest<DocumentColorParams, ColorInformation[]>(TextDocumentDocumentColorName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/documentSymbol'.
        /// </summary>
        public static readonly LspRequest<DocumentSymbolParams, SymbolInformation[]?> TextDocumentDocumentSymbol = new LspRequest<DocumentSymbolParams, SymbolInformation[]?>(TextDocumentDocumentSymbolName);

        /// <summary>
        /// Stronly typed message object for 'textDocument/foldingRange'.
        /// </summary>
        public static readonly LspRequest<FoldingRangeParams, FoldingRange[]?> TextDocumentFoldingRange = new LspRequest<FoldingRangeParams, FoldingRange[]?>(TextDocumentFoldingRangeName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/formatting'.
        /// </summary>
        public static readonly LspRequest<DocumentFormattingParams, TextEdit[]?> TextDocumentFormatting = new LspRequest<DocumentFormattingParams, TextEdit[]?>(TextDocumentFormattingName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/hover'.
        /// </summary>
        public static readonly LspRequest<TextDocumentPositionParams, Hover> TextDocumentHover = new LspRequest<TextDocumentPositionParams, Hover>(TextDocumentHoverName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/onTypeFormatting'.
        /// </summary>
        public static readonly LspRequest<DocumentOnTypeFormattingParams, TextEdit[]?> TextDocumentOnTypeFormatting = new LspRequest<DocumentOnTypeFormattingParams, TextEdit[]?>(TextDocumentOnTypeFormattingName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/rangeFormatting'.
        /// </summary>
        public static readonly LspRequest<DocumentRangeFormattingParams, TextEdit[]?> TextDocumentRangeFormatting = new LspRequest<DocumentRangeFormattingParams, TextEdit[]?>(TextDocumentRangeFormattingName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/publishDiagnostics'.
        /// </summary>
        public static readonly LspNotification<PublishDiagnosticParams> TextDocumentPublishDiagnostics = new LspNotification<PublishDiagnosticParams>(TextDocumentPublishDiagnosticsName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/implementation'.
        /// </summary>
        public static readonly LspRequest<TextDocumentPositionParams, SumType<Location, Location[]>?> TextDocumentImplementation = new LspRequest<TextDocumentPositionParams, SumType<Location, Location[]>?>(TextDocumentImplementationName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/inlayHint'.
        /// </summary>
        public static readonly LspRequest<InlayHintParams, InlayHint[]?> TextDocumentInlayHint = new LspRequest<InlayHintParams, InlayHint[]?>(TextDocumentInlayHintName);

        /// <summary>
        /// Strongly typed message object for 'inlayHint/resolve'.
        /// </summary>
        public static readonly LspRequest<InlayHint, InlayHint> InlayHintResolve = new LspRequest<InlayHint, InlayHint>(InlayHintResolveName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/typeDefinition'.
        /// </summary>
        public static readonly LspRequest<TextDocumentPositionParams, SumType<Location, Location[]>?> TextDocumentTypeDefinition = new LspRequest<TextDocumentPositionParams, SumType<Location, Location[]>?>(TextDocumentTypeDefinitionName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/references'.
        /// </summary>
        public static readonly LspRequest<ReferenceParams, Location[]?> TextDocumentReferences = new LspRequest<ReferenceParams, Location[]?>(TextDocumentReferencesName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/rename'.
        /// </summary>
        public static readonly LspRequest<RenameParams, WorkspaceEdit?> TextDocumentRename = new LspRequest<RenameParams, WorkspaceEdit?>(TextDocumentRenameName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/prepareRename'.
        /// </summary>
        public static readonly LspRequest<PrepareRenameParams, SumType<RenameRange, DefaultBehaviorPrepareRename, Range>?> TextDocumentPrepareRename = new LspRequest<PrepareRenameParams, SumType<RenameRange, DefaultBehaviorPrepareRename, Range>?>(TextDocumentPrepareRenameName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/signatureHelp'.
        /// </summary>
        public static readonly LspRequest<SignatureHelpParams, SignatureHelp?> TextDocumentSignatureHelp = new LspRequest<SignatureHelpParams, SignatureHelp?>(TextDocumentSignatureHelpName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/willSave'.
        /// </summary>
        public static readonly LspNotification<WillSaveTextDocumentParams> TextDocumentWillSave = new LspNotification<WillSaveTextDocumentParams>(TextDocumentWillSaveName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/willSaveWaitUntil'.
        /// </summary>
        public static readonly LspRequest<WillSaveTextDocumentParams, TextEdit[]?> TextDocumentWillSaveWaitUntil = new LspRequest<WillSaveTextDocumentParams, TextEdit[]?>(TextDocumentWillSaveWaitUntilName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/linkedEditingRange'.
        /// </summary>
        public static readonly LspRequest<LinkedEditingRangeParams, LinkedEditingRanges?> TextDocumentLinkedEditingRange = new LspRequest<LinkedEditingRangeParams, LinkedEditingRanges?>(TextDocumentLinkedEditingRangeName);

        /// <summary>
        /// Strongly typed message object for 'window/logMessage'.
        /// </summary>
        public static readonly LspNotification<LogMessageParams> WindowLogMessage = new LspNotification<LogMessageParams>(WindowLogMessageName);

        /// <summary>
        /// Strongly typed message object for 'window/showMessage'.
        /// </summary>
        public static readonly LspNotification<ShowMessageParams> WindowShowMessage = new LspNotification<ShowMessageParams>(WindowShowMessageName);

        /// <summary>
        /// Strongly typed message object for 'window/showMessageRequest'.
        /// </summary>
        public static readonly LspRequest<ShowMessageRequestParams, MessageActionItem> WindowShowMessageRequest = new LspRequest<ShowMessageRequestParams, MessageActionItem>(WindowShowMessageRequestName);

        /// <summary>
        /// Strongly typed message object for 'workspace/applyEdit'.
        /// </summary>
        public static readonly LspRequest<ApplyWorkspaceEditParams, ApplyWorkspaceEditResponse> WorkspaceApplyEdit = new LspRequest<ApplyWorkspaceEditParams, ApplyWorkspaceEditResponse>(WorkspaceApplyEditName);

        /// <summary>
        /// Strongly typed message object for 'workspace/semanticTokens/refresh'.
        /// </summary>
        public static readonly LspRequest<object?, object?> WorkspaceSemanticTokensRefresh = new LspRequest<object?, object?>(WorkspaceSemanticTokensRefreshName);

        /// <summary>
        /// Strongly typed message object for 'workspace/configuration'.
        /// </summary>
        public static readonly LspRequest<ConfigurationParams, object?[]> WorkspaceConfiguration = new LspRequest<ConfigurationParams, object?[]>(WorkspaceConfigurationName);

        /// <summary>
        /// Strongly typed message object for 'workspace/didChangeConfiguration'.
        /// </summary>
        public static readonly LspNotification<DidChangeConfigurationParams> WorkspaceDidChangeConfiguration = new LspNotification<DidChangeConfigurationParams>(WorkspaceDidChangeConfigurationName);

        /// <summary>
        /// Strongly typed message object for 'workspace/executeCommand'.
        /// </summary>
        public static readonly LspRequest<ExecuteCommandParams, object?> WorkspaceExecuteCommand = new LspRequest<ExecuteCommandParams, object?>(WorkspaceExecuteCommandName);

        /// <summary>
        /// Strongly typed message object for 'workspace/symbol'.
        /// </summary>
        public static readonly LspRequest<WorkspaceSymbolParams, SymbolInformation[]?> WorkspaceSymbol = new LspRequest<WorkspaceSymbolParams, SymbolInformation[]?>(WorkspaceSymbolName);

        /// <summary>
        /// Strongly typed message object for 'workspace/didChangeWatchedFiles'.
        /// </summary>
        public static readonly LspNotification<DidChangeWatchedFilesParams> WorkspaceDidChangeWatchedFiles = new LspNotification<DidChangeWatchedFilesParams>(WorkspaceDidChangeWatchedFilesName);

        /// <summary>
        /// Strongly typed message object for 'workspace/codeLens/refresh'.
        /// </summary>
        public static readonly LspRequest<object?, object?> WorkspaceCodeLensRefresh = new LspRequest<object?, object?>(WorkspaceCodeLensRefreshName);

        /// <summary>
        /// Strongly typed message object for 'workspace/inlayHint/refresh'.
        /// </summary>
        public static readonly LspRequest<object?, object?> WorkspaceInlayHintRefresh = new LspRequest<object?, object?>(WorkspaceInlayHintRefreshName);

        /// <summary>
        /// Strongly typed message object for 'telemetry/event'.
        /// </summary>
        public static readonly LspNotification<object> TelemetryEvent = new LspNotification<object>(TelemetryEventName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/semanticTokens/full'.
        /// </summary>
        public static readonly LspRequest<SemanticTokensParams, SemanticTokens?> TextDocumentSemanticTokensFull = new LspRequest<SemanticTokensParams, SemanticTokens?>(TextDocumentSemanticTokensFullName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/semanticTokens/range'.
        /// </summary>
        public static readonly LspRequest<SemanticTokensRangeParams, SemanticTokens?> TextDocumentSemanticTokensRange = new LspRequest<SemanticTokensRangeParams, SemanticTokens?>(TextDocumentSemanticTokensRangeName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/semanticTokens/full/delta'.
        /// </summary>
        public static readonly LspRequest<SemanticTokensDeltaParams, SumType<SemanticTokens, SemanticTokensDelta>?> TextDocumentSemanticTokensFullDelta
            = new LspRequest<SemanticTokensDeltaParams, SumType<SemanticTokens, SemanticTokensDelta>?>(TextDocumentSemanticTokensFullDeltaName);
    }
}
