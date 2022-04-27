// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens for a given range.
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(SemanticTokensRangeHandler)), Shared]
    [Method(Methods.TextDocumentSemanticTokensRangeName)]
    internal class SemanticTokensRangeHandler : AbstractStatelessRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>, IDisposable
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListener _asyncListener;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        /// <summary>
        /// Lock over the mutable state that follows.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Mapping from project id to the workqueue for producing the corresponding compilation for it on the OOP server.
        /// </summary>
        private readonly Dictionary<ProjectId, CompilationAvailableEventSource> _projectIdToEventSource = new();

        /// <summary>
        /// Mapping from project id to the solution checksum we were at when the project for it had its compilation
        /// produced on the oop server.
        /// </summary>
        private readonly Dictionary<ProjectId, Checksum> _projectIdLastComputedSolutionChecksum = new();

        // initialized when first request comes in.

        /// <summary>
        /// Initially null.  Set to true/false when first initialized.  The other following fields will be set if this
        /// is true.
        /// </summary>
        private bool? _supportsRefresh;

        private LspWorkspaceManager? _lspWorkspaceManager;
        private ILanguageServerNotificationManager? _notificationManager;

        /// <summary>
        /// Debouncing queue so that we don't attempt to issue a semantic tokens refresh notification too often.
        /// </summary>
        private AsyncBatchingWorkQueue? _semanticTokenRefreshQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler(
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
        {
            _globalOptions = globalOptions;
            _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Classification);
        }

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensRangeParams request)
        {
            Contract.ThrowIfNull(request.TextDocument);
            return request.TextDocument;
        }

        public void Dispose()
        {
            ImmutableArray<CompilationAvailableEventSource> eventSources;
            lock (_gate)
            {
                eventSources = _projectIdToEventSource.Values.ToImmutableArray();
                _projectIdToEventSource.Clear();
                _projectIdLastComputedSolutionChecksum.Clear();

                if (_lspWorkspaceManager != null)
                    _lspWorkspaceManager.LspSolutionChanged -= OnLspSolutionChanged;
            }

            foreach (var eventSource in eventSources)
                eventSource.Dispose();
        }

        [MemberNotNull(nameof(_supportsRefresh))]
        private void InitializeIfFirstRequest(RequestContext context)
        {
            lock (_gate)
            {
                if (_supportsRefresh == null)
                {
                    _supportsRefresh = context.ClientCapabilities.Workspace?.SemanticTokens.RefreshSupport is true;

                    if (_supportsRefresh.Value)
                    {
                        _lspWorkspaceManager = context.LspWorkspaceManager;
                        _notificationManager = context.NotificationManager;

                        // Only send a refresh notification to the client every 2s (if needed)
                        // in order to avoid sending too many notifications at once.
                        _semanticTokenRefreshQueue = new AsyncBatchingWorkQueue(
                            delay: TimeSpan.FromMilliseconds(2000),
                            processBatchAsync: SendSemanticTokensNotificationAsync,
                            asyncListener: _asyncListener,
                            context.QueueCancellationToken);

                        _lspWorkspaceManager.LspSolutionChanged += OnLspSolutionChanged;
                    }
                }
            }
        }

        private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
            => _semanticTokenRefreshQueue?.AddWork();

        public ValueTask SendSemanticTokensNotificationAsync(CancellationToken cancellationToken)
            => _notificationManager!.SendNotificationAsync(Methods.WorkspaceSemanticTokensRefreshName, cancellationToken);

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            SemanticTokensRangeParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            // If this is the first time getting a request, initialize our state with information about the
            // server/manager we're owned by.
            InitializeIfFirstRequest(context);

            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            Contract.ThrowIfNull(context.Document, "Document is null.");

            var project = context.Document.Project;
            var options = _globalOptions.GetClassificationOptions(project.Language);

            // The results from the range handler should not be cached since we don't want to cache
            // partial token results. In addition, a range request is only ever called with a whole
            // document request, so caching range results is unnecessary since the whole document
            // handler will cache the results anyway.
            var tokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document,
                SemanticTokensHelpers.TokenTypeToIndex,
                request.Range,
                options,
                includeSyntacticClassifications: context.Document.IsRazorDocument(),
                cancellationToken).ConfigureAwait(false);

            // The above call to get semantic tokens may be inaccurate (because we use frozen partial semantics).  Kick
            // off a request to ensure that the OOP side gets a fully up to compilation for this project.  Once it does
            // we can optionally choose to notify our caller to do a refresh if we computed a compilation for a new
            // solution snapshot.
            if (_supportsRefresh.Value)
            {
                lock (_gate)
                {
                    if (!_projectIdToEventSource.TryGetValue(project.Id, out var eventSource))
                    {
                        eventSource = new CompilationAvailableEventSource(_asyncListener);
                        _projectIdToEventSource.Add(project.Id, eventSource);
                    }

                    // Kick off work to have this project be sync'ed over to OOP so it's ready in the future for an upcoming 
                    eventSource.EnsureCompilationAvailability(project, OnCompilationAvailableAsync);
                }
            }

            return new LSP.SemanticTokens { Data = tokensData };
        }

        private async ValueTask OnCompilationAvailableAsync(Project project, CancellationToken cancellationToken)
        {
            var solutionChecksum = await project.Solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                if (_projectIdLastComputedSolutionChecksum.TryGetValue(project.Id, out var lastComputedChecksum) &&
                    lastComputedChecksum == solutionChecksum)
                {
                    // We got a notification again that the compilation is ready for a project we already notified the
                    // client about.  No need to do anything here.
                    return;
                }

                // keep track of this checksum.  That way we don't get into a loop where we send a refresh notification,
                // then we get called back into, causing us to compute the compilation, causing us to send the refresh
                // notification, etc. etc.
                _projectIdLastComputedSolutionChecksum[project.Id] = solutionChecksum;
            }

            // Enqueue an item to notify the client that they should do a refresh.
            _semanticTokenRefreshQueue?.AddWork();
        }
    }
}
