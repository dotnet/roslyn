// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Threading;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IHtmlDocumentSynchronizer))]
[method: ImportingConstructor]
internal sealed partial class HtmlDocumentSynchronizer(
    IRemoteServiceInvoker remoteServiceInvoker,
    IHtmlDocumentPublisher htmlDocumentPublisher,
    ILoggerFactory loggerFactory)
    : IHtmlDocumentSynchronizer
{
    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;
    private readonly IHtmlDocumentPublisher _htmlDocumentPublisher = htmlDocumentPublisher;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<HtmlDocumentSynchronizer>();

    private readonly Dictionary<Uri, SynchronizationRequest> _synchronizationRequests = [];
    // Semaphore to lock access to the dictionary above
#pragma warning disable RS0030 // Do not use banned APIs
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1);
#pragma warning restore RS0030 // Do not use banned APIs

    public void DocumentRemoved(Uri razorFileUri, CancellationToken cancellationToken)
    {
        using var _ = _semaphore.DisposableWait(cancellationToken);

        if (_synchronizationRequests.TryGetValue(razorFileUri, out var request))
        {
            _logger.LogDebug($"Document {razorFileUri} removed, so we're disposing and clearing out the sync request for it");
            request.Dispose();
            _synchronizationRequests.Remove(razorFileUri);
        }
    }

    public async Task<SynchronizationResult> TrySynchronizeAsync(TextDocument document, CancellationToken cancellationToken)
    {
        var requestedVersion = await RazorDocumentVersion.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug($"TrySynchronize for {document.FilePath} as at {requestedVersion}");

        return await GetSynchronizationRequestTaskAsync(document, requestedVersion, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a task that will complete when the html document for the Razor document has been made available
    /// </summary>
    /// <remarks>
    /// Whilst this is a task-returning method, really its not is to manage the <see cref="_synchronizationRequests" /> dictionary.
    /// When this method is called, one of 3 things could happen:
    /// <list type="number">
    /// <item>Nobody has asked for that document before, or they asked but the task failed, so a new task is started and returned</item>
    /// <item>Somebody else already asked for that document, so you get the task they were given</item>
    /// <item>Somebody else already asked for a future version of that document, so you get nothing</item>
    /// </list>
    /// If option 1 is taken, any pending tasks for older versions of the document will be cancelled.
    /// </remarks>
    private async Task<SynchronizationResult> GetSynchronizationRequestTaskAsync(TextDocument document, RazorDocumentVersion requestedVersion, CancellationToken cancellationToken)
    {
        Task<SynchronizationResult> taskToGetResult;
        using (var _ = await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug($"Not synchronizing Html text for {document.FilePath} as the request was cancelled.");
                return default;
            }

            taskToGetResult = GetOrAddResultTask_CallUnderLockAsync();
        }

        return await taskToGetResult.ConfigureAwait(false);

        Task<SynchronizationResult> GetOrAddResultTask_CallUnderLockAsync()
        {
            var documentUri = document.CreateUri();
            if (_synchronizationRequests.TryGetValue(documentUri, out var request))
            {
                if (requestedVersion.Checksum.Equals(request.RequestedVersion.Checksum))
                {
                    // Two documents are always equal if their checksums are equal, for the purposes of Html document generation, because
                    // Html documents don't require semantic information. WorkspaceVersion changed too often to be used as a measure
                    // of equality for this purpose.

                    if (request.Task.IsCompleted && !request.Task.VerifyCompleted().Synchronized)
                    {
                        _logger.LogDebug($"Already finished that version for {document.FilePath}, but was unsuccessful, so will recompute");
                        request.Dispose();
                    }
                    else
                    {
                        _logger.LogDebug($"Already {(request.Task.IsCompleted ? "finished" : "working on")} that version for {document.FilePath}");
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                        return request.Task;
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
                    }
                }
                else if (requestedVersion.WorkspaceVersion < request.RequestedVersion.WorkspaceVersion)
                {
                    // We know the documents aren't the same, but checksums can't tell us which is newer, so we use WorkspaceVersion for that.
                    // It is theoretically possible, however, that two different documents could have the same WorkspaceVersion, so we use the
                    // fact that LSP change messages are strictly ordered, and only move the document forward, such that if we get a request
                    // for a different checksum, but the same workspace version, we assume the new request is the newer document.

                    _logger.LogDebug($"We've already seen {request.RequestedVersion} for {document.FilePath} so that's a no from me");
                    return SpecializedTasks.Default<SynchronizationResult>();
                }
                else if (!request.Task.IsCompleted)
                {
                    // We've had a previous request, but this is newer, and our previous work hasn't finished yet
                    _logger.LogDebug($"We were working on {request.RequestedVersion} for {document.FilePath} but you're newer so we're giving up on that");
                    request.Dispose();
                }
            }

            _logger.LogDebug($"Going to start working on Html for {document.FilePath} as at {requestedVersion}");

            var newRequest = SynchronizationRequest.CreateAndStart(document, requestedVersion, PublishHtmlDocumentAsync, cancellationToken);
            _synchronizationRequests[documentUri] = newRequest;
            return newRequest.Task;
        }
    }

    private async Task<SynchronizationResult> PublishHtmlDocumentAsync(TextDocument document, RazorDocumentVersion requestedVersion, CancellationToken cancellationToken)
    {
        string? htmlText;
        try
        {
            htmlText = await _remoteServiceInvoker.TryInvokeAsync<IRemoteHtmlDocumentService, string?>(document.Project.Solution,
                (service, solutionInfo, ct) => service.GetHtmlDocumentTextAsync(solutionInfo, document.Id, ct),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, $"Error getting Html text for {document.FilePath}. Html document contents will be stale");
            return default;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug($"Not publishing Html text for {document.FilePath} as the request was cancelled.");
            return default;
        }

        if (htmlText is null)
        {
            _logger.LogError($"Couldn't get Html text for {document.FilePath}. Html document contents will be stale");
            return default;
        }

        try
        {
            var synchronized = await _htmlDocumentPublisher.TryPublishAsync(document, requestedVersion.Checksum, htmlText, cancellationToken).ConfigureAwait(false);
            var result = new SynchronizationResult(Synchronized: synchronized, requestedVersion.Checksum);

            // If we were cancelled, we can't trust that the publish worked.
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug($"Not publishing Html text for {document.FilePath} as the request was cancelled.");
                return default;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug($"Not publishing Html text for {document.FilePath} as the request was cancelled.");
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing Html text for {document.FilePath}. Html document contents will be stale");
            return default;
        }
    }

    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly HtmlDocumentSynchronizer _instance;

        internal TestAccessor(HtmlDocumentSynchronizer instance)
        {
            _instance = instance;
        }

        public Task<SynchronizationResult> GetSynchronizationRequestTaskAsync(TextDocument document, RazorDocumentVersion requestedVersion, CancellationToken cancellationToken)
            => _instance.GetSynchronizationRequestTaskAsync(document, requestedVersion, cancellationToken);
    }
}
