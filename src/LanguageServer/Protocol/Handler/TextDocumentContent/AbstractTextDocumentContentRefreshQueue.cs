// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.TextDocumentContent;

/// <summary>
/// Abstract refresh queue for text document content providers. Subclasses specify which URI scheme they handle
/// and implement custom change detection logic via <see cref="AbstractRefreshQueue.OnLspSolutionChanged"/>.
/// Refresh notifications are sent for any open document matching the specified scheme.
/// </summary>
internal abstract class AbstractTextDocumentContentRefreshQueue :
    IOnInitialized,
    ILspService,
    IDisposable
{
    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly CancellationTokenSource _disposalTokenSource = new();
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
    private readonly LspWorkspaceManager _lspWorkspaceManager;
    private readonly IClientLanguageServerManager _notificationManager;
    private readonly AsyncBatchingWorkQueue _refreshQueue;
    public AbstractTextDocumentContentRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager)
    {
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _lspWorkspaceManager = lspWorkspaceManager;
        _notificationManager = notificationManager;
        _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Workspace);

        // Batch up workspace notifications so that we only send a notification to refresh virtual files
        // every 2 seconds - long enough to avoid spamming the client with notifications, but short enough to refresh
        // the virtual files relatively frequently.
        _refreshQueue = _refreshQueue = new AsyncBatchingWorkQueue(
            delay: DelayTimeSpan.Idle,
            processBatchAsync: RefreshVirtualDocumentsAsync,
            asyncListener: _asyncListener,
            _disposalTokenSource.Token);
    }

    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        if (clientCapabilities.Workspace?.TextDocumentContent == null)
        {
            return;
        }

        // After we have initialized we can start listening for workspace changes.
        _lspWorkspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
    }

    private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        var asyncToken = _asyncListener.BeginAsyncOperation($"{nameof(AbstractTextDocumentContentRefreshQueue)}.{nameof(OnLspSolutionChanged)}");
        _ = OnLspSolutionChangedAsync(e)
            .CompletesAsyncOperation(asyncToken)
            .ReportNonFatalErrorUnlessCancelledAsync(_disposalTokenSource.Token);
    }

    protected async Task OnLspSolutionChangedAsync(WorkspaceChangeEventArgs e)
    {
        var shouldQueue = await ShouldEnqueueRefreshNotificationAsync(e, _disposalTokenSource.Token).ConfigureAwait(false);
        if (shouldQueue)
        {
            _refreshQueue.AddWork();
        }
    }

    protected abstract Task<bool> ShouldEnqueueRefreshNotificationAsync(WorkspaceChangeEventArgs e, CancellationToken cancellationToken);

    /// <summary>
    /// The scheme that this queue is responsible for.
    /// </summary>
    protected abstract string Scheme { get; }

    private async ValueTask RefreshVirtualDocumentsAsync(
        CancellationToken cancellationToken)
    {
        var trackedDocuments = _lspWorkspaceManager.GetTrackedLspText();

        foreach (var kvp in trackedDocuments)
        {
            var uri = kvp.Key;
            if (uri.ParsedUri is { } parsedUri && parsedUri.Scheme == Scheme)
            {
                try
                {
                    await _notificationManager.SendRequestAsync(
                        Methods.WorkspaceTextDocumentContentRefreshName,
                        new TextDocumentContentRefreshParams { Uri = uri },
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
                {
                    // Connection may be lost during shutdown.
                    return;
                }
            }
        }
    }

    public void Dispose()
    {
        _lspWorkspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
        _disposalTokenSource.Cancel();
        _disposalTokenSource.Dispose();
    }
}
