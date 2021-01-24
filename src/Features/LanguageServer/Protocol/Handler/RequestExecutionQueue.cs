// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Coordinates the exectution of LSP messages to ensure correct results are sent back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a request comes in for some data the handler must be able to access a solution state that is correct
    /// at the time of the request, that takes into account any text change requests that have come in  previously
    /// (via textDocument/didChange for example).
    /// </para>
    /// <para>
    /// This class acheives this by distinguishing between mutating and non-mutating requests, and ensuring that
    /// when a mutating request comes in, its processing blocks all subsequent requests. As each request comes in
    /// it is added to a queue, and a queue item will not be retrieved while a mutating request is running. Before
    /// any request is handled the solution state is created by merging workspace solution state, which could have
    /// changes from non-LSP means (eg, adding a project reference), with the current "mutated" state.
    /// When a non-mutating work item is retrieved from the queue, it is given the current solution state, but then
    /// run in a fire-and-forget fashion.
    /// </para>
    /// <para>
    /// Regardless of whether a request is mutating or not, or blocking or not, is an implementation detail of this class
    /// and any consumers observing the results of the task returned from <see cref="ExecuteAsync{TRequestType, TResponseType}(bool, IRequestHandler{TRequestType, TResponseType}, TRequestType, ClientCapabilities, string?, string, CancellationToken)"/>
    /// will see the results of the handling of the request, whenever it occurred.
    /// </para>
    /// <para>
    /// Exceptions in the handling of non-mutating requests are sent back to callers. Exceptions in the processing of
    /// the queue will close the LSP connection so that the client can reconnect. Exceptions in the handling of mutating
    /// requests will also close the LSP connection, as at that point the mutated solution is in an unknown state.
    /// </para>
    /// <para>
    /// After shutdown is called, or an error causes the closing of the connection, the queue will not accept any
    /// more messages, and a new queue will need to be created.
    /// </para>
    /// </remarks>
    internal partial class RequestExecutionQueue
    {
        private readonly string _serverName;

        private readonly AsyncQueue<QueueItem> _queue;
        private readonly CancellationTokenSource _cancelSource;
        private readonly DocumentChangeTracker _documentChangeTracker;

        // This dictionary is used to cache our forked LSP solution so we don't have to
        // recompute it for each request. We don't need to worry about threading because they are only
        // used when preparing to handle a request, which happens in a single thread in the ProcessQueueAsync
        // method.
        private readonly Dictionary<Workspace, (Solution workspaceSolution, Solution lspSolution)> _lspSolutionCache = new();
        private readonly ILspLogger _logger;
        private readonly ILspWorkspaceRegistrationService _workspaceRegistrationService;

        public CancellationToken CancellationToken => _cancelSource.Token;

        /// <summary>
        /// Raised when the execution queue has failed, or the solution state its tracking is in an unknown state
        /// and so the only course of action is to shutdown the server so that the client re-connects and we can
        /// start over again.
        /// </summary>
        /// <remarks>
        /// Once this event has been fired all currently active and pending work items in the queue will be cancelled.
        /// </remarks>
        public event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;

        public RequestExecutionQueue(
            ILspLogger logger,
            ILspWorkspaceRegistrationService workspaceRegistrationService,
            string serverName)
        {
            _logger = logger;
            _workspaceRegistrationService = workspaceRegistrationService;
            _serverName = serverName;

            _queue = new AsyncQueue<QueueItem>();
            _cancelSource = new CancellationTokenSource();
            _documentChangeTracker = new DocumentChangeTracker();

            // Start the queue processing
            _ = ProcessQueueAsync();
        }

        /// <summary>
        /// Shuts down the queue, stops accepting new messages, and cancels any in-progress or queued tasks. Calling
        /// this multiple times won't cause any issues.
        /// </summary>
        public void Shutdown()
        {
            _cancelSource.Cancel();
            DrainQueue();
        }

        /// <summary>
        /// Queues a request to be handled by the specified handler, with mutating requests blocking subsequent requests
        /// from starting until the mutation is complete.
        /// </summary>
        /// <param name="mutatesSolutionState">Whether or not handling this method results in changes to the current solution state.
        /// Mutating requests will block all subsequent requests from starting until after they have
        /// completed and mutations have been applied.</param>
        /// <param name="handler">The handler that will handle the request.</param>
        /// <param name="request">The request to handle.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <param name="clientName">The client name.</param>
        /// <param name="methodName">The name of the LSP method.</param>
        /// <param name="requestCancellationToken">A cancellation token that will cancel the handing of this request.
        /// The request could also be cancelled by the queue shutting down.</param>
        /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
        public Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(
            bool mutatesSolutionState,
            IRequestHandler<TRequestType, TResponseType> handler,
            TRequestType request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            string methodName,
            CancellationToken requestCancellationToken) where TRequestType : class
        {
            // Create a task completion source that will represent the processing of this request to the caller
            var completion = new TaskCompletionSource<TResponseType>();

            // Note: If the queue is not accepting any more items then TryEnqueue below will fail.

            var textDocument = handler.GetTextDocumentIdentifier(request);
            var item = new QueueItem(
                mutatesSolutionState,
                clientCapabilities,
                clientName,
                methodName,
                textDocument,
                Trace.CorrelationManager.ActivityId,
                _logger,
                callbackAsync: async (context, cancellationToken) =>
                {
                    // Check if cancellation was requested while this was waiting in the queue
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.SetCanceled();

                        return;
                    }

                    try
                    {
                        var result = await handler.HandleRequestAsync(request, context, cancellationToken).ConfigureAwait(false);
                        completion.SetResult(result);
                    }
                    catch (OperationCanceledException ex)
                    {
                        completion.TrySetCanceled(ex.CancellationToken);
                    }
                    catch (Exception exception)
                    {
                        // Pass the exception to the task completion source, so the caller of the ExecuteAsync method can react
                        completion.SetException(exception);

                        // Also allow the exception to flow back to the request queue to handle as appropriate
                        throw new InvalidOperationException($"Error handling '{methodName}' request: {exception.Message}", exception);
                    }
                }, requestCancellationToken);

            var didEnqueue = _queue.TryEnqueue(item);

            // If the queue has been shut down the enqueue will fail, so we just fault the task immediately.
            // The queue itself is threadsafe (_queue.TryEnqueue and _queue.Complete use the same lock).
            if (!didEnqueue)
            {
                completion.SetException(new InvalidOperationException($"{_serverName} was requested to shut down."));
            }

            return completion.Task;
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (!_cancelSource.IsCancellationRequested)
                {
                    var work = await _queue.DequeueAsync().ConfigureAwait(false);

                    // Create a linked cancellation token to cancel any requests in progress when this shuts down
                    var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cancelSource.Token, work.CancellationToken).Token;

                    // Restore our activity id so that logging/tracking works across asynchronous calls.
                    Trace.CorrelationManager.ActivityId = work.ActivityId;
                    var context = CreateRequestContext(work);

                    if (work.MutatesSolutionState)
                    {
                        // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                        await work.CallbackAsync(context, cancellationToken).ConfigureAwait(false);

                        // Now that we've mutated our solution, clear out our saved state to ensure it gets recalculated
                        _lspSolutionCache.Remove(context.Solution.Workspace);
                    }
                    else
                    {
                        // Non mutating are fire-and-forget because they are by definition readonly. Any errors
                        // will be sent back to the client but we can still capture errors in queue processing
                        // via NFW, though these errors don't put us into a bad state as far as the rest of the queue goes.
                        _ = work.CallbackAsync(context, cancellationToken).ReportNonFatalErrorAsync();
                    }
                }
            }
            catch (OperationCanceledException e) when (e.CancellationToken == _cancelSource.Token)
            {
                // If the queue is asked to shut down between the start of the while loop, and the Dequeue call
                // we could end up here, but we don't want to report an error. The Shutdown call will take care of things.
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                _logger.TraceException(e);
                OnRequestServerShutdown($"Error occurred processing queue in {_serverName}: {e.Message}.");
            }
        }

        private void OnRequestServerShutdown(string message)
        {
            RequestServerShutdown?.Invoke(this, new RequestShutdownEventArgs(message));

            Shutdown();
        }

        /// <summary>
        /// Cancels all requests in the queue and stops the queue from accepting any more requests. After this method
        /// is called this queue is essentially useless.
        /// </summary>
        private void DrainQueue()
        {
            // Tell the queue not to accept any more items
            _queue.Complete();

            // Spin through the queue and pass in our cancelled token, so that the waiting tasks are cancelled.
            // NOTE: This only really works because the first thing that CallbackAsync does is check for cancellation
            // but generics make it annoying to store the TaskCompletionSource<TResult> on the QueueItem so this
            // is the best we can do for now. Ideally we would manipulate the TaskCompletionSource directly here
            // and just call SetCanceled
            while (_queue.TryDequeue(out var item))
            {
                _ = item.CallbackAsync(default, new CancellationToken(true));
            }
        }

        private RequestContext CreateRequestContext(QueueItem queueItem)
        {
            var trackerToUse = queueItem.MutatesSolutionState
                ? (IDocumentChangeTracker)_documentChangeTracker
                : new NonMutatingDocumentChangeTracker(_documentChangeTracker);

            return RequestContext.Create(
                queueItem.TextDocument,
                queueItem.ClientName,
                _logger,
                queueItem.ClientCapabilities,
                _workspaceRegistrationService,
                _lspSolutionCache,
                trackerToUse);
        }
    }
}
