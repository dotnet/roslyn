// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using System.Collections.Immutable;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

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
/// and any consumers observing the results of the task returned from
/// <see cref="ExecuteAsync{TRequestType, TResponseType}(TRequestType, string, ILspServices, CancellationToken)"/>
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
public class RequestExecutionQueue<TRequestContext> : IRequestExecutionQueue<TRequestContext>
{
    protected readonly ILspLogger _logger;
    private readonly IHandlerProvider _handlerProvider;

    /// <summary>
    /// The queue containing the ordered LSP requests along with a combined cancellation token
    /// representing the queue's cancellation token and the individual request cancellation token.
    /// </summary>
    protected readonly AsyncQueue<(IQueueItem<TRequestContext> queueItem, CancellationToken cancellationToken)> _queue = new();
    private readonly CancellationTokenSource _cancelSource = new();

    /// <summary>
    /// For test purposes only.
    /// A task that completes when the queue processing stops.
    /// </summary>
    protected Task? _queueProcessingTask;

    public CancellationToken CancellationToken => _cancelSource.Token;

    public RequestExecutionQueue(ILspLogger logger, IHandlerProvider handlerProvider)
    {
        _logger = logger;
        _handlerProvider = handlerProvider;
    }

    public void Start()
    {
        // Start the queue processing
        _queueProcessingTask = ProcessQueueAsync();
    }

    protected ITextDocumentIdentifierHandler? GetTextDocumentIdentifierHandler<TRequest, TResponse>(string methodName)
    {
        var handler = GetMethodHandler<TRequest, TResponse>(methodName);

        ITextDocumentIdentifierHandler? textDocument = null;
        if (handler is ITextDocumentIdentifierHandler textDocumentIdentifierHandler)
            textDocument = textDocumentIdentifierHandler;

        return textDocument;
    }

    private IMethodHandler GetMethodHandler<TRequest, TResponse>(string methodName)
    {
        var requestType = typeof(TRequest) == typeof(VoidReturn) ? null : typeof(TRequest);
        var responseType = typeof(TResponse) == typeof(VoidReturn) ? null : typeof(TResponse);

        var handler = _handlerProvider.GetMethodHandler(methodName, requestType, responseType);

        return handler;
    }

    /// <summary>
    /// Queues a request to be handled by the specified handler, with mutating requests blocking subsequent requests
    /// from starting until the mutation is complete.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="methodName">The name of the LSP method.</param>
    /// <param name="requestCancellationToken">A cancellation token that will cancel the handing of this request.
    /// The request could also be cancelled by the queue shutting down.</param>
    /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
    public Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        // Note: If the queue is not accepting any more items then TryEnqueue below will fail.

        var handler = GetMethodHandler<TRequest, TResponse>(methodName);
        // Create a combined cancellation token so either the client cancelling it's token or the queue
        // shutting down cancels the request.
        var combinedTokenSource = _cancelSource.Token.CombineWith(requestCancellationToken);
        var combinedCancellationToken = combinedTokenSource.Token;
        var (item, resultTask) = CreateQueueItem<TRequest, TResponse>(
            handler.MutatesSolutionState,
            methodName,
            handler,
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
            return Task.FromException<TResponse>(new InvalidOperationException("Server was requested to shut down."));

        return resultTask;
    }

    internal (IQueueItem<TRequestContext>, Task<TResponse>) CreateQueueItem<TRequest, TResponse>(
        bool mutatesSolutionState,
        string methodName,
        IMethodHandler methodHandler,
        TRequest request,
        IMethodHandler handler,
        ILspServices lspServices,
        CancellationToken cancellationToken) => QueueItem<TRequest, TResponse, TRequestContext>.Create(mutatesSolutionState,
            methodName,
            methodHandler,
            request,
            handler,
            lspServices,
            _logger,
            cancellationToken);

    private async Task ProcessQueueAsync()
    {
        ILspServices? lspServices = null;
        try
        {
            while (!_cancelSource.IsCancellationRequested)
            {
                // First attempt to de-queue the work item in its own try-catch.
                // This is because before we de-queue we do not have access to the queue item's linked cancellation token.
                (IQueueItem<TRequestContext> work, CancellationToken cancellationToken) queueItem;
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
                    lspServices = work.LspServices;

                    var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);

                    if (work.MutatesServerState)
                    {
                        // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                        // Since we're explicitly awaiting exceptions to mutating requests will bubble up here.
                        await work.StartRequestAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // Non mutating are fire-and-forget because they are by definition readonly. Any errors
                        // will be sent back to the client but we can still capture errors in queue processing
                        // via NFW, though these errors don't put us into a bad state as far as the rest of the queue goes.
                        // Furthermore we use Task.Run here to protect ourselves against synchronous execution of work
                        // blocking the request queue for longer periods of time (it enforces parallelizabilty).
                        _ = Task.Run(() => work.StartRequestAsync(cancellationToken), cancellationToken);
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
            _logger.LogException(ex);
            var message = $"Error occurred processing queue: {ex.Message}.";
            if (lspServices is not null)
            {
                var lifecycleManager = lspServices.GetRequiredService<ILifeCycleManager>();
                await lifecycleManager.ShutdownAsync(message).ConfigureAwait(false);
                await lifecycleManager.ExitAsync().ConfigureAwait(false);
            }

            await DisposeAsync().ConfigureAwait(false);
            return;
        }
    }

    /// <summary>
    /// Shuts down the queue, stops accepting new messages, and cancels any in-progress or queued tasks.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _cancelSource.Cancel();

        // Tell the queue not to accept any more items.
        // Note: We do not need to spin through the queue manually and cancel items as
        // 1.  New queue instances are created for each server, so items in the queue would be gc'd.
        // 2.  Their cancellation tokens are linked to the queue's _cancelSource so are also cancelled.
        _queue.Complete();

        return new ValueTask();
    }

    #region Test Accessor
    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly RequestExecutionQueue<TRequestContext> _queue;

        public TestAccessor(RequestExecutionQueue<TRequestContext> queue)
            => _queue = queue;

        public bool IsComplete() => _queue._queue.IsCompleted && _queue._queue.IsEmpty;

        public async Task WaitForProcessingToStopAsync()
        {
            if (_queue._queueProcessingTask is not null)
            {
                await _queue._queueProcessingTask.ConfigureAwait(false);
            }
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
                    return false;
            }

            return true;
        }
    }
    #endregion
}
