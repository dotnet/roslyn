// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;

/// <summary>
/// Batches requests to refresh the semantic tokens to optimize user experience.
/// </summary>
/// <remarks>This implements <see cref="IOnInitialized"/> to avoid race conditions related to creating the queue on the
/// first request.</remarks>
internal class SemanticTokensRefreshQueue :
    IOnInitialized,
    ILspService,
    IDisposable
{
    /// <summary>
    /// Lock over the mutable state that follows.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Mapping from project id to the project-cone-checksum for it we were at when the project for it had its
    /// compilation produced on the oop server.
    /// </summary>
    private readonly Dictionary<ProjectId, Checksum> _projectIdToLastComputedChecksum = [];

    private readonly LspWorkspaceManager _lspWorkspaceManager;
    private readonly IClientLanguageServerManager _notificationManager;
    private readonly ICapabilitiesProvider _capabilitiesProvider;

    private readonly IAsynchronousOperationListener _asyncListener;
    private readonly CancellationTokenSource _disposalTokenSource = new();

    /// <summary>
    /// Debouncing queue so that we don't attempt to issue a semantic tokens refresh notification too often.
    /// <para/>
    /// <see langword="null"/> when the client does not support sending refresh notifications.
    /// </summary>
    private AsyncBatchingWorkQueue<Uri?>? _semanticTokenRefreshQueue;

    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

    public SemanticTokensRefreshQueue(
        IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspWorkspaceManager lspWorkspaceManager,
        IClientLanguageServerManager notificationManager,
        ICapabilitiesProvider capabilitiesProvider)
    {
        _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Classification);

        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _lspWorkspaceManager = lspWorkspaceManager;
        _notificationManager = notificationManager;
        _capabilitiesProvider = capabilitiesProvider;
    }

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken _)
    {
        if (_semanticTokenRefreshQueue is null
            && clientCapabilities.Workspace?.SemanticTokens?.RefreshSupport is true
            && _capabilitiesProvider.GetCapabilities(clientCapabilities).SemanticTokensOptions is not null)
        {
            // Only send a refresh notification to the client every 2s (if needed) in order to avoid sending too many
            // notifications at once.  This ensures we batch up workspace notifications, but also means we send soon
            // enough after a compilation-computation to not make the user wait an enormous amount of time.
            _semanticTokenRefreshQueue = new AsyncBatchingWorkQueue<Uri?>(
                delay: TimeSpan.FromMilliseconds(2000),
                processBatchAsync: FilterLspTrackedDocumentsAsync,
                equalityComparer: EqualityComparer<Uri?>.Default,
                asyncListener: _asyncListener,
                _disposalTokenSource.Token);

            _lspWorkspaceRegistrationService.LspSolutionChanged += OnLspSolutionChanged;
        }

        return Task.CompletedTask;
    }

    public async Task TryEnqueueRefreshComputationAsync(Project project, CancellationToken cancellationToken)
    {
        if (_semanticTokenRefreshQueue is not null)
        {
            // Determine the checksum for this project cone.  Note: this should be fast in practice because this is the
            // same project-cone-checksum we used to even call into OOP above when we computed semantic tokens.
            var projectChecksum = await project.Solution.CompilationState.GetChecksumAsync(project.Id, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                // If this checksum is the same as the last computed result, no need to continue, we would not produce a
                // different compilation.
                if (_projectIdToLastComputedChecksum.TryGetValue(project.Id, out var lastChecksum) && lastChecksum == projectChecksum)
                    return;

                // keep track of this checksum.  That way we don't get into a loop where we send a refresh notification,
                // then we get called back into, causing us to compute the compilation, causing us to send the refresh
                // notification, etc. etc.
                _projectIdToLastComputedChecksum[project.Id] = projectChecksum;

            }

            EnqueueSemanticTokenRefreshNotification(documentUri: null);
        }
    }

    private ValueTask FilterLspTrackedDocumentsAsync(
        ImmutableSegmentedList<Uri?> documentUris,
        CancellationToken cancellationToken)
    {
        var trackedDocuments = _lspWorkspaceManager.GetTrackedLspText();
        foreach (var documentUri in documentUris)
        {
            if (documentUri is null || !trackedDocuments.ContainsKey(documentUri))
                return _notificationManager.SendRequestAsync(Methods.WorkspaceSemanticTokensRefreshName, cancellationToken);
        }

        // LSP is already tracking all changed documents so we don't need to send a refresh request.
        return ValueTaskFactory.CompletedTask;
    }

    private void OnLspSolutionChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        Uri? documentUri = null;

        if (e.DocumentId is not null)
        {
            // We enqueue the URI since there's a chance the client is already tracking the
            // document, in which case we don't need to send a refresh notification.
            // We perform the actual check when processing the batch to ensure we have the
            // most up-to-date list of tracked documents.
            if (e.Kind is WorkspaceChangeKind.DocumentChanged)
            {
                var document = e.NewSolution.GetRequiredDocument(e.DocumentId);
                documentUri = document.GetURI();
            }
            else if (e.Kind is WorkspaceChangeKind.AdditionalDocumentChanged)
            {
                var document = e.NewSolution.GetRequiredAdditionalDocument(e.DocumentId);

                // Changes to files with certain extensions (eg: razor) shouldn't trigger semantic a token refresh
                if (DisallowsAdditionalDocumentChangedRefreshes(document.FilePath))
                    return;
            }
            else if (e.Kind is WorkspaceChangeKind.DocumentReloaded)
            {
                var newDocument = e.NewSolution.GetRequiredDocument(e.DocumentId);
                var oldDocument = e.OldSolution.GetDocument(e.DocumentId);

                // If the document's attributes haven't changed, then use the document's URI for
                //   the call to EnqueueSemanticTokenRefreshNotification which will enable the
                //   tracking check before sending the WorkspaceSemanticTokensRefreshName message.
                if (oldDocument?.State.Attributes.Checksum == newDocument.State.Attributes.Checksum)
                    documentUri = newDocument.GetURI();
            }
        }

        EnqueueSemanticTokenRefreshNotification(documentUri);
    }

    // Duplicated from Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.LoadedProject.TreatAsIsDynamicFile
    private static bool DisallowsAdditionalDocumentChangedRefreshes(string? filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension is ".cshtml" or ".razor";
    }

    private void EnqueueSemanticTokenRefreshNotification(Uri? documentUri)
    {
        // We should have only gotten here if semantic tokens refresh is supported and initialized.
        Contract.ThrowIfNull(_semanticTokenRefreshQueue);
        _semanticTokenRefreshQueue.AddWork(documentUri);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _lspWorkspaceRegistrationService.LspSolutionChanged -= OnLspSolutionChanged;
        }

        _disposalTokenSource.Cancel();
        _disposalTokenSource.Dispose();
    }
}
