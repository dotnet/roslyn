// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
{
    /// <summary>
    /// Implementation of <see cref="LanguageServerTarget"/> that also supports
    /// VS LSP extension methods.
    /// </summary>
    internal class VisualStudioInProcLanguageServer : LanguageServerTarget
    {
        /// <summary>
        /// Legacy support for LSP push diagnostics.
        /// Work queue responsible for receiving notifications about diagnostic updates and publishing those to
        /// interested parties.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<DocumentId> _diagnosticsWorkQueue;
        private readonly IDiagnosticService? _diagnosticService;

        private readonly ImmutableArray<string> _supportedLanguages;

        internal VisualStudioInProcLanguageServer(
            AbstractRequestDispatcherFactory requestDispatcherFactory,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            LspWorkspaceRegistrationService workspaceRegistrationService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLogger logger,
            IDiagnosticService? diagnosticService,
            ImmutableArray<string> supportedLanguages,
            string? clientName,
            string userVisibleServerName,
            string telemetryServerTypeName)
            : base(requestDispatcherFactory, jsonRpc, capabilitiesProvider, workspaceRegistrationService, lspMiscellaneousFilesWorkspace: null, globalOptions, listenerProvider, logger, supportedLanguages, clientName, userVisibleServerName, telemetryServerTypeName)
        {
            _supportedLanguages = supportedLanguages;
            _diagnosticService = diagnosticService;

            // Dedupe on DocumentId.  If we hear about the same document multiple times, we only need to process that id once.
            _diagnosticsWorkQueue = new AsyncBatchingWorkQueue<DocumentId>(
                TimeSpan.FromMilliseconds(250),
                (ids, ct) => ProcessDiagnosticUpdatedBatchAsync(_diagnosticService, ids, ct),
                EqualityComparer<DocumentId>.Default,
                Listener,
                Queue.CancellationToken);

            if (_diagnosticService != null)
                _diagnosticService.DiagnosticsUpdated += DiagnosticService_DiagnosticsUpdated;
        }

        public override Task InitializedAsync()
        {
            try
            {
                Logger?.TraceStart("Initialized");

                // Publish diagnostics for all open documents immediately following initialization.
                PublishOpenFileDiagnostics();

                return Task.CompletedTask;
            }
            finally
            {
                Logger?.TraceStop("Initialized");
            }

            void PublishOpenFileDiagnostics()
            {
                foreach (var workspace in WorkspaceRegistrationService.GetAllRegistrations())
                {
                    var solution = workspace.CurrentSolution;
                    var openDocuments = workspace.GetOpenDocumentIds();
                    foreach (var documentId in openDocuments)
                        DiagnosticService_DiagnosticsUpdated(solution, documentId);
                }
            }
        }

        [JsonRpcMethod(VSInternalMethods.DocumentPullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalDiagnosticReport[]?> GetDocumentPullDiagnosticsAsync(VSInternalDocumentDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]?>(
                Queue, VSInternalMethods.DocumentPullDiagnosticName,
                diagnosticsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(VSInternalMethods.WorkspacePullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalWorkspaceDiagnosticReport[]?> GetWorkspacePullDiagnosticsAsync(VSInternalWorkspaceDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]?>(
                Queue, VSInternalMethods.WorkspacePullDiagnosticName,
                diagnosticsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(VSMethods.GetProjectContextsName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSProjectContextList?> GetProjectContextsAsync(VSGetProjectContextsParams textDocumentWithContextParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSGetProjectContextsParams, VSProjectContextList?>(Queue, VSMethods.GetProjectContextsName,
                textDocumentWithContextParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(VSInternalMethods.OnAutoInsertName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalDocumentOnAutoInsertResponseItem?> GetDocumentOnAutoInsertAsync(VSInternalDocumentOnAutoInsertParams autoInsertParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem?>(Queue, VSInternalMethods.OnAutoInsertName,
                autoInsertParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentLinkedEditingRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<LinkedEditingRanges?> GetLinkedEditingRangesAsync(LinkedEditingRangeParams renameParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<LinkedEditingRangeParams, LinkedEditingRanges?>(Queue, Methods.TextDocumentLinkedEditingRangeName,
                renameParams, _clientCapabilities, ClientName, cancellationToken);
        }

        protected override void ShutdownImpl()
        {
            base.ShutdownImpl();
            if (_diagnosticService != null)
                _diagnosticService.DiagnosticsUpdated -= DiagnosticService_DiagnosticsUpdated;
        }

        private void DiagnosticService_DiagnosticsUpdated(object? _, DiagnosticsUpdatedArgs e)
            => DiagnosticService_DiagnosticsUpdated(e.Solution, e.DocumentId);

        private void DiagnosticService_DiagnosticsUpdated(Solution? solution, DocumentId? documentId)
        {
            // LSP doesn't support diagnostics without a document. So if we get project level diagnostics without a document, ignore them.
            if (documentId != null && solution != null)
            {
                var document = solution.GetDocument(documentId);
                if (document == null || document.FilePath == null)
                    return;

                // Only publish document diagnostics for the languages this provider supports.
                if (!_supportedLanguages.Contains(document.Project.Language))
                    return;

                _diagnosticsWorkQueue.AddWork(document.Id);
            }
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
        private readonly Dictionary<Uri, Dictionary<DocumentId, ImmutableArray<LSP.Diagnostic>>> _publishedFileToDiagnostics = new();

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

        // internal for testing purposes
        internal async ValueTask ProcessDiagnosticUpdatedBatchAsync(
            IDiagnosticService? diagnosticService, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            if (diagnosticService == null)
                return;

            foreach (var documentId in documentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = WorkspaceRegistrationService.GetAllRegistrations().Select(w => w.CurrentSolution.GetDocument(documentId)).FirstOrDefault();

                if (document != null)
                {
                    // If this is a `pull` client, and `pull` diagnostics is on, then we should not `publish` (push) the
                    // diagnostics here. 
                    var diagnosticMode = document.IsRazorDocument()
                        ? InternalDiagnosticsOptions.RazorDiagnosticMode
                        : InternalDiagnosticsOptions.NormalDiagnosticMode;
                    if (GlobalOptions.IsPushDiagnostics(diagnosticMode))
                        await PublishDiagnosticsAsync(diagnosticService, document, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task PublishDiagnosticsAsync(IDiagnosticService diagnosticService, Document document, CancellationToken cancellationToken)
        {
            // Retrieve all diagnostics for the current document grouped by their actual file uri.
            var fileUriToDiagnostics = await GetDiagnosticsAsync(diagnosticService, document, cancellationToken).ConfigureAwait(false);

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
                    IReadOnlyDictionaryExtensions.GetValueOrDefault(_publishedFileToDiagnostics, fileUri)?.Remove(document.Id);
                    _documentsToPublishedUris.MultiRemove(document.Id, fileUri);
                }
            }
        }

        private async Task SendDiagnosticsNotificationAsync(Uri uri, ImmutableArray<LSP.Diagnostic> diagnostics)
        {
            var publishDiagnosticsParams = new PublishDiagnosticParams { Diagnostics = diagnostics.ToArray(), Uri = uri };
            await JsonRpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, publishDiagnosticsParams).ConfigureAwait(false);
        }

        private async Task<Dictionary<Uri, ImmutableArray<LSP.Diagnostic>>> GetDiagnosticsAsync(
            IDiagnosticService diagnosticService, Document document, CancellationToken cancellationToken)
        {
            var option = document.IsRazorDocument()
                ? InternalDiagnosticsOptions.RazorDiagnosticMode
                : InternalDiagnosticsOptions.NormalDiagnosticMode;
            var pushDiagnostics = await diagnosticService.GetPushDiagnosticsAsync(document.Project.Solution.Workspace, document.Project.Id, document.Id, id: null, includeSuppressedDiagnostics: false, option, cancellationToken).ConfigureAwait(false);
            var diagnostics = pushDiagnostics.WhereAsArray(IncludeDiagnostic);

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

                // Razor wants to handle all span mapping themselves.  So if we are in razor, return the raw doc spans, and
                // do not map them.
                var filePath = diagnosticData.DataLocation.MappedFilePath ?? diagnosticData.DataLocation.OriginalFilePath;
                return ProtocolConversions.GetUriFromFilePath(filePath);
            }
        }

        private LSP.Diagnostic ConvertToLspDiagnostic(DiagnosticData diagnosticData, SourceText text)
        {
            Contract.ThrowIfNull(diagnosticData.DataLocation);

            var diagnostic = new LSP.Diagnostic
            {
                Source = TelemetryServerName,
                Code = diagnosticData.Id,
                Severity = Convert(diagnosticData.Severity),
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

        private static LSP.DiagnosticSeverity Convert(CodeAnalysis.DiagnosticSeverity severity)
            => severity switch
            {
                CodeAnalysis.DiagnosticSeverity.Hidden => LSP.DiagnosticSeverity.Hint,
                CodeAnalysis.DiagnosticSeverity.Info => LSP.DiagnosticSeverity.Hint,
                CodeAnalysis.DiagnosticSeverity.Warning => LSP.DiagnosticSeverity.Warning,
                CodeAnalysis.DiagnosticSeverity.Error => LSP.DiagnosticSeverity.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(severity),
            };

        // Some diagnostics only apply to certain clients and document types, e.g. Razor.
        // If the DocumentPropertiesService.DiagnosticsLspClientName property exists, we only include the
        // diagnostic if it directly matches the client name.
        // If the DocumentPropertiesService.DiagnosticsLspClientName property doesn't exist,
        // we know that the diagnostic we're working with is contained in a C#/VB file, since
        // if we were working with a non-C#/VB file, then the property should have been populated.
        // In this case, unless we have a null client name, we don't want to publish the diagnostic
        // (since a null client name represents the C#/VB language server).
        private bool IncludeDiagnostic(DiagnosticData diagnostic)
            => IReadOnlyDictionaryExtensions.GetValueOrDefault(diagnostic.Properties, nameof(DocumentPropertiesService.DiagnosticsLspClientName)) == ClientName;

        private static LSP.Range GetDiagnosticRange(DiagnosticDataLocation diagnosticDataLocation, SourceText text)
        {
            var linePositionSpan = DiagnosticData.GetLinePositionSpan(diagnosticDataLocation, text, useMapped: true);
            return ProtocolConversions.LinePositionToRange(linePositionSpan);
        }

        internal new TestAccessor GetTestAccessor() => new(this);

        internal new readonly struct TestAccessor
        {
            private readonly VisualStudioInProcLanguageServer _server;

            internal TestAccessor(VisualStudioInProcLanguageServer server)
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
                    return diagnostics;

                return ImmutableArray<LSP.Diagnostic>.Empty;
            }
        }
    }
}
