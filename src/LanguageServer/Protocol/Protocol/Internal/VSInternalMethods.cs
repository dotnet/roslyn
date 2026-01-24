// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Class which contains the string values for all Language Server Protocol Visual Studio specific methods.
/// </summary>
internal static class VSInternalMethods
{
    public const string CopilotRelatedDocumentsName = "copilot/_related_documents";
    public const string DocumentFoldingRangeRefreshName = "textDocument/foldingRange/_vs_refresh";
    public const string DocumentPullDiagnosticName = "textdocument/_vs_diagnostic";
    public const string DocumentReferencesName = "textDocument/_vs_references";
    public const string OnAutoInsertName = "textDocument/_vs_onAutoInsert";
    public const string TextDocumentDataTipRangeName = "textdocument/_vs_dataTipRange";
    public const string TextDocumentIconMappingResolveName = "textDocument/_vs_iconMappingResolve";
    public const string TextDocumentInlineCompletionName = "textDocument/_vs_inlineCompletion";
    public const string TextDocumentSpellCheckableRangesName = "textDocument/_vs_spellCheckableRanges";
    public const string TextDocumentTextPresentationName = "textDocument/_vs_textPresentation";
    public const string TextDocumentUriPresentationName = "textDocument/_vs_uriPresentation";
    public const string TextDocumentValidateBreakableRangeName = "textDocument/_vs_validateBreakableRange";
    public const string WorkspaceMapCodeName = "workspace/_vs_mapCode";
    public const string WorkspaceProjectContextRefreshName = "workspace/projectContext/_vs_refresh";
    public const string WorkspacePullDiagnosticName = "workspace/_vs_diagnostic";
    public const string WorkspaceSpellCheckableRangesName = "workspace/_vs_spellCheckableRanges";

    /// <summary>
    /// Strongly typed message object for 'textDocument/_vs_onAutoInsert'.
    /// </summary>
    public static readonly LspRequest<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem> OnAutoInsert = new(OnAutoInsertName);

    /// <summary>
    /// Strongly typed message object for 'textDocument/_vs_iconMappingResolve'.
    /// </summary>
    public static readonly LspRequest<VSInternalKindAndModifier, VSInternalIconMapping> TextDocumentIconMappingResolve = new(TextDocumentIconMappingResolveName);

    /// <summary>
    /// Strongly typed message object for 'textDocument/_vs_diagnostic'.
    /// </summary>
    public static readonly LspRequest<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]> DocumentPullDiagnostic = new(DocumentPullDiagnosticName);

    /// <summary>
    /// Strongly typed message object for 'workspace/_vs_diagnostic'.
    /// </summary>
    public static readonly LspRequest<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]> WorkspacePullDiagnostic = new(WorkspacePullDiagnosticName);

    /// <summary>
    /// Strongly typed message object for 'textDocument/_vs_validateBreakableRange'.
    /// </summary>
    public static readonly LspRequest<VSInternalValidateBreakableRangeParams, Range?> TextDocumentValidateBreakableRange = new(TextDocumentValidateBreakableRangeName);

    /// <summary>
    /// Strongly typed message object for 'textDocument/inlineCompletion'.
    /// </summary>
    public static readonly LspRequest<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList> TextDocumentInlineCompletion = new(TextDocumentInlineCompletionName);

    /// <summary>
    /// Strongly typed message object for 'textDocument/_vs_uriPresentation'.
    /// </summary>
    public static readonly LspRequest<VSInternalUriPresentationParams, WorkspaceEdit?> TextDocumentUriPresentation = new(TextDocumentUriPresentationName);

    /// <summary>
    /// Strongly typed message object for 'textDocument/_vs_textPresentation'.
    /// </summary>
    public static readonly LspRequest<VSInternalTextPresentationParams, WorkspaceEdit?> TextDocumentTextPresentation = new(TextDocumentTextPresentationName);

    /// <summary>
    /// Strongly typed message object for 'textDocument/_vs_spellCheckableRanges'.
    /// </summary>
    public static readonly LspRequest<VSInternalDocumentSpellCheckableParams, VSInternalSpellCheckableRangeReport[]> TextDocumentSpellCheckableRanges = new(TextDocumentSpellCheckableRangesName);

    /// <summary>
    /// Strongly typed message object for 'workspace/_vs_spellCheckableRanges'.
    /// </summary>
    public static readonly LspRequest<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport[]> WorkspaceSpellCheckableRanges = new(WorkspaceSpellCheckableRangesName);

    /// <summary>
    /// Strongly typed message object for 'workspace/_vs_mapCode'
    /// </summary>
    public static readonly LspRequest<VSInternalMapCodeParams, WorkspaceEdit?> WorkspaceMapCode = new(WorkspaceMapCodeName);
}
