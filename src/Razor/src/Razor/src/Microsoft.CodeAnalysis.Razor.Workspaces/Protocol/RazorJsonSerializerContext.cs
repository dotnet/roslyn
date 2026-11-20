// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.CallHierarchy;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol.AutoInsert;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol.DevTools;
using Microsoft.CodeAnalysis.Razor.Protocol.DocumentHighlight;
using Microsoft.CodeAnalysis.Razor.Protocol.DocumentPresentation;
using Microsoft.CodeAnalysis.Razor.Protocol.Folding;
using Microsoft.CodeAnalysis.Razor.Protocol.InlayHints;
using Microsoft.CodeAnalysis.Razor.Protocol.NestedFiles;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.TypeHierarchy;
using Microsoft.CodeAnalysis.Razor.Workspaces.CodeLens;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

/// <summary>
/// Metadata for Razor LSP and JSON brokered-service contracts, including object-valued protocol payloads.
/// Serializable properties supply the rest of each contract's type graph.
/// </summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(VSCodeActionParams))]
[JsonSerializable(typeof(RazorVSInternalCodeAction[]))]
[JsonSerializable(typeof(RazorCodeActionResolutionParams))]
[JsonSerializable(typeof(RazorVSInternalCompletionParams))]
[JsonSerializable(typeof(RazorVSInternalCompletionList))]
[JsonSerializable(typeof(CompletionResult))]
[JsonSerializable(typeof(CompletionPositionInfo?))]
[JsonSerializable(typeof(RazorCompletionResolveData))]
[JsonSerializable(typeof(CompletionListMerger.MergedCompletionListData))]
[JsonSerializable(typeof(DelegatedCompletionResolutionContext))]
[JsonSerializable(typeof(SnippetCompletionResolutionContext))]
[JsonSerializable(typeof(RazorCallHierarchyResolveData))]
[JsonSerializable(typeof(RazorTypeHierarchyResolveData))]
[JsonSerializable(typeof(RazorCodeLensResolveData))]
[JsonSerializable(typeof(InlayHintDataWrapper))]
[JsonSerializable(typeof(RazorTextPresentationParams))]
[JsonSerializable(typeof(RazorUriPresentationParams))]
[JsonSerializable(typeof(TextPresentationParams))]
[JsonSerializable(typeof(UriPresentationParams))]
[JsonSerializable(typeof(AddNestedFileParams))]
[JsonSerializable(typeof(DocumentContentsRequest))]
[JsonSerializable(typeof(SyntaxVisualizerTree))]
[JsonSerializable(typeof(TagHelpersKind))]
[JsonSerializable(typeof(RemoteAutoInsertTextEdit?))]
[JsonSerializable(typeof(RemoteDocumentHighlight[]))]
[JsonSerializable(typeof(ImmutableArray<RemoteFoldingRange>))]
[JsonSerializable(typeof(GoToDefinitionResponse))]
[JsonSerializable(typeof(RazorFormattingOptions))]
[JsonSerializable(typeof(RazorFileKind))]
[JsonSerializable(typeof(RazorTextChange[]))]
[JsonSerializable(typeof(ImmutableArray<RazorTextChange>))]
[JsonSerializable(typeof(LinePositionSpan?))]
[JsonSerializable(typeof(ClientSettings))]
[JsonSerializable(typeof(RemoteClientLSPInitializationOptions))]
[JsonSerializable(typeof(JsonSerializableDocumentId))]
[JsonSerializable(typeof(JsonSerializableRazorSolutionWrapper))]
[JsonSerializable(typeof(RazorCompletionOptions))]
[JsonSerializable(typeof(CodeActionRequestInfo))]
[JsonSerializable(typeof(ImmutableArray<LSP.Diagnostic>))]
[JsonSerializable(typeof(RemoteResponse<CompletionResult>))]
[JsonSerializable(typeof(RemoteResponse<GoToDefinitionResponse>))]
[JsonSerializable(typeof(RemoteResponse<LSP.CallHierarchyIncomingCall[]>))]
[JsonSerializable(typeof(RemoteResponse<LSP.CallHierarchyItem[]>))]
[JsonSerializable(typeof(RemoteResponse<LSP.CallHierarchyOutgoingCall[]>))]
[JsonSerializable(typeof(RemoteResponse<LSP.Hover>))]
[JsonSerializable(typeof(RemoteResponse<LSP.Location[]>))]
[JsonSerializable(typeof(RemoteResponse<LSP.Range>))]
[JsonSerializable(typeof(RemoteResponse<LSP.SumType<LSP.VSInternalReferenceItem, LSP.Location>[]>))]
[JsonSerializable(typeof(RemoteResponse<LSP.TypeHierarchyItem[]>))]
[JsonSerializable(typeof(RemoteResponse<LSP.VSInternalDataTip>))]
[JsonSerializable(typeof(RemoteResponse<LSP.WorkspaceEdit>))]
[JsonSerializable(typeof(SumType<LspLocation, LspLocation[], DocumentLink[]>?))]
[JsonSerializable(typeof(SumType<LspLocation[], VSInternalReferenceItem[]>?))]
internal sealed partial class RazorJsonSerializerContext : JsonSerializerContext;
