// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal abstract class AbstractRefreshQueue :
        IOnInitialized,
        ILspService,
        IDisposable
    {
        private AsyncBatchingWorkQueue<string?>? _refreshQueue;

        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly IClientLanguageServerManager _notificationManager;

        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly CancellationTokenSource _disposalTokenSource;
        private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

        protected bool _isQueueCreated;

        protected abstract string GetFeatureAttribute();
        protected abstract bool? GetRefreshSupport(ClientCapabilities clientCapabilities);
        protected abstract string GetWorkspaceRefreshName();

        public AbstractRefreshQueue(
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
            LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            LspWorkspaceManager lspWorkspaceManager,
            IClientLanguageServerManager notificationManager)
        {
            _isQueueCreated = false;
            _asyncListener = asynchronousOperationListenerProvider.GetListener(GetFeatureAttribute());
            _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            _disposalTokenSource = new();
            _lspWorkspaceManager = lspWorkspaceManager;
            _notificationManager = notificationManager;
        }

        public Task OnInitializedAsync(ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (_refreshQueue is null && GetRefreshSupport(clientCapabilities) is true)
            {
                // Only send a refresh notification to the client every 2s (if needed) in order to avoid
                // sending too many notifications at once.  This ensures we batch up workspace notifications,
                // but also means we send soon enough after a compilation-computation to not make the user wait
                // an enormous amount of time.
                _refreshQueue = new AsyncBatchingWorkQueue<string?>(
                    delay: TimeSpan.FromMilliseconds(2000),
                    processBatchAsync: (documentPaths, cancellationToken)
                        => FilterLspTrackedDocumentsAsync(_lspWorkspaceManager, _notificationManager, documentPaths, cancellationToken),
                    equalityComparer: EqualityComparer<string?>.Default,
                    asyncListener: _asyncListener,
                    _disposalTokenSource.Token);
                _isQueueCreated = true;
                _lspWorkspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
            }

            return Task.CompletedTask;
        }

        private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            if (e.DocumentId is not null && e.Kind is WorkspaceChangeKind.DocumentChanged)
            {
                var document = e.NewSolution.GetRequiredDocument(e.DocumentId);
                var documentPath = ProtocolConversions.GetDocumentFilePathFromUri(document.GetURI());

                // We enqueue the URI since there's a chance the client is already tracking the
                // document, in which case we don't need to send a refresh notification.
                // We perform the actual check when processing the batch to ensure we have the
                // most up-to-date list of tracked documents.
                EnqueueRefreshNotification(documentPath);
            }
            else
            {
                EnqueueRefreshNotification(documentPath: null);
            }
        }

        protected void EnqueueRefreshNotification(string? documentPath)
        {
            if (_isQueueCreated)
            {
                Contract.ThrowIfNull(_refreshQueue);
                _refreshQueue.AddWork(documentPath);
            }
        }

        private ValueTask FilterLspTrackedDocumentsAsync(
            LspWorkspaceManager lspWorkspaceManager,
            IClientLanguageServerManager notificationManager,
            ImmutableSegmentedList<string?> documentPaths,
            CancellationToken cancellationToken)
        {
            var trackedDocuments = lspWorkspaceManager.GetTrackedLspText();
            foreach (var documentPath in documentPaths)
            {
                if (documentPath is null || !trackedDocuments.ContainsKey(documentPath))
                {
                    return notificationManager.SendRequestAsync(GetWorkspaceRefreshName(), cancellationToken);
                }
            }

            // LSP is already tracking all changed documents so we don't need to send a refresh request.
            return ValueTaskFactory.CompletedTask;
        }

        public virtual void Dispose()
        {
            _lspWorkspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
            _disposalTokenSource.Cancel();
            _disposalTokenSource.Dispose();
        }
    }
}
