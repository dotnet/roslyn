// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;

internal class SourceGeneratorRefreshQueue :
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
            delay: TimeSpan.FromMilliseconds(2000),
            processBatchAsync: RefreshSourceGeneratedDocumentsAsync,
            asyncListener: _asyncListener,
            _disposalTokenSource.Token);
    }

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        if (clientCapabilities.HasVisualStudioLspCapability())
        {
            // VS source generated document content is not provided by LSP.
            return Task.CompletedTask;
        }

        // After we have initialized we can start listening for workspace changes.
        _lspWorkspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
        return Task.CompletedTask;
    }

    private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        var projectId = e.ProjectId ?? e.DocumentId?.ProjectId;
        if (projectId is not null)
        {
            // We have a specific changed project - do some additional checks to see if
            // source generators possibly changed.  Note that this overreports actual
            // changes to the source generated text; we rely on resultIds in the text retrieval to avoid unnecessary serialization.

            // Trivial check.  see if the SG version of these projects changed.  If so, we definitely want to update
            // this generated file.
            if (e.OldSolution.GetSourceGeneratorExecutionVersion(projectId) !=
                e.NewSolution.GetSourceGeneratorExecutionVersion(projectId))
            {
                _refreshQueue.AddWork();
                return;
            }

            var oldProject = e.OldSolution.GetProject(projectId);
            var newProject = e.NewSolution.GetProject(projectId);

            if (oldProject != null && newProject != null)
            {
                var asyncToken = _asyncListener.BeginAsyncOperation($"{nameof(SourceGeneratorRefreshQueue)}.{nameof(OnLspSolutionChanged)}");
                CheckDependentVersionsAsync(oldProject, newProject, _disposalTokenSource.Token).CompletesAsyncOperation(asyncToken);
            }
        }
        else
        {
            // We don't have a specific project change - if this is a solution change we need to queue a refresh anyway.
            if (e.Kind is WorkspaceChangeKind.SolutionChanged or WorkspaceChangeKind.SolutionAdded or WorkspaceChangeKind.SolutionRemoved or WorkspaceChangeKind.SolutionReloaded or WorkspaceChangeKind.SolutionCleared)
            {
                _refreshQueue.AddWork();
            }
        }

        async Task CheckDependentVersionsAsync(Project oldProject, Project newProject, CancellationToken cancellationToken)
        {
            if (await oldProject.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false) !=
                await newProject.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false))
            {
                _refreshQueue.AddWork();
            }
        }
    }

    private ValueTask RefreshSourceGeneratedDocumentsAsync(
        CancellationToken cancellationToken)
    {
        var hasOpenSourceGeneratedDocuments = _lspWorkspaceManager.GetTrackedLspText().Keys.Any(uri => uri.Scheme == SourceGeneratedDocumentUri.Scheme);
        if (!hasOpenSourceGeneratedDocuments)
        {
            // There are no opened source generated documents - we don't need to bother asking the client to refresh anything.
            return ValueTaskFactory.CompletedTask;
        }

        try
        {
            return _notificationManager.SendNotificationAsync(RefreshSourceGeneratedDocumentName, cancellationToken);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or ConnectionLostException)
        {
            // It is entirely possible that we're shutting down and the connection is lost while we're trying to send a notification
            // as this runs outside of the guaranteed ordering in the queue. We can safely ignore this exception.
        }

        return ValueTaskFactory.CompletedTask;
    }

    public void Dispose()
    {
        _lspWorkspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
        _disposalTokenSource.Cancel();
        _disposalTokenSource.Dispose();
    }
}
