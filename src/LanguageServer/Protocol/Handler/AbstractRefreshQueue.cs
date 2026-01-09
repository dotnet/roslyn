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
using Microsoft.CodeAnalysis.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal abstract class AbstractRefreshQueue :
    IOnInitialized,
    ILspService,
    IDisposable
{
    private AsyncBatchingWorkQueue<DocumentUri?>? _refreshQueue;

    private readonly LspWorkspaceManager _lspWorkspaceManager;
    private readonly IClientLanguageServerManager _notificationManager;

    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly CancellationTokenSource _disposalTokenSource;
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

    protected virtual bool ListenForProviderRefresh => true;

    protected abstract string GetFeatureAttribute();
    protected abstract bool? GetRefreshSupport(ClientCapabilities clientCapabilities);
    protected abstract string GetWorkspaceRefreshName();

    public AbstractRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager,
        IFeatureProviderRefresher refresher)
    {
        _asyncListener = asynchronousOperationListenerProvider.GetListener(GetFeatureAttribute());
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _disposalTokenSource = new();
        _lspWorkspaceManager = lspWorkspaceManager;
        _notificationManager = notificationManager;

        if (ListenForProviderRefresh)
        {
            refresher.ProviderRefreshRequested += documentUri =>
            {
                EnqueueRefreshNotification(documentUri);
            };
        }
    }

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        Initialize(clientCapabilities);
        return Task.CompletedTask;
    }

    public void Initialize(ClientCapabilities clientCapabilities)
    {
        if (_refreshQueue is null && GetRefreshSupport(clientCapabilities) is true)
        {
            // Only send a refresh notification to the client every 2s (if needed) in order to avoid
            // sending too many notifications at once.  This ensures we batch up workspace notifications,
            // but also means we send soon enough after a compilation-computation to not make the user wait
            // an enormous amount of time.
            _refreshQueue = new AsyncBatchingWorkQueue<DocumentUri?>(
                delay: TimeSpan.FromMilliseconds(2000),
                processBatchAsync: (documentUris, cancellationToken)
                    => FilterLspTrackedDocumentsAsync(_lspWorkspaceManager, _notificationManager, documentUris, cancellationToken),
                equalityComparer: EqualityComparer<DocumentUri?>.Default,
                asyncListener: _asyncListener,
                _disposalTokenSource.Token);
            _lspWorkspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
        }
    }

    protected virtual void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        if (e.DocumentId is not null && e.Kind is WorkspaceChangeKind.DocumentChanged)
        {
            var document = e.NewSolution.GetRequiredDocument(e.DocumentId);
            var documentUri = document.GetURI();

            // We enqueue the URI since there's a chance the client is already tracking the
            // document, in which case we don't need to send a refresh notification.
            // We perform the actual check when processing the batch to ensure we have the
            // most up-to-date list of tracked documents.
            EnqueueRefreshNotification(documentUri);
        }
        else
        {
            EnqueueRefreshNotification(documentUri: null);
        }
    }

    /// <summary>
    /// Enqueues a request to refresh the workspace.  If <paramref name="documentUri"/> is null, then the refresh will
    /// always happen.  If non-null, the refresh will only happen if the client is <em>not</em> tracking that document.
    /// If the client is tracking the document, no refresh is necessary as the client clearly knows about the change.
    /// </summary>
    protected void EnqueueRefreshNotification(DocumentUri? documentUri)
        => _refreshQueue?.AddWork(documentUri);

    private async ValueTask FilterLspTrackedDocumentsAsync(
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager,
        ImmutableSegmentedList<DocumentUri?> documentUris,
        CancellationToken cancellationToken)
    {
        var trackedDocuments = lspWorkspaceManager.GetTrackedLspText();
        foreach (var documentUri in documentUris)
        {
            if (documentUri is null || !trackedDocuments.ContainsKey(documentUri))
            {
                try
                {
                    // Fire the notification and immediately return.  Refresh notifications are server-wide, and are not
                    // associated with a particular project/document.  So once we've sent one, we can stop processing
                    // entirely.
                    await notificationManager.SendRequestAsync(GetWorkspaceRefreshName(), cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
                {
                    // It is entirely possible that we're shutting down and the connection is lost while we're trying to send a notification
                    // as this runs outside of the guaranteed ordering in the queue. We can safely ignore this exception.
                }
            }
        }

        // LSP is already tracking all changed documents so we don't need to send a refresh request.
    }

    public virtual void Dispose()
    {
        _lspWorkspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
        _disposalTokenSource.Cancel();
        _disposalTokenSource.Dispose();
    }
}
