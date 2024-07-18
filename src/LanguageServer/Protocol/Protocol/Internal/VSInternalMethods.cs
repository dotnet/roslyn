// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Class which contains the string values for all Language Server Protocol Visual Studio specific methods.
    /// </summary>
    internal static class VSInternalMethods
    {
        /// <summary>
        /// Method name for 'textDocument/foldingRange/_vs_refresh'.
        /// </summary>
        public const string DocumentFoldingRangeRefreshName = "textDocument/foldingRange/_vs_refresh";

        /// <summary>
        /// Method name for 'textDocument/_vs_references'.
        /// </summary>
        public const string DocumentReferencesName = "textDocument/_vs_references";

        /// <summary>
        /// Method name for 'textDocument/_vs_onAutoInsert'.
        /// </summary>
        public const string OnAutoInsertName = "textDocument/_vs_onAutoInsert";

        /// <summary>
        /// Method name for 'textDocument/_vs_iconMappingResolve'.
        /// </summary>
        public const string TextDocumentIconMappingResolveName = "textDocument/_vs_iconMappingResolve";

        /// <summary>
        /// Method name for 'textdocument/_vs_diagnostic'.
        /// </summary>
        public const string DocumentPullDiagnosticName = "textdocument/_vs_diagnostic";

        /// <summary>
        /// Method name for 'workspace/_vs_diagnostic'.
        /// </summary>
        public const string WorkspacePullDiagnosticName = "workspace/_vs_diagnostic";

        /// <summary>
        /// Method name for 'textDocument/_vs_validateBreakableRange'.
        /// </summary>
        public const string TextDocumentValidateBreakableRangeName = "textDocument/_vs_validateBreakableRange";

        /// <summary>
        /// Method name for 'textDocument/_vs_inlineCompletion'.
        /// </summary>
        public const string TextDocumentInlineCompletionName = "textDocument/_vs_inlineCompletion";

        /// <summary>
        /// Method name for 'textDocument/_vs_spellCheckableRanges'.
        /// </summary>
        public const string TextDocumentSpellCheckableRangesName = "textDocument/_vs_spellCheckableRanges";

        /// <summary>
        /// Method name for 'textDocument/_vs_uriPresentation'.
        /// </summary>
        public const string TextDocumentUriPresentationName = "textDocument/_vs_uriPresentation";

        /// <summary>
        /// Method name for 'textDocument/_vs_textPresentation'.
        /// </summary>
        public const string TextDocumentTextPresentationName = "textDocument/_vs_textPresentation";

        /// <summary>
        /// Method name for 'workspace/_vs_spellCheckableRanges'.
        /// </summary>
        public const string WorkspaceSpellCheckableRangesName = "workspace/_vs_spellCheckableRanges";

        /// <summary>
        /// Method name for 'workspace/_vs_mapCode'.
        /// </summary>
        public const string WorkspaceMapCodeName = "workspace/_vs_mapCode";

        /// <summary>
        /// Strongly typed message object for 'textDocument/_vs_onAutoInsert'.
        /// </summary>
        public static readonly LspRequest<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem> OnAutoInsert = new LspRequest<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem>(OnAutoInsertName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/_vs_iconMappingResolve'.
        /// </summary>
        public static readonly LspRequest<VSInternalKindAndModifier, VSInternalIconMapping> TextDocumentIconMappingResolve = new LspRequest<VSInternalKindAndModifier, VSInternalIconMapping>(TextDocumentIconMappingResolveName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/_vs_diagnostic'.
        /// </summary>
        public static readonly LspRequest<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]> DocumentPullDiagnostic = new LspRequest<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]>(DocumentPullDiagnosticName);

        /// <summary>
        /// Strongly typed message object for 'workspace/_vs_diagnostic'.
        /// </summary>
        public static readonly LspRequest<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]> WorkspacePullDiagnostic = new LspRequest<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]>(WorkspacePullDiagnosticName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/_vs_validateBreakableRange'.
        /// </summary>
        public static readonly LspRequest<VSInternalValidateBreakableRangeParams, Range?> TextDocumentValidateBreakableRange = new LspRequest<VSInternalValidateBreakableRangeParams, Range?>(TextDocumentValidateBreakableRangeName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/inlineCompletion'.
        /// </summary>
        public static readonly LspRequest<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList> TextDocumentInlineCompletion = new LspRequest<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList>(TextDocumentInlineCompletionName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/_vs_uriPresentation'.
        /// </summary>
        public static readonly LspRequest<VSInternalUriPresentationParams, WorkspaceEdit?> TextDocumentUriPresentation = new LspRequest<VSInternalUriPresentationParams, WorkspaceEdit?>(TextDocumentUriPresentationName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/_vs_textPresentation'.
        /// </summary>
        public static readonly LspRequest<VSInternalTextPresentationParams, WorkspaceEdit?> TextDocumentTextPresentation = new LspRequest<VSInternalTextPresentationParams, WorkspaceEdit?>(TextDocumentTextPresentationName);

        /// <summary>
        /// Strongly typed message object for 'textDocument/_vs_spellCheckableRanges'.
        /// </summary>
        public static readonly LspRequest<VSInternalDocumentSpellCheckableParams, VSInternalSpellCheckableRangeReport[]> TextDocumentSpellCheckableRanges = new LspRequest<VSInternalDocumentSpellCheckableParams, VSInternalSpellCheckableRangeReport[]>(TextDocumentSpellCheckableRangesName);

        /// <summary>
        /// Strongly typed message object for 'workspace/_vs_spellCheckableRanges'.
        /// </summary>
        public static readonly LspRequest<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport[]> WorkspaceSpellCheckableRanges = new LspRequest<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport[]>(WorkspaceSpellCheckableRangesName);

        /// <summary>
        /// Strongly typed message object for 'workspace/_vs_mapCode'
        /// </summary>
        public static readonly LspRequest<VSInternalMapCodeParams, WorkspaceEdit?> WorkspaceMapCode = new(WorkspaceMapCodeName);
    }
}
