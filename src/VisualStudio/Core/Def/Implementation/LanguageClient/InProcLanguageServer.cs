﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Utilities.Internal;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    /// <summary>
    /// Defines the language server to be hooked up to an <see cref="ILanguageClient"/> using StreamJsonRpc.  This runs
    /// in proc as not all features provided by this server are available out of proc (e.g. some diagnostics).
    /// </summary>
    internal class InProcLanguageServer
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly IAsynchronousOperationListener _listener;
        private readonly string? _clientName;
        private readonly JsonRpc _jsonRpc;
        private readonly AbstractRequestHandlerProvider _requestHandlerProvider;
        private readonly Workspace _workspace;
        private readonly RequestExecutionQueue _queue;

        private VSClientCapabilities _clientCapabilities;
        private bool _shuttingDown;

        public InProcLanguageServer(Stream inputStream,
            Stream outputStream,
            AbstractRequestHandlerProvider requestHandlerProvider,
            Workspace workspace,
            IDiagnosticService diagnosticService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspSolutionProvider solutionProvider,
            string? clientName)
        {
            _requestHandlerProvider = requestHandlerProvider;
            _workspace = workspace;

            var jsonMessageFormatter = new JsonMessageFormatter();
            jsonMessageFormatter.JsonSerializer.Converters.Add(new VSExtensionConverter<TextDocumentIdentifier, VSTextDocumentIdentifier>());
            jsonMessageFormatter.JsonSerializer.Converters.Add(new VSExtensionConverter<ClientCapabilities, VSClientCapabilities>());

            _jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, jsonMessageFormatter));
            _jsonRpc.AddLocalRpcTarget(this);
            _jsonRpc.StartListening();

            _diagnosticService = diagnosticService;
            _listener = listenerProvider.GetListener(FeatureAttribute.LanguageServer);
            _clientName = clientName;
            _diagnosticService.DiagnosticsUpdated += DiagnosticService_DiagnosticsUpdated;

            _clientCapabilities = new VSClientCapabilities();

            _queue = new RequestExecutionQueue(solutionProvider);
            _queue.RequestServerShutdown += RequestExecutionQueue_Errored;
        }

        public bool Running => !_shuttingDown && !_jsonRpc.IsDisposed;

        /// <summary>
        /// Handle the LSP initialize request by storing the client capabilities
        /// and responding with the server capabilities.
        /// The specification assures that the initialize request is sent only once.
        /// </summary>
        [JsonRpcMethod(Methods.InitializeName, UseSingleObjectParameterDeserialization = true)]
        public async Task<InitializeResult> InitializeAsync(InitializeParams initializeParams, CancellationToken cancellationToken)
        {
            _clientCapabilities = (VSClientCapabilities)initializeParams.Capabilities;

            var serverCapabilities = await _requestHandlerProvider.ExecuteRequestAsync<InitializeParams, InitializeResult>(_queue, Methods.InitializeName,
                initializeParams, _clientCapabilities, _clientName, cancellationToken).ConfigureAwait(false);

            // Always support hover - if any LSP client for a content type advertises support,
            // then the liveshare provider is disabled.  So we must provide for both C# and razor
            // until https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1106064/ is fixed
            // or we have different content types.
            serverCapabilities.Capabilities.HoverProvider = true;
            return serverCapabilities;
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public async Task InitializedAsync()
        {
            // Publish diagnostics for all open documents immediately following initialization.
            var solution = _workspace.CurrentSolution;
            var openDocuments = _workspace.GetOpenDocumentIds();
            foreach (var documentId in openDocuments)
            {
                var document = solution.GetDocument(documentId);
                if (document != null)
                {
                    await PublishDiagnosticsAsync(document).ConfigureAwait(false);
                }
            }
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public Task ShutdownAsync(CancellationToken _)
        {
            Contract.ThrowIfTrue(_shuttingDown, "Shutdown has already been called.");

            _shuttingDown = true;
            _diagnosticService.DiagnosticsUpdated -= DiagnosticService_DiagnosticsUpdated;

            ShutdownRequestQueue();

            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ExitName)]
        public Task ExitAsync(CancellationToken _)
        {
            Contract.ThrowIfFalse(_shuttingDown, "Shutdown has not been called yet.");

            try
            {
                _jsonRpc.Dispose();
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
                // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
            }

            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.TextDocumentDefinitionName, UseSingleObjectParameterDeserialization = true)]
        public Task<LSP.Location[]> GetTextDocumentDefinitionAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(_queue, Methods.TextDocumentDefinitionName,
                textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentRenameName, UseSingleObjectParameterDeserialization = true)]
        public Task<WorkspaceEdit> GetTextDocumentRenameAsync(RenameParams renameParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<RenameParams, WorkspaceEdit>(_queue, Methods.TextDocumentRenameName,
                renameParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentReferencesName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSReferenceItem[]> GetTextDocumentReferencesAsync(ReferenceParams referencesParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<ReferenceParams, VSReferenceItem[]>(_queue, Methods.TextDocumentReferencesName,
                referencesParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentCodeActionName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSCodeAction[]> GetTextDocumentCodeActionsAsync(CodeActionParams codeActionParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<CodeActionParams, VSCodeAction[]>(_queue, Methods.TextDocumentCodeActionName,
                codeActionParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(MSLSPMethods.TextDocumentCodeActionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSCodeAction> ResolveCodeActionAsync(VSCodeAction vsCodeAction, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<VSCodeAction, VSCodeAction>(_queue, MSLSPMethods.TextDocumentCodeActionResolveName,
                vsCodeAction, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentCompletionName, UseSingleObjectParameterDeserialization = true)]
        public async Task<SumType<CompletionList, CompletionItem[]>> GetTextDocumentCompletionAsync(CompletionParams completionParams, CancellationToken cancellationToken)
            // Convert to sumtype before reporting to work around https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1107698
            => await _requestHandlerProvider.ExecuteRequestAsync<CompletionParams, CompletionList>(_queue, Methods.TextDocumentCompletionName,
                completionParams, _clientCapabilities, _clientName, cancellationToken).ConfigureAwait(false);

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<CompletionItem> ResolveCompletionItemAsync(CompletionItem completionItem, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<CompletionItem, CompletionItem>(_queue, Methods.TextDocumentCompletionResolveName,
                completionItem, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentFoldingRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<FoldingRange[]> GetTextDocumentFoldingRangeAsync(FoldingRangeParams textDocumentFoldingRangeParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<FoldingRangeParams, FoldingRange[]>(_queue, Methods.TextDocumentFoldingRangeName,
                textDocumentFoldingRangeParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentHighlight[]> GetTextDocumentDocumentHighlightsAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, DocumentHighlight[]>(_queue, Methods.TextDocumentDocumentHighlightName,
                textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentHoverName, UseSingleObjectParameterDeserialization = true)]
        public Task<Hover?> GetTextDocumentDocumentHoverAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, Hover?>(_queue, Methods.TextDocumentHoverName,
                textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentDocumentSymbolName, UseSingleObjectParameterDeserialization = true)]
        public Task<object[]> GetTextDocumentDocumentSymbolsAsync(DocumentSymbolParams documentSymbolParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<DocumentSymbolParams, object[]>(_queue, Methods.TextDocumentDocumentSymbolName,
                documentSymbolParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentFormattingAsync(DocumentFormattingParams documentFormattingParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<DocumentFormattingParams, TextEdit[]>(_queue, Methods.TextDocumentFormattingName,
                documentFormattingParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentFormattingOnTypeAsync(DocumentOnTypeFormattingParams documentOnTypeFormattingParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<DocumentOnTypeFormattingParams, TextEdit[]>(_queue, Methods.TextDocumentOnTypeFormattingName,
                documentOnTypeFormattingParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentImplementationName, UseSingleObjectParameterDeserialization = true)]
        public Task<LSP.Location[]> GetTextDocumentImplementationsAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(_queue, Methods.TextDocumentImplementationName,
                textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentRangeFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> GetTextDocumentRangeFormattingAsync(DocumentRangeFormattingParams documentRangeFormattingParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<DocumentRangeFormattingParams, TextEdit[]>(_queue, Methods.TextDocumentRangeFormattingName,
                documentRangeFormattingParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName, UseSingleObjectParameterDeserialization = true)]
        public Task<SignatureHelp> GetTextDocumentSignatureHelpAsync(TextDocumentPositionParams textDocumentPositionParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<TextDocumentPositionParams, SignatureHelp>(_queue, Methods.TextDocumentSignatureHelpName,
                textDocumentPositionParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.WorkspaceExecuteCommandName, UseSingleObjectParameterDeserialization = true)]
        public Task<object> ExecuteWorkspaceCommandAsync(ExecuteCommandParams executeCommandParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<ExecuteCommandParams, object>(_queue, Methods.WorkspaceExecuteCommandName,
                executeCommandParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(Methods.WorkspaceSymbolName, UseSingleObjectParameterDeserialization = true)]
        public Task<SymbolInformation[]> GetWorkspaceSymbolsAsync(WorkspaceSymbolParams workspaceSymbolParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<WorkspaceSymbolParams, SymbolInformation[]>(_queue, Methods.WorkspaceSymbolName,
                workspaceSymbolParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(MSLSPMethods.ProjectContextsName, UseSingleObjectParameterDeserialization = true)]
        public Task<ActiveProjectContexts?> GetProjectContextsAsync(GetTextDocumentWithContextParams textDocumentWithContextParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<GetTextDocumentWithContextParams, ActiveProjectContexts?>(_queue, MSLSPMethods.ProjectContextsName,
                textDocumentWithContextParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensAsync(SemanticTokensParams semanticTokensParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<SemanticTokensParams, SemanticTokens>(_queue, SemanticTokensMethods.TextDocumentSemanticTokensName,
                semanticTokensParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensEditsName, UseSingleObjectParameterDeserialization = true)]
        public Task<SumType<SemanticTokens, SemanticTokensEdits>> GetTextDocumentSemanticTokensEditsAsync(SemanticTokensEditsParams semanticTokensEditsParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<SemanticTokensEditsParams, SumType<SemanticTokens, SemanticTokensEdits>>(_queue, SemanticTokensMethods.TextDocumentSemanticTokensEditsName,
                semanticTokensEditsParams, _clientCapabilities, _clientName, cancellationToken);

        // Note: Since a range request is always received in conjunction with a whole document request, we don't need to cache range results.
        [JsonRpcMethod(SemanticTokensMethods.TextDocumentSemanticTokensRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<SemanticTokens> GetTextDocumentSemanticTokensRangeAsync(SemanticTokensRangeParams semanticTokensRangeParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<SemanticTokensRangeParams, SemanticTokens>(_queue, SemanticTokensMethods.TextDocumentSemanticTokensRangeName,
                semanticTokensRangeParams, _clientCapabilities, _clientName, cancellationToken);

        [JsonRpcMethod(MSLSPMethods.OnAutoInsertName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentOnAutoInsertResponseItem[]> GetDocumentOnAutoInsertAsync(DocumentOnAutoInsertParams autoInsertParams, CancellationToken cancellationToken)
            => _requestHandlerProvider.ExecuteRequestAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem[]>(_queue, MSLSPMethods.OnAutoInsertName,
                autoInsertParams, _clientCapabilities, _clientName, cancellationToken);

        private void DiagnosticService_DiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
        {
            // LSP doesn't support diagnostics without a document. So if we get project level diagnostics without a document, ignore them.
            if (e.DocumentId != null && e.Solution != null)
            {
                var document = e.Solution.GetDocument(e.DocumentId);
                if (document == null || document.FilePath == null)
                {
                    return;
                }

                // Only publish document diagnostics for the languages this provider supports.
                if (document.Project.Language != CodeAnalysis.LanguageNames.CSharp && document.Project.Language != CodeAnalysis.LanguageNames.VisualBasic)
                {
                    return;
                }

                // LSP does not currently support publishing diagnostics incrementally, so we re-publish all diagnostics.
                var asyncToken = _listener.BeginAsyncOperation(nameof(PublishDiagnosticsAsync));
                Task.Run(() => PublishDiagnosticsAsync(document))
                    .CompletesAsyncOperation(asyncToken);
            }
        }

        private void ShutdownRequestQueue()
        {
            _queue.RequestServerShutdown -= RequestExecutionQueue_Errored;
            // if the queue requested shutdown via its event, it will have already shut itself down, but this
            // won't cause any problems calling it again
            _queue.Shutdown();
        }

        private void RequestExecutionQueue_Errored(object sender, RequestShutdownEventArgs e)
        {
            // log message and shut down

            var message = new LogMessageParams()
            {
                MessageType = MessageType.Error,
                Message = e.Message
            };

            var asyncToken = _listener.BeginAsyncOperation(nameof(RequestExecutionQueue_Errored));
            Task.Run(async () =>
            {
                await _jsonRpc.NotifyWithParameterObjectAsync(Methods.WindowLogMessageName, message).ConfigureAwait(false);

                // The "default" here is the cancellation token, which these methods don't use, hence the discard name
                await ShutdownAsync(_: default).ConfigureAwait(false);
                await ExitAsync(_: default).ConfigureAwait(false);
            }).CompletesAsyncOperation(asyncToken);
        }

        /// <summary>
        /// Stores the last published LSP diagnostics with the Roslyn document that they came from.
        /// This is useful in the following scenario.  Imagine we have documentA which has contributions to mapped files m1 and m2.
        /// dA -> m1
        /// And m1 has contributions from documentB.
        /// m1 -> dA, dB
        /// When we query for diagnostic on dA, we get a subset of the diagnostics on m1 (missing the contributions from dB)
        /// Since each publish diagnostics notification replaces diagnostics per document,
        /// we must union the diagnostics contribution from dB and dA to produce all diagnostics for m1 and publish all at once.
        ///
        /// This dictionary stores the previously computed diagnostics for the published file so that we can
        /// union the currently computed diagnostics (e.g. for dA) with previously computed diagnostics (e.g. from dB).
        /// </summary>
        private readonly Dictionary<Uri, Dictionary<DocumentId, ImmutableArray<LanguageServer.Protocol.Diagnostic>>> _publishedFileToDiagnostics =
            new();

        /// <summary>
        /// Stores the mapping of a document to the uri(s) of diagnostics previously produced for this document.  When
        /// we get empty diagnostics for the document we need to find the uris we previously published for this
        /// document. Then we can publish the updated diagnostics set for those uris (either empty or the diagnostic
        /// contributions from other documents).  We use a sorted set to ensure consistency in the order in which we
        /// report URIs.  While it's not necessary to publish a document's mapped file diagnostics in a particular
        /// order, it does make it much easier to write tests and debug issues if we have a consistent ordering.
        /// </summary>
        private readonly Dictionary<DocumentId, ImmutableSortedSet<Uri>> _documentsToPublishedUris = new();

        /// <summary>
        /// Basic comparer for Uris used by <see cref="_documentsToPublishedUris"/> when publishing notifications.
        /// </summary>
        private static readonly Comparer<Uri> s_uriComparer = Comparer<Uri>.Create((uri1, uri2)
            => Uri.Compare(uri1, uri2, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase));

        internal async Task PublishDiagnosticsAsync(Document document)
        {
            // Retrieve all diagnostics for the current document grouped by their actual file uri.
            var fileUriToDiagnostics = await GetDiagnosticsAsync(document, CancellationToken.None).ConfigureAwait(false);

            // Get the list of file uris with diagnostics (for the document).
            // We need to join the uris from current diagnostics with those previously published
            // so that we clear out any diagnostics in mapped files that are no longer a part
            // of the current diagnostics set (because the diagnostics were fixed).
            // Use sorted set to have consistent publish ordering for tests and debugging.
            var urisForCurrentDocument = _documentsToPublishedUris.GetValueOrDefault(document.Id, ImmutableSortedSet.Create<Uri>(s_uriComparer)).Union(fileUriToDiagnostics.Keys);

            // Update the mapping for this document to be the uris we're about to publish diagnostics for.
            _documentsToPublishedUris[document.Id] = urisForCurrentDocument;

            // Go through each uri and publish the updated set of diagnostics per uri.
            foreach (var fileUri in urisForCurrentDocument)
            {
                // Get the updated diagnostics for a single uri that were contributed by the current document.
                var diagnostics = fileUriToDiagnostics.GetValueOrDefault(fileUri, ImmutableArray<LSP.Diagnostic>.Empty);

                if (_publishedFileToDiagnostics.ContainsKey(fileUri))
                {
                    // Get all previously published diagnostics for this uri excluding those that were contributed from the current document.
                    // We don't need those since we just computed the updated values above.
                    var diagnosticsFromOtherDocuments = _publishedFileToDiagnostics[fileUri].Where(kvp => kvp.Key != document.Id).SelectMany(kvp => kvp.Value);

                    // Since diagnostics are replaced per uri, we must publish both contributions from this document and any other document
                    // that has diagnostic contributions to this uri, so union the two sets.
                    diagnostics = diagnostics.AddRange(diagnosticsFromOtherDocuments);
                }

                await SendDiagnosticsNotificationAsync(fileUri, diagnostics).ConfigureAwait(false);

                // There are three cases here ->
                // 1.  There are no diagnostics to publish for this fileUri.  We no longer need to track the fileUri at all.
                // 2.  There are diagnostics from the current document.  Store the diagnostics for the fileUri and document
                //      so they can be published along with contributions to the fileUri from other documents.
                // 3.  There are no diagnostics contributed by this document to the fileUri (could be some from other documents).
                //     We should clear out the diagnostics for this document for the fileUri.
                if (diagnostics.IsEmpty)
                {
                    // We published an empty set of diagnostics for this uri.  We no longer need to keep track of this mapping
                    // since there will be no previous diagnostics that we need to clear out.
                    _documentsToPublishedUris.MultiRemove(document.Id, fileUri);

                    // There are not any diagnostics to keep track of for this file, so we can stop.
                    _publishedFileToDiagnostics.Remove(fileUri);
                }
                else if (fileUriToDiagnostics.ContainsKey(fileUri))
                {
                    // We do have diagnostics from the current document - update the published diagnostics map
                    // to contain the new diagnostics contributed by this document for this uri.
                    var documentsToPublishedDiagnostics = _publishedFileToDiagnostics.GetOrAdd(fileUri, (_) =>
                        new Dictionary<DocumentId, ImmutableArray<LSP.Diagnostic>>());
                    documentsToPublishedDiagnostics[document.Id] = fileUriToDiagnostics[fileUri];
                }
                else
                {
                    // There were diagnostics from other documents, but none from the current document.
                    // If we're tracking the current document, we can stop.
                    _publishedFileToDiagnostics.GetOrDefault(fileUri)?.Remove(document.Id);
                    _documentsToPublishedUris.MultiRemove(document.Id, fileUri);
                }
            }
        }

        private async Task SendDiagnosticsNotificationAsync(Uri uri, ImmutableArray<LSP.Diagnostic> diagnostics)
        {
            var publishDiagnosticsParams = new PublishDiagnosticParams { Diagnostics = diagnostics.ToArray(), Uri = uri };
            await _jsonRpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, publishDiagnosticsParams).ConfigureAwait(false);
        }

        private async Task<Dictionary<Uri, ImmutableArray<LSP.Diagnostic>>> GetDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var diagnostics = _diagnosticService.GetDiagnostics(document.Project.Solution.Workspace, document.Project.Id, document.Id, null, false, cancellationToken)
                                                .Where(IncludeDiagnostic);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Retrieve diagnostics for the document.  These diagnostics could be for the current document, or they could map
            // to a different location in a different file.  We need to publish the diagnostics for the mapped locations as well.
            // An example of this is razor imports where the generated C# document maps to many razor documents.
            // https://docs.microsoft.com/en-us/aspnet/core/mvc/views/layout?view=aspnetcore-3.1#importing-shared-directives
            // https://docs.microsoft.com/en-us/aspnet/core/blazor/layouts?view=aspnetcore-3.1#centralized-layout-selection
            // So we get the diagnostics and group them by the actual mapped path so we can publish notifications
            // for each mapped file's diagnostics.
            var fileUriToDiagnostics = diagnostics.GroupBy(diagnostic => GetDiagnosticUri(document, diagnostic)).ToDictionary(
                group => group.Key,
                group => group.Select(diagnostic => ConvertToLspDiagnostic(diagnostic, text)).ToImmutableArray());
            return fileUriToDiagnostics;

            static Uri GetDiagnosticUri(Document document, DiagnosticData diagnosticData)
            {
                Contract.ThrowIfNull(diagnosticData.DataLocation, "Diagnostic data location should not be null here");

                var filePath = diagnosticData.DataLocation.MappedFilePath ?? diagnosticData.DataLocation.OriginalFilePath;
                return ProtocolConversions.GetUriFromFilePath(filePath);
            }

            static LSP.Diagnostic ConvertToLspDiagnostic(DiagnosticData diagnosticData, SourceText text)
            {
                Contract.ThrowIfNull(diagnosticData.DataLocation);

                var diagnostic = new LSP.Diagnostic
                {
                    Code = diagnosticData.Id,
                    Severity = ProtocolConversions.DiagnosticSeverityToLspDiagnositcSeverity(diagnosticData.Severity),
                    Range = GetDiagnosticRange(diagnosticData.DataLocation, text),
                    // Only the unnecessary diagnostic tag is currently supported via LSP.
                    Tags = diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary)
                        ? new DiagnosticTag[] { DiagnosticTag.Unnecessary }
                        : Array.Empty<DiagnosticTag>()
                };

                if (diagnosticData.Message != null)
                    diagnostic.Message = diagnosticData.Message;

                return diagnostic;
            }
        }

        // Some diagnostics only apply to certain clients and document types, e.g. Razor.
        // If the DocumentPropertiesService.DiagnosticsLspClientName property exists, we only include the
        // diagnostic if it directly matches the client name.
        // If the DocumentPropertiesService.DiagnosticsLspClientName property doesn't exist,
        // we know that the diagnostic we're working with is contained in a C#/VB file, since
        // if we were working with a non-C#/VB file, then the property should have been populated.
        // In this case, unless we have a null client name, we don't want to publish the diagnostic
        // (since a null client name represents the C#/VB language server).
        private bool IncludeDiagnostic(DiagnosticData diagnostic) =>
            diagnostic.Properties.GetOrDefault(nameof(DocumentPropertiesService.DiagnosticsLspClientName)) == _clientName;

        private static LSP.Range GetDiagnosticRange(DiagnosticDataLocation diagnosticDataLocation, SourceText text)
        {
            var linePositionSpan = DiagnosticData.GetLinePositionSpan(diagnosticDataLocation, text, useMapped: true);
            return ProtocolConversions.LinePositionToRange(linePositionSpan);
        }

        internal TestAccessor GetTestAccessor() => new(this);

        internal readonly struct TestAccessor
        {
            private readonly InProcLanguageServer _server;

            internal TestAccessor(InProcLanguageServer server)
            {
                _server = server;
            }

            internal ImmutableArray<Uri> GetFileUrisInPublishDiagnostics()
                => _server._publishedFileToDiagnostics.Keys.ToImmutableArray();

            internal ImmutableArray<DocumentId> GetDocumentIdsInPublishedUris()
                => _server._documentsToPublishedUris.Keys.ToImmutableArray();

            internal IImmutableSet<Uri> GetFileUrisForDocument(DocumentId documentId)
                => _server._documentsToPublishedUris.GetValueOrDefault(documentId, ImmutableSortedSet<Uri>.Empty);

            internal ImmutableArray<LSP.Diagnostic> GetDiagnosticsForUriAndDocument(DocumentId documentId, Uri uri)
            {
                if (_server._publishedFileToDiagnostics.TryGetValue(uri, out var dict) && dict.TryGetValue(documentId, out var diagnostics))
                {
                    return diagnostics;
                }

                return ImmutableArray<LSP.Diagnostic>.Empty;
            }
        }
    }
}
