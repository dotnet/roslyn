// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler.EditAndContinue;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;
using Microsoft.CodeAnalysis.LanguageServer.Handler.TestHooks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Testing;
using Microsoft.CodeAnalysis.LanguageServer.Handler.TypeHierarchy;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Core.Imaging;
using Roslyn.Text.Adornments;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Metadata for built-in protocol contracts, including payloads dispatched by custom converters.
/// </summary>
/// <remarks>
/// Types reachable through serializable properties are discovered automatically; only additional roots are listed.
/// Collection roots also cover their element types.
/// </remarks>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[SuppressMessage("ApiDesign", "RS0030", Justification = "Generated metadata preserves existing protocol contracts whose System.Uri usage is allowed by the protocol model's .editorconfig.")]
#pragma warning disable CS0618 // Obsolete contracts remain part of the wire protocol.
[JsonSerializable(typeof(BuildOnlyDiagnosticIdsResult))]
[JsonSerializable(typeof(CallHierarchyResolveData))]
[JsonSerializable(typeof(CodeActionResolveData))]
[JsonSerializable(typeof(RoslynFixAllCodeAction))]
[JsonSerializable(typeof(CodeLensResolveData))]
[JsonSerializable(typeof(CompletionResolveData))]
[JsonSerializable(typeof(DocumentResolveData))]
[JsonSerializable(typeof(LspSolutionSnapshotId))]
[JsonSerializable(typeof(ActivateExtensionParams))]
[JsonSerializable(typeof(ActivateExtensionResponse))]
[JsonSerializable(typeof(DeactivateExtensionParams))]
[JsonSerializable(typeof(DispatchDocumentExtensionMessageParams))]
[JsonSerializable(typeof(DispatchExtensionMessageResponse))]
[JsonSerializable(typeof(DispatchWorkspaceExtensionMessageParams))]
[JsonSerializable(typeof(RefreshSourceGeneratorsParams))]
[JsonSerializable(typeof(InlayHintResolveData))]
[JsonSerializable(typeof(RoslynDocumentSymbolParams))]
[JsonSerializable(typeof(RoslynDocumentSymbol[]))]
[JsonSerializable(typeof(WaitForAsyncOperationsParams))]
[JsonSerializable(typeof(WaitForAsyncOperationsResponse))]
[JsonSerializable(typeof(DebugAttachParams))]
[JsonSerializable(typeof(DebugAttachResult))]
[JsonSerializable(typeof(RunTestsParams))]
[JsonSerializable(typeof(RunTestsPartialResult))]
[JsonSerializable(typeof(WorkDoneProgressManager.ProgressReportType))]
[JsonSerializable(typeof(TypeHierarchyResolveData))]
[JsonSerializable(typeof(ImageId))]
[JsonSerializable(typeof(CommonErrorData))]
[JsonSerializable(typeof(AnnotatedTextEdit))]
[JsonSerializable(typeof(ApplyWorkspaceEditParams))]
[JsonSerializable(typeof(ApplyWorkspaceEditResponse))]
[JsonSerializable(typeof(CallHierarchyIncomingCall[]))]
[JsonSerializable(typeof(CallHierarchyIncomingCallsParams))]
[JsonSerializable(typeof(CallHierarchyItem[]))]
[JsonSerializable(typeof(CallHierarchyOptions))]
[JsonSerializable(typeof(CallHierarchyOutgoingCall[]))]
[JsonSerializable(typeof(CallHierarchyOutgoingCallsParams))]
[JsonSerializable(typeof(CallHierarchyPrepareParams))]
[JsonSerializable(typeof(CallHierarchyRegistrationOptions))]
[JsonSerializable(typeof(CodeActionOptions))]
[JsonSerializable(typeof(CodeActionParams))]
[JsonSerializable(typeof(CodeActionRegistrationOptions))]
[JsonSerializable(typeof(CodeAction[]))]
[JsonSerializable(typeof(CodeLensParams))]
[JsonSerializable(typeof(CodeLensRegistrationOptions))]
[JsonSerializable(typeof(CodeLens[]))]
[JsonSerializable(typeof(ColorInformation[]))]
[JsonSerializable(typeof(ColorPresentationParams))]
[JsonSerializable(typeof(ColorPresentation[]))]
[JsonSerializable(typeof(CompletionList))]
[JsonSerializable(typeof(CompletionParams))]
[JsonSerializable(typeof(CompletionRegistrationOptions))]
[JsonSerializable(typeof(ConfigurationParams))]
[JsonSerializable(typeof(CreateFile))]
[JsonSerializable(typeof(CreateFilesParams))]
[JsonSerializable(typeof(DeclarationOptions))]
[JsonSerializable(typeof(DeclarationParams))]
[JsonSerializable(typeof(DeclarationRegistrationOptions))]
[JsonSerializable(typeof(DefaultBehaviorPrepareRename))]
[JsonSerializable(typeof(DefinitionOptions))]
[JsonSerializable(typeof(DefinitionParams))]
[JsonSerializable(typeof(DefinitionRegistrationOptions))]
[JsonSerializable(typeof(DeleteFile))]
[JsonSerializable(typeof(DeleteFilesParams))]
[JsonSerializable(typeof(DiagnosticOptions))]
[JsonSerializable(typeof(DiagnosticRegistrationOptions))]
[JsonSerializable(typeof(DiagnosticServerCancellationData))]
[JsonSerializable(typeof(DidChangeConfigurationParams))]
[JsonSerializable(typeof(DidChangeNotebookDocumentParams))]
[JsonSerializable(typeof(DidChangeTextDocumentParams))]
[JsonSerializable(typeof(DidChangeWatchedFilesParams))]
[JsonSerializable(typeof(DidChangeWatchedFilesRegistrationOptions))]
[JsonSerializable(typeof(DidChangeWorkspaceFoldersParams))]
[JsonSerializable(typeof(DidCloseNotebookDocumentParams))]
[JsonSerializable(typeof(DidCloseTextDocumentParams))]
[JsonSerializable(typeof(DidOpenNotebookDocumentParams))]
[JsonSerializable(typeof(DidOpenTextDocumentParams))]
[JsonSerializable(typeof(DidSaveNotebookDocumentParams))]
[JsonSerializable(typeof(DidSaveTextDocumentParams))]
[JsonSerializable(typeof(DocumentColorOptions))]
[JsonSerializable(typeof(DocumentColorParams))]
[JsonSerializable(typeof(DocumentColorRegistrationOptions))]
[JsonSerializable(typeof(DocumentDiagnosticParams))]
[JsonSerializable(typeof(DocumentDiagnosticReportPartialResult))]
[JsonSerializable(typeof(DocumentFormattingOptions))]
[JsonSerializable(typeof(DocumentFormattingParams))]
[JsonSerializable(typeof(DocumentFormattingRegistrationOptions))]
[JsonSerializable(typeof(DocumentHighlightOptions))]
[JsonSerializable(typeof(DocumentHighlightParams))]
[JsonSerializable(typeof(DocumentHighlightRegistrationOptions))]
[JsonSerializable(typeof(DocumentHighlight[]))]
[JsonSerializable(typeof(DocumentLinkParams))]
[JsonSerializable(typeof(DocumentLinkRegistrationOptions))]
[JsonSerializable(typeof(DocumentLink[]))]
[JsonSerializable(typeof(DocumentOnTypeFormattingParams))]
[JsonSerializable(typeof(DocumentOnTypeFormattingRegistrationOptions))]
[JsonSerializable(typeof(DocumentRangeFormattingOptions))]
[JsonSerializable(typeof(DocumentRangeFormattingParams))]
[JsonSerializable(typeof(DocumentRangeFormattingRegistrationOptions))]
[JsonSerializable(typeof(DocumentRangesFormattingParams))]
[JsonSerializable(typeof(DocumentSymbolOptions))]
[JsonSerializable(typeof(DocumentSymbolParams))]
[JsonSerializable(typeof(DocumentSymbolRegistrationOptions))]
[JsonSerializable(typeof(ExecuteCommandParams))]
[JsonSerializable(typeof(ExecuteCommandRegistrationOptions))]
[JsonSerializable(typeof(FileDelete))]
[JsonSerializable(typeof(FoldingRangeOptions))]
[JsonSerializable(typeof(FoldingRangeParams))]
[JsonSerializable(typeof(FoldingRangeRegistrationOptions))]
[JsonSerializable(typeof(FoldingRange[]))]
[JsonSerializable(typeof(FullDocumentDiagnosticReport))]
[JsonSerializable(typeof(Hover))]
[JsonSerializable(typeof(HoverOptions))]
[JsonSerializable(typeof(HoverParams))]
[JsonSerializable(typeof(HoverRegistrationOptions))]
[JsonSerializable(typeof(IAnnotatedChange))]
[JsonSerializable(typeof(IDynamicRegistrationSetting))]
[JsonSerializable(typeof(IStaticRegistrationOptions))]
[JsonSerializable(typeof(ITextDocumentParams))]
[JsonSerializable(typeof(ITextDocumentPositionParams))]
[JsonSerializable(typeof(ITextDocumentRegistrationOptions))]
[JsonSerializable(typeof(IWorkDoneProgressOptions))]
[JsonSerializable(typeof(IWorkDoneProgressParams))]
[JsonSerializable(typeof(ImplementationOptions))]
[JsonSerializable(typeof(ImplementationParams))]
[JsonSerializable(typeof(ImplementationRegistrationOptions))]
[JsonSerializable(typeof(InitializeError))]
[JsonSerializable(typeof(InitializeParams))]
[JsonSerializable(typeof(InitializeResult))]
[JsonSerializable(typeof(InitializedParams))]
[JsonSerializable(typeof(InlayHintLabelPart[]))]
[JsonSerializable(typeof(InlayHintOptions))]
[JsonSerializable(typeof(InlayHintParams))]
[JsonSerializable(typeof(InlayHintRegistrationOptions))]
[JsonSerializable(typeof(InlayHint[]))]
[JsonSerializable(typeof(InlineCompletionList))]
[JsonSerializable(typeof(InlineCompletionOptions))]
[JsonSerializable(typeof(InlineCompletionParams))]
[JsonSerializable(typeof(InlineCompletionRegistrationOptions))]
[JsonSerializable(typeof(InlineValueEvaluatableExpression))]
[JsonSerializable(typeof(InlineValueOptions))]
[JsonSerializable(typeof(InlineValueParams))]
[JsonSerializable(typeof(InlineValueRegistrationOptions))]
[JsonSerializable(typeof(InlineValueText))]
[JsonSerializable(typeof(InlineValueVariableLookup))]
[JsonSerializable(typeof(InsertReplaceEdit))]
[JsonSerializable(typeof(InsertReplaceRange))]
[JsonSerializable(typeof(LinkedEditingRangeOptions))]
[JsonSerializable(typeof(LinkedEditingRangeParams))]
[JsonSerializable(typeof(LinkedEditingRangeRegistrationOptions))]
[JsonSerializable(typeof(LinkedEditingRanges))]
[JsonSerializable(typeof(LocationLink[]))]
[JsonSerializable(typeof(LogMessageParams))]
[JsonSerializable(typeof(LogTraceParams))]
[JsonSerializable(typeof(MarkedString))]
[JsonSerializable(typeof(MarkupContent))]
[JsonSerializable(typeof(MonikerOptions))]
[JsonSerializable(typeof(MonikerParams))]
[JsonSerializable(typeof(MonikerRegistrationOptions))]
[JsonSerializable(typeof(Moniker[]))]
[JsonSerializable(typeof(NotebookCellTextDocumentFilter))]
[JsonSerializable(typeof(NotebookDocumentFilter))]
[JsonSerializable(typeof(NotebookDocumentSyncOptions))]
[JsonSerializable(typeof(NotebookDocumentSyncRegistrationOptions))]
[JsonSerializable(typeof(OptimizedVSCompletionList))]
[JsonSerializable(typeof(PrepareRenameParams))]
[JsonSerializable(typeof(PublishDiagnosticParams))]
[JsonSerializable(typeof(ReferenceOptions))]
[JsonSerializable(typeof(ReferenceParams))]
[JsonSerializable(typeof(ReferenceRegistrationOptions))]
[JsonSerializable(typeof(RegistrationParams))]
[JsonSerializable(typeof(RelatedFullDocumentDiagnosticReport))]
[JsonSerializable(typeof(RelatedUnchangedDocumentDiagnosticReport))]
[JsonSerializable(typeof(RelativePattern))]
[JsonSerializable(typeof(RenameFile))]
[JsonSerializable(typeof(RenameFilesParams))]
[JsonSerializable(typeof(RenameOptions))]
[JsonSerializable(typeof(RenameParams))]
[JsonSerializable(typeof(RenameRange))]
[JsonSerializable(typeof(RenameRegistrationOptions))]
[JsonSerializable(typeof(RoslynInitializeResult))]
[JsonSerializable(typeof(SaveOptions))]
[JsonSerializable(typeof(SelectionRangeOptions))]
[JsonSerializable(typeof(SelectionRangeParams))]
[JsonSerializable(typeof(SelectionRangeRegistrationOptions))]
[JsonSerializable(typeof(SelectionRange[]))]
[JsonSerializable(typeof(SemanticTokens))]
[JsonSerializable(typeof(SemanticTokensDelta))]
[JsonSerializable(typeof(SemanticTokensDeltaParams))]
[JsonSerializable(typeof(SemanticTokensDeltaPartialResult))]
[JsonSerializable(typeof(SemanticTokensFullOptions))]
[JsonSerializable(typeof(SemanticTokensFullParams))]
[JsonSerializable(typeof(SemanticTokensOptions))]
[JsonSerializable(typeof(SemanticTokensParams))]
[JsonSerializable(typeof(SemanticTokensPartialResult))]
[JsonSerializable(typeof(SemanticTokensRangeParams))]
[JsonSerializable(typeof(SemanticTokensRegistrationOptions))]
[JsonSerializable(typeof(SemanticTokensRequestsFullSetting))]
[JsonSerializable(typeof(SetTraceParams))]
[JsonSerializable(typeof(ShowDocumentParams))]
[JsonSerializable(typeof(ShowDocumentResult))]
[JsonSerializable(typeof(ShowMessageParams))]
[JsonSerializable(typeof(ShowMessageRequestParams))]
[JsonSerializable(typeof(SignatureHelpParams))]
[JsonSerializable(typeof(SignatureHelpRegistrationOptions))]
[JsonSerializable(typeof(StringValue))]
[JsonSerializable(typeof(SumType<Command, CodeAction>[]))]
[JsonSerializable(typeof(SumType<DocumentSymbol[], SymbolInformation[]>))]
[JsonSerializable(typeof(SumType<Location[], LocationLink[]>))]
[JsonSerializable(typeof(SumType<Location, Location[], DocumentLink[]>))]
[JsonSerializable(typeof(SumType<Location[], VSInternalReferenceItem[]>))]
[JsonSerializable(typeof(SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>))]
[JsonSerializable(typeof(SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport>))]
[JsonSerializable(typeof(SumType<SymbolInformation[], DocumentSymbol[]>))]
[JsonSerializable(typeof(SumType<TextDocumentEdit, CreateFile, RenameFile, DeleteFile>[]))]
[JsonSerializable(typeof(SumType<VSInternalReferenceItem, Location>[]))]
[JsonSerializable(typeof(SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>))]
[JsonSerializable(typeof(SumType<string, MarkedString>[]))]
[JsonSerializable(typeof(SumType<string, Tuple<int, int>>))]
[JsonSerializable(typeof(SymbolInformation[]))]
[JsonSerializable(typeof(TextDocumentChangeRegistrationOptions))]
[JsonSerializable(typeof(TextDocumentContentChangePartial))]
[JsonSerializable(typeof(TextDocumentContentChangeWholeDocument))]
[JsonSerializable(typeof(TextDocumentContentParams))]
[JsonSerializable(typeof(TextDocumentContentRefreshParams))]
[JsonSerializable(typeof(TextDocumentContentRegistrationOptions))]
[JsonSerializable(typeof(TextDocumentContentResult))]
[JsonSerializable(typeof(TextDocumentEdit[]))]
[JsonSerializable(typeof(TextDocumentPositionParams))]
[JsonSerializable(typeof(TextDocumentRegistrationOptions))]
[JsonSerializable(typeof(TextDocumentSaveRegistrationOptions))]
[JsonSerializable(typeof(TypeDefinitionOptions))]
[JsonSerializable(typeof(TypeDefinitionParams))]
[JsonSerializable(typeof(TypeDefinitionRegistrationOptions))]
[JsonSerializable(typeof(TypeHierarchyItem[]))]
[JsonSerializable(typeof(TypeHierarchyOptions))]
[JsonSerializable(typeof(TypeHierarchyPrepareParams))]
[JsonSerializable(typeof(TypeHierarchyRegistrationOptions))]
[JsonSerializable(typeof(TypeHierarchySubtypesParams))]
[JsonSerializable(typeof(TypeHierarchySupertypesParams))]
[JsonSerializable(typeof(UnchangedDocumentDiagnosticReport))]
[JsonSerializable(typeof(UnregistrationParams))]
[JsonSerializable(typeof(VSDiagnostic))]
[JsonSerializable(typeof(VSFoldingRangeSetting))]
[JsonSerializable(typeof(VSGetProjectContextsParams))]
[JsonSerializable(typeof(VSInternalClientCapabilities))]
[JsonSerializable(typeof(VSInternalClipboardContent))]
[JsonSerializable(typeof(VSInternalCodeActionContext))]
[JsonSerializable(typeof(VSInternalCodeActionLiteralSetting))]
[JsonSerializable(typeof(VSInternalCodeAction[]))]
[JsonSerializable(typeof(VSInternalCommitCharacter[]))]
[JsonSerializable(typeof(VSInternalCompletionContext))]
[JsonSerializable(typeof(VSInternalCompletionItem))]
[JsonSerializable(typeof(VSInternalCompletionList))]
[JsonSerializable(typeof(VSInternalCompletionSetting))]
[JsonSerializable(typeof(VSInternalContinueCharacterClass))]
[JsonSerializable(typeof(VSInternalContinueCharacterRange))]
[JsonSerializable(typeof(VSInternalContinueCharacterSingle))]
[JsonSerializable(typeof(VSInternalDataTip))]
[JsonSerializable(typeof(VSInternalDocumentOnAutoInsertParams))]
[JsonSerializable(typeof(VSInternalDocumentOnAutoInsertRegistrationOptions))]
[JsonSerializable(typeof(VSInternalDocumentOnAutoInsertResponseItem))]
[JsonSerializable(typeof(VSInternalDocumentSpellCheckableParams))]
[JsonSerializable(typeof(VSInternalExecuteCommandClientCapabilities))]
[JsonSerializable(typeof(VSInternalHover))]
[JsonSerializable(typeof(VSInternalIconMapping))]
[JsonSerializable(typeof(VSInternalInlineCompletionList))]
[JsonSerializable(typeof(VSInternalInlineCompletionRegistrationOptions))]
[JsonSerializable(typeof(VSInternalInlineCompletionRequest))]
[JsonSerializable(typeof(VSInternalLocation))]
[JsonSerializable(typeof(VSInternalMapCodeParams))]
[JsonSerializable(typeof(VSInternalProjectContext))]
[JsonSerializable(typeof(VSInternalProjectContextWorkspaceSetting))]
[JsonSerializable(typeof(VSInternalReferenceParams))]
[JsonSerializable(typeof(VSInternalRenameParams))]
[JsonSerializable(typeof(VSInternalRenameRange))]
[JsonSerializable(typeof(VSInternalServerCapabilities))]
[JsonSerializable(typeof(VSInternalSignatureInformation))]
[JsonSerializable(typeof(VSInternalSpellCheckableRangeReport[]))]
[JsonSerializable(typeof(VSInternalSymbolInformation))]
[JsonSerializable(typeof(VSInternalTextDocumentClientCapabilities))]
[JsonSerializable(typeof(VSInternalTextDocumentRegistrationOptions))]
[JsonSerializable(typeof(VSInternalTextPresentationParams))]
[JsonSerializable(typeof(VSInternalUriPresentationParams))]
[JsonSerializable(typeof(VSInternalValidateBreakableRangeParams))]
[JsonSerializable(typeof(VSInternalWorkspaceClientCapabilities))]
[JsonSerializable(typeof(VSInternalWorkspaceSpellCheckableParams))]
[JsonSerializable(typeof(VSInternalWorkspaceSpellCheckableReport[]))]
[JsonSerializable(typeof(VSLocation))]
[JsonSerializable(typeof(VSProjectContextList))]
[JsonSerializable(typeof(VSServerCapabilities))]
[JsonSerializable(typeof(VSSymbolInformation))]
[JsonSerializable(typeof(VSTextDocumentIdentifier))]
[JsonSerializable(typeof(WillSaveTextDocumentParams))]
[JsonSerializable(typeof(WorkDoneProgressBegin))]
[JsonSerializable(typeof(WorkDoneProgressCancelParams))]
[JsonSerializable(typeof(WorkDoneProgressCreateParams))]
[JsonSerializable(typeof(WorkDoneProgressEnd))]
[JsonSerializable(typeof(WorkDoneProgressReport))]
[JsonSerializable(typeof(WorkspaceDiagnosticParams))]
[JsonSerializable(typeof(WorkspaceDiagnosticReport))]
[JsonSerializable(typeof(WorkspaceDiagnosticReportPartialResult))]
[JsonSerializable(typeof(WorkspaceFullDocumentDiagnosticReport))]
[JsonSerializable(typeof(WorkspaceSymbolLocation))]
[JsonSerializable(typeof(WorkspaceSymbolOptions))]
[JsonSerializable(typeof(WorkspaceSymbolParams))]
[JsonSerializable(typeof(WorkspaceSymbolRegistrationOptions))]
[JsonSerializable(typeof(WorkspaceSymbol[]))]
[JsonSerializable(typeof(WorkspaceUnchangedDocumentDiagnosticReport))]
[JsonSerializable(typeof(ClassifiedTextRun))]
[JsonSerializable(typeof(ContainerElement))]
[JsonSerializable(typeof(IEnumerable<TextEdit>))]
[JsonSerializable(typeof(IEnumerable<SumType<TextEdit, AnnotatedTextEdit>>))]
[JsonSerializable(typeof(List<object>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(long?))]
[JsonSerializable(typeof(SumType<CompletionItem[], CompletionList>?))]
[JsonSerializable(typeof(SumType<SymbolInformation[], WorkspaceSymbol[]>?))]
[JsonSerializable(typeof(SumType<DocumentSymbol[], SymbolInformation[]>?))]
[JsonSerializable(typeof(IOrderedEnumerable<Location>))]
[JsonSerializable(typeof(VSInternalReferenceItem[]))]
[JsonSerializable(typeof(JsonDocument))]
[JsonSerializable(typeof(JsonElement?))]
[JsonSerializable(typeof(JsonArray))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(JsonObject))]
[JsonSerializable(typeof(JsonValue))]
[JsonSerializable(typeof(Tuple<int, int>))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(Dictionary<string, WorkDoneProgress>))]
#pragma warning restore CS0618
internal sealed partial class LspJsonSerializerContext : JsonSerializerContext
{
    internal static IJsonTypeInfoResolver Resolver { get; } = new ProtocolTypeInfoResolver();

    private sealed class ProtocolTypeInfoResolver : IJsonTypeInfoResolver
    {
        private static readonly RequestIdConverter s_requestIdConverter = new();

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            // StreamJsonRpc's RequestId converter is internal, so the source generator cannot instantiate it.
            if (type == typeof(RequestId))
                return JsonMetadataServices.CreateValueInfo<RequestId>(options, s_requestIdConverter);

            return ((IJsonTypeInfoResolver)Default).GetTypeInfo(type, options);
        }
    }

    private sealed class RequestIdConverter : JsonConverter<RequestId>
    {
        public override RequestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType switch
            {
                JsonTokenType.Null => RequestId.Null,
                JsonTokenType.String => new RequestId(reader.GetString()),
                JsonTokenType.Number => new RequestId(reader.GetInt64()),
                _ => throw new JsonException("Unexpected token type for id property: " + reader.TokenType),
            };

        public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options)
        {
            if (value.Number is long number)
                writer.WriteNumberValue(number);
            else if (value.String is string text)
                writer.WriteStringValue(text);
            else
                writer.WriteNullValue();
        }
    }

    internal static void RegisterGenericTypes(SystemTextJsonFormatter formatter)
    {
#pragma warning disable CS0618 // Obsolete contracts remain part of the wire protocol.
        formatter.RegisterGenericType<WorkDoneProgress>();
        formatter.RegisterGenericType<RunTestsPartialResult>();
        formatter.RegisterGenericType<CallHierarchyIncomingCall[]>();
        formatter.RegisterGenericType<CallHierarchyOutgoingCall[]>();
        formatter.RegisterGenericType<CodeLens[]>();
        formatter.RegisterGenericType<ColorInformation[]>();
        formatter.RegisterGenericType<ColorPresentation[]>();
        formatter.RegisterGenericType<DocumentHighlight[]>();
        formatter.RegisterGenericType<DocumentLink[]>();
        formatter.RegisterGenericType<FoldingRange[]>();
        formatter.RegisterGenericType<Location[]>();
        formatter.RegisterGenericType<Moniker[]>();
        formatter.RegisterGenericType<SelectionRange[]>();
        formatter.RegisterGenericType<SemanticTokensDeltaPartialResult>();
        formatter.RegisterGenericType<SemanticTokensPartialResult>();
        formatter.RegisterGenericType<SumType<Command, CodeAction>[]>();
        formatter.RegisterGenericType<SumType<Location[], LocationLink[]>>();
        formatter.RegisterGenericType<SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>>();
        formatter.RegisterGenericType<SumType<SymbolInformation[], DocumentSymbol[]>>();
        formatter.RegisterGenericType<SumType<SymbolInformation[], WorkspaceSymbol[]>>();
        formatter.RegisterGenericType<SumType<VSInternalReferenceItem, Location>[]>();
        formatter.RegisterGenericType<SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>>();
        formatter.RegisterGenericType<TypeHierarchyItem[]>();
        formatter.RegisterGenericType<VSInternalSpellCheckableRangeReport[]>();
        formatter.RegisterGenericType<VSInternalWorkspaceSpellCheckableReport[]>();
        formatter.RegisterGenericType<SumType<CompletionItem[], CompletionList>?>();
#pragma warning restore CS0618
    }

    internal static void AddProgressConverters(JsonSerializerOptions options, JsonRpc jsonRpc)
    {
        for (var i = options.Converters.Count - 1; i >= 0; i--)
        {
            if (options.Converters[i].CanConvert(typeof(IProgress<WorkDoneProgress>)))
                options.Converters.RemoveAt(i);
        }

#pragma warning disable CS0618 // Obsolete contracts remain part of the wire protocol.
        options.Converters.Insert(0, new LspProgressConverter<WorkDoneProgress>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<RunTestsPartialResult>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<CallHierarchyIncomingCall[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<CallHierarchyOutgoingCall[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<CodeLens[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<ColorInformation[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<ColorPresentation[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<DocumentHighlight[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<DocumentLink[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<FoldingRange[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<Location[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<Moniker[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SelectionRange[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SemanticTokensDeltaPartialResult>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SemanticTokensPartialResult>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<Command, CodeAction>[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<Location[], LocationLink[]>>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<SymbolInformation[], DocumentSymbol[]>>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<SymbolInformation[], WorkspaceSymbol[]>>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<VSInternalReferenceItem, Location>[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<WorkspaceDiagnosticReport, WorkspaceDiagnosticReportPartialResult>>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<TypeHierarchyItem[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<VSInternalSpellCheckableRangeReport[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<VSInternalWorkspaceSpellCheckableReport[]>(jsonRpc));
        options.Converters.Insert(0, new LspProgressConverter<SumType<CompletionItem[], CompletionList>?>(jsonRpc));
#pragma warning restore CS0618
    }
}
