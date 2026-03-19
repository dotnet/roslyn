// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;

internal sealed class SourceGeneratorRefreshQueue :
    IOnInitialized,
    ILspService,
    IDisposable
{
    private const string RefreshSourceGeneratedDocumentName = "workspace/refreshSourceGeneratedDocument";

    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly CancellationTokenSource _disposalTokenSource = new();
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
    private readonly LspWorkspaceManager _lspWorkspaceManager;
    private readonly IClientLanguageServerManager _notificationManager;
    private readonly AsyncBatchingWorkQueue _refreshQueue;

    public SourceGeneratorRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager)
    {
        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _lspWorkspaceManager = lspWorkspaceManager;
        _notificationManager = notificationManager;
        _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.SourceGenerators);

        // Batch up workspace notifications so that we only send a notification to refresh source generated files
        // every 2 seconds - long enough to avoid spamming the client with notifications, but short enough to refresh
        // the source generated files relatively frequently.
        _refreshQueue = _refreshQueue = new AsyncBatchingWorkQueue(
            delay: DelayTimeSpan.Idle,
            processBatchAsync: RefreshSourceGeneratedDocumentsAsync,
            asyncListener: _asyncListener,
            _disposalTokenSource.Token);
    }

    public async Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        if (clientCapabilities.HasVisualStudioLspCapability())
        {
            // VS source generated document content is not provided by LSP.
            return;
        }

        // After we have initialized we can start listening for workspace changes.
        _lspWorkspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
    }

    private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        var asyncToken = _asyncListener.BeginAsyncOperation($"{nameof(SourceGeneratorRefreshQueue)}.{nameof(OnLspSolutionChanged)}");
        _ = OnLspSolutionChangedAsync(e)
            .CompletesAsyncOperation(asyncToken)
            .ReportNonFatalErrorUnlessCancelledAsync(_disposalTokenSource.Token);
    }

    private async Task OnLspSolutionChangedAsync(WorkspaceChangeEventArgs e)
    {
        var projectId = e.ProjectId ?? e.DocumentId?.ProjectId;
        if (projectId is not null)
        {
            // We have a specific changed project - do some additional checks to see if
            // source generators possibly changed.  Note that this overreports actual
            // changes to the source generated text; we rely on resultIds in the text retrieval to avoid unnecessary serialization.

            var oldProject = e.OldSolution.GetProject(projectId);
            var newProject = e.NewSolution.GetProject(projectId);

            // If the project has been added/removed, we need to update the generated files.
            if (oldProject is null || newProject is null)
            {
                _refreshQueue.AddWork();
                return;
            }

            // Trivial check.  see if the SG version of these projects changed.  If so, we definitely want to update generated files.
            if (e.OldSolution.GetSourceGeneratorExecutionVersion(projectId) !=
                e.NewSolution.GetSourceGeneratorExecutionVersion(projectId))
            {
                _refreshQueue.AddWork();
                return;
            }

            // More expensive check - see if the dependent versions are different.
            await CheckDependentVersionsAsync(oldProject, newProject).ConfigureAwait(false);
        }
        else
        {
            // We don't have a specific project change - if this is a solution change we need to queue a refresh anyway.
            if (e.Kind is WorkspaceChangeKind.SolutionChanged or WorkspaceChangeKind.SolutionAdded or WorkspaceChangeKind.SolutionRemoved or WorkspaceChangeKind.SolutionReloaded or WorkspaceChangeKind.SolutionCleared)
            {
                _refreshQueue.AddWork();
            }
        }

        async Task CheckDependentVersionsAsync(Project oldProject, Project newProject)
        {
            if (await oldProject.GetDependentVersionAsync(_disposalTokenSource.Token).ConfigureAwait(false) !=
                await newProject.GetDependentVersionAsync(_disposalTokenSource.Token).ConfigureAwait(false))
            {
                _refreshQueue.AddWork();
            }
        }
    }

    private async ValueTask RefreshSourceGeneratedDocumentsAsync(
        CancellationToken cancellationToken)
    {
        var hasOpenSourceGeneratedDocuments = _lspWorkspaceManager.GetTrackedLspText().Keys.Any(uri => uri.ParsedUri?.Scheme == SourceGeneratedDocumentUri.Scheme);
        if (!hasOpenSourceGeneratedDocuments)
        {
            // There are no opened source generated documents - we don't need to bother asking the client to refresh anything.
            return;
        }

        try
        {
            await _notificationManager.SendNotificationAsync(RefreshSourceGeneratedDocumentName, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
        {
            // It is entirely possible that we're shutting down and the connection is lost while we're trying to send a notification
            // as this runs outside of the guaranteed ordering in the queue. We can safely ignore this exception.
        }
    }

    public void Dispose()
    {
        _lspWorkspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
        _disposalTokenSource.Cancel();
        _disposalTokenSource.Dispose();
    }
}
