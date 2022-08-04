// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using System.Collections.Immutable;

#nullable enable

namespace CommonLanguageServerProtocol.Framework;

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
/// and any consumers observing the results of the task returned from <see cref="ExecuteAsync{TRequestType, TResponseType}(bool, bool, IRequestHandler{TRequestType, TResponseType, RequestContextType}, TRequestType, ClientCapabilities, string, CancellationToken)"/>
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
public class RequestExecutionQueue<RequestContextType> : IRequestExecutionQueue<RequestContextType>
{
    protected readonly string _serverKind;
    protected readonly ILspLogger _logger;

    /// <summary>
    /// The queue containing the ordered LSP requests along with a combined cancellation token
    /// representing the queue's cancellation token and the individual request cancellation token.
    /// </summary>
    protected readonly AsyncQueue<(IQueueItem<RequestContextType> queueItem, CancellationToken cancellationToken)> _queue = new();
    private readonly CancellationTokenSource _cancelSource = new();

    /// <summary>
    /// For test purposes only.
    /// A task that completes when the queue processing stops.
    /// </summary>
    protected Task? _queueProcessingTask;

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
        string serverKind,
        ILspLogger logger)
    {
        _serverKind = serverKind;
        _logger = logger;
    }

    /// <summary>
    /// Shuts down the queue, stops accepting new messages, and cancels any in-progress or queued tasks. Calling
    /// this multiple times won't cause any issues.
    /// </summary>
    public void Shutdown()
    {
        _cancelSource.Cancel();

        // Tell the queue not to accept any more items.
        // Note: We do not need to spin through the queue manually and cancel items as
        // 1.  New queue instances are created for each server, so items in the queue would be gc'd.
        // 2.  Their cancellation tokens are linked to the queue's _cancelSource so are also cancelled.
        _queue.Complete();
    }

    public void Start(ILspServices lspServices)
    {
        // Start the queue processing
        _queueProcessingTask = ProcessQueueAsync(lspServices);
    }

    public Task ExecuteAsync<TRequestType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        INotificationHandler<TRequestType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        var combinedTokenSource = _cancelSource.Token.CombineWith(requestCancellationToken);
        var combinedCancellationToken = combinedTokenSource.Token;
        var (item, resultTask) = CreateQueueItem(
            mutatesSolutionState,
            requiresLSPSolution,
            methodName,
            textDocument: null,
            request,
            handler,
            lspServices,
            combinedCancellationToken);

        _ = resultTask.ContinueWith(_ => combinedTokenSource.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        var didEnqueue = _queue.TryEnqueue((item, combinedCancellationToken));

        if (!didEnqueue)
        {
            return Task.FromException(new InvalidOperationException($"{_serverKind} was requested to shut down."));
        }

        return resultTask;
    }

    public Task ExecuteAsync(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        INotificationHandler<RequestContextType> handler,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        var combinedTokenSource = _cancelSource.Token.CombineWith(requestCancellationToken);
        var combinedCancellationToken = combinedTokenSource.Token;
        var (item, resultTask) = CreateQueueItem(
            mutatesSolutionState,
            requiresLSPSolution,
            methodName,
            textDocument: null,
            handler,
            lspServices,
            combinedCancellationToken);

        _ = resultTask.ContinueWith(_ => combinedTokenSource.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        var didEnqueue = _queue.TryEnqueue((item, combinedCancellationToken));

        if (!didEnqueue)
        {
            return Task.FromException(new InvalidOperationException($"{_serverKind} was requested to shut down."));
        }

        return resultTask;
    }

    /// <summary>
    /// Queues a request to be handled by the specified handler, with mutating requests blocking subsequent requests
    /// from starting until the mutation is complete.
    /// </summary>
    /// <param name="mutatesSolutionState">Whether or not handling this method results in changes to the current solution state.
    /// Mutating requests will block all subsequent requests from starting until after they have
    /// completed and mutations have been applied.</param>
    /// <param name="requiresLSPSolution">Whether or not to build a solution that represents the LSP view of the world. If this
    /// is set to false, the default workspace's current solution will be used.</param>
    /// <param name="handler">The handler that will handle the request.</param>
    /// <param name="request">The request to handle.</param>
    /// <param name="clientCapabilities">The client capabilities.</param>
    /// <param name="methodName">The name of the LSP method.</param>
    /// <param name="requestCancellationToken">A cancellation token that will cancel the handing of this request.
    /// The request could also be cancelled by the queue shutting down.</param>
    /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
    public Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        TRequestType request,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        // Note: If the queue is not accepting any more items then TryEnqueue below will fail.

        var textDocument = handler.GetTextDocumentUri(request);

        // Create a combined cancellation token so either the client cancelling it's token or the queue
        // shutting down cancels the request.
        var combinedTokenSource = _cancelSource.Token.CombineWith(requestCancellationToken);
        var combinedCancellationToken = combinedTokenSource.Token;
        var (item, resultTask) = CreateQueueItem(
            mutatesSolutionState,
            requiresLSPSolution,
            methodName,
            textDocument,
            request,
            handler,
            lspServices,
            combinedCancellationToken);

        // Run a continuation to ensure the cts is disposed of.
        // We pass CancellationToken.None as we always want to dispose of the source
        // even when the request is cancelled or the queue is shutting down.
        _ = resultTask.ContinueWith(_ => combinedTokenSource.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        var didEnqueue = _queue.TryEnqueue((item, combinedCancellationToken));

        // If the queue has been shut down the enqueue will fail, so we just fault the task immediately.
        // The queue itself is threadsafe (_queue.TryEnqueue and _queue.Complete use the same lock).
        if (!didEnqueue)
        {
            return Task.FromException<TResponseType>(new InvalidOperationException($"{_serverKind} was requested to shut down."));
        }

        return resultTask;
    }

    public virtual (IQueueItem<RequestContextType>, Task<TResponseType>) CreateQueueItem<TRequestType, TResponseType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        TRequestType request,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        ILspServices lspServices,
        CancellationToken cancellationToken)
    {
        return QueueItem<TRequestType, TResponseType, RequestContextType>.Create(mutatesSolutionState,
            requiresLSPSolution,
            methodName,
            textDocument,
            request,
            handler,
            _logger,
            cancellationToken);
    }

    public virtual (IQueueItem<RequestContextType>, Task) CreateQueueItem<TRequestType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        TRequestType request,
        INotificationHandler<TRequestType, RequestContextType> handler,
        ILspServices lspServices,
        CancellationToken cancellationToken)
    {
        return QueueItem<TRequestType, VoidReturn, RequestContextType>.Create(mutatesSolutionState,
            requiresLSPSolution,
            methodName,
            textDocument,
            request,
            handler,
            _logger,
            cancellationToken);
    }

    public virtual (IQueueItem<RequestContextType>, Task) CreateQueueItem(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        string methodName,
        object? textDocument,
        INotificationHandler<RequestContextType> handler,
        ILspServices lspServices,
        CancellationToken cancellationToken)
    {
        return QueueItem<VoidReturn, VoidReturn, RequestContextType>.Create(mutatesSolutionState,
            requiresLSPSolution,
            methodName,
            textDocument,
            VoidReturn.Instance,
            handler,
            _logger,
            cancellationToken);
    }

    private async Task ProcessQueueAsync(ILspServices lspServices)
    {
        try
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                // First attempt to de-queue the work item in its own try-catch.
                // This is because before we de-queue we do not have access to the queue item's linked cancellation token.
                (IQueueItem<RequestContextType> work, CancellationToken cancellationToken) queueItem;
                try
                {
                    queueItem = await _queue.DequeueAsync(_cancelSource.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == _cancelSource.Token)
                {
                    // The queue's cancellation token was invoked which means we are shutting down the queue.
                    // Exit out of the loop so we stop processing new items.
                    return;
                }

                try
                {
                    var (work, cancellationToken) = queueItem;
                    // Record when the work item was been de-queued and the request context preparation started.
                    work.OnExecutionStart();

                    var requestContextFactory = lspServices.GetRequiredService<IRequestContextFactory<RequestContextType>>();
                    var context = await requestContextFactory.CreateRequestContextAsync(work, queueCancellationToken: this.CancellationToken, requestCancellationToken: cancellationToken);

                    if (work.MutatesDocumentState)
                    {
                        // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                        // Since we're explicitly awaiting exceptions to mutating requests will bubble up here.
                        await work.StartRequestAsync(context, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Non mutating are fire-and-forget because they are by definition readonly. Any errors
                        // will be sent back to the client but we can still capture errors in queue processing
                        // via NFW, though these errors don't put us into a bad state as far as the rest of the queue goes.
                        // Furthermore we use Task.Run here to protect ourselves against synchronous execution of work
                        // blocking the request queue for longer periods of time (it enforces parallelizabilty).
                        _ = Task.Run(() => work.StartRequestAsync(context, cancellationToken), cancellationToken);
                    }
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == queueItem.cancellationToken)
                {
                    // Explicitly ignore this exception as cancellation occured as a result of our linked cancellation token.
                    // This means either the queue is shutting down or the request itself was cancelled.
                    //   1.  If the queue is shutting down, then while loop will exit before the next iteration since it checks for cancellation.
                    //   2.  Request cancellations are normal so no need to report anything there.
                }
            }
        }
        catch (Exception ex)
        {
            // We encountered an unexpected exception in processing the queue or in a mutating request.
            // Log it, shutdown the queue, and exit the loop.
            _logger.TraceException(ex);
            OnRequestServerShutdown($"Error occurred processing queue in {_serverKind}: {ex.Message}.");
            return;
        }
    }

    protected virtual IRequestContextFactory<RequestContextType> GetRequestContextFactory(ILspServices lspServices)
    {
        return lspServices.GetRequiredService<IRequestContextFactory<RequestContextType>>();
    }

    private void OnRequestServerShutdown(string message)
    {
        RequestServerShutdown?.Invoke(this, new RequestShutdownEventArgs(message));

        Shutdown();
    }

    #region Test Accessor
    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly RequestExecutionQueue<RequestContextType> _queue;

        public TestAccessor(RequestExecutionQueue<RequestContextType> queue)
            => _queue = queue;

        public bool IsComplete() => _queue._queue.IsCompleted && _queue._queue.IsEmpty;

        public async Task WaitForProcessingToStopAsync()
        {
            await _queue._queueProcessingTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Test only method to validate that remaining items in the queue are cancelled.
        /// This directly mutates the queue in an unsafe way, so ensure that all relevant queue operations
        /// are done before calling.
        /// </summary>
        public async Task<bool> AreAllItemsCancelledUnsafeAsync()
        {
            while (!_queue._queue.IsEmpty)
            {
                var (_, cancellationToken) = await _queue._queue.DequeueAsync().ConfigureAwait(false);
                if (!cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            }

            return true;
        }
    }
    #endregion
}
