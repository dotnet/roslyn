// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Coordinates the execution of LSP messages to ensure correct results are sent back.
/// </summary>
/// <remarks>
/// <para>
/// When a request comes in for some data the handler must be able to access a solution state that is correct
/// at the time of the request, that takes into account any text change requests that have come in  previously
/// (via textDocument/didChange for example).
/// </para>
/// <para>
/// This class achieves this by distinguishing between mutating and non-mutating requests, and ensuring that
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
/// <see cref="ExecuteAsync(object?, string, Microsoft.CommonLanguageServerProtocol.Framework.ILspServices, CancellationToken)"/>
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
internal class RequestExecutionQueue<TRequestContext> : IRequestExecutionQueue<TRequestContext>
{
    private static readonly MethodInfo s_processQueueCoreAsync = typeof(RequestExecutionQueue<TRequestContext>)
        .GetMethod(nameof(RequestExecutionQueue<TRequestContext>.ProcessQueueCoreAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    protected readonly ILspLogger _logger;
    protected readonly AbstractHandlerProvider _handlerProvider;
    private readonly AbstractLanguageServer<TRequestContext> _languageServer;

    /// <summary>
    /// The queue containing the ordered LSP requests along with the trace activityId (to associate logs with a request) and
    ///  a combined cancellation token representing the queue's cancellation token and the individual request cancellation token.
    /// </summary>
    protected readonly AsyncQueue<(IQueueItem<TRequestContext> queueItem, Guid ActivityId, CancellationToken cancellationToken)> _queue = new();
    private readonly CancellationTokenSource _cancelSource = new();

    /// <summary>
    /// Map of method to the handler info for each language.
    /// The handler info is created lazily to avoid instantiating any types or handlers until a request is recieved for
    /// that particular method and language.
    /// </summary>
    private readonly FrozenDictionary<string, FrozenDictionary<string, Lazy<(RequestHandlerMetadata Metadata, IMethodHandler Handler, MethodInfo MethodInfo)>>> _handlerInfoMap;

    /// <summary>
    /// For test purposes only.
    /// A task that completes when the queue processing stops.
    /// </summary>
    protected Task? _queueProcessingTask;

    public CancellationToken CancellationToken => _cancelSource.Token;

    public RequestExecutionQueue(AbstractLanguageServer<TRequestContext> languageServer, ILspLogger logger, AbstractHandlerProvider handlerProvider)
    {
        _languageServer = languageServer;
        _logger = logger;
        _handlerProvider = handlerProvider;
        _handlerInfoMap = BuildHandlerMap(handlerProvider, languageServer.TypeRefResolver);
    }

    private static FrozenDictionary<string, FrozenDictionary<string, Lazy<(RequestHandlerMetadata, IMethodHandler, MethodInfo)>>> BuildHandlerMap(AbstractHandlerProvider handlerProvider, AbstractTypeRefResolver typeRefResolver)
    {
        var genericMethodMap = new Dictionary<string, FrozenDictionary<string, Lazy<(RequestHandlerMetadata, IMethodHandler, MethodInfo)>>>();
        var noValueType = NoValue.Instance.GetType();
        // Get unique set of methods from the handler provider for the default language.
        foreach (var methodGroup in handlerProvider
            .GetRegisteredMethods()
            .GroupBy(m => m.MethodName))
        {
            var languages = new Dictionary<string, Lazy<(RequestHandlerMetadata, IMethodHandler, MethodInfo)>>();
            foreach (var metadata in methodGroup)
            {
                languages.Add(metadata.Language, new(() =>
                {
                    var requestType = metadata.RequestTypeRef is TypeRef requestTypeRef
                            ? typeRefResolver.Resolve(requestTypeRef) ?? noValueType
                            : noValueType;
                    var responseType = metadata.ResponseTypeRef is TypeRef responseTypeRef
                            ? typeRefResolver.Resolve(responseTypeRef) ?? noValueType
                            : noValueType;

                    var method = s_processQueueCoreAsync.MakeGenericMethod(requestType, responseType);
                    var handler = handlerProvider.GetMethodHandler(metadata.MethodName, metadata.RequestTypeRef, metadata.ResponseTypeRef, metadata.Language);
                    return (metadata, handler, method);
                }));
            }

            genericMethodMap.Add(methodGroup.Key, languages.ToFrozenDictionary());
        }

        return genericMethodMap.ToFrozenDictionary();
    }

    public void Start()
    {
        // Start the queue processing
        _queueProcessingTask = ProcessQueueAsync();
    }

    /// <summary>
    /// Indicates this queue requires in-progress work to be cancelled before servicing
    /// a mutating request.
    /// </summary>
    /// <remarks>
    /// This was added for WebTools consumption as they aren't resilient to
    /// incomplete requests continuing execution during didChange notifications. As their
    /// parse trees are mutable, a didChange notification requires all previous requests
    /// to be completed before processing. This is similar to the O#
    /// WithContentModifiedSupport(false) behavior.
    /// </remarks>
    protected virtual bool CancelInProgressWorkUponMutatingRequest => false;

    /// <summary>
    /// Queues a request to be handled by the specified handler, with mutating requests blocking subsequent requests
    /// from starting until the mutation is complete.
    /// </summary>
    /// <param name="serializedRequest">The serialized request to handle.</param>
    /// <param name="methodName">The name of the LSP method.</param>
    /// <param name="lspServices">The set of LSP services to use.</param>
    /// <param name="requestCancellationToken">A cancellation token that will cancel the handing of this request.
    /// The request could also be cancelled by the queue shutting down.</param>
    /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
    public virtual Task<object?> ExecuteAsync(
        object? serializedRequest,
        string methodName,
        ILspServices lspServices,
        CancellationToken requestCancellationToken)
    {
        // Note: If the queue is not accepting any more items then TryEnqueue below will fail.

        // Create a combined cancellation token so either the client cancelling it's token or the queue
        // shutting down cancels the request.
        var combinedTokenSource = _cancelSource.Token.CombineWith(requestCancellationToken);
        var combinedCancellationToken = combinedTokenSource.Token;
        var (item, resultTask) = QueueItem<TRequestContext>.Create(
            methodName,
            serializedRequest,
            lspServices,
            _logger,
            combinedCancellationToken);

        // Run a continuation to ensure the cts is disposed of.
        // We pass CancellationToken.None as we always want to dispose of the source
        // even when the request is cancelled or the queue is shutting down.
        _ = resultTask.ContinueWith(_ => combinedTokenSource.Dispose(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        var didEnqueue = _queue.TryEnqueue((item, Trace.CorrelationManager.ActivityId, combinedCancellationToken));

        // If the queue has been shut down the enqueue will fail, so we just fault the task immediately.
        // The queue itself is threadsafe (_queue.TryEnqueue and _queue.Complete use the same lock).
        if (!didEnqueue)
            return Task.FromException<object?>(new InvalidOperationException("Server was requested to shut down."));

        return resultTask;
    }

    private async Task ProcessQueueAsync()
    {
        ILspServices? lspServices = null;
        try
        {
            var concurrentlyExecutingTasks = new ConcurrentDictionary<Task, CancellationTokenSource>();

            while (!_cancelSource.IsCancellationRequested)
            {
                // First attempt to de-queue the work item in its own try-catch.
                // This is because before we de-queue we do not have access to the queue item's linked cancellation token.
                (IQueueItem<TRequestContext> work, Guid activityId, CancellationToken cancellationToken) queueItem;
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
                    var (work, activityId, cancellationToken) = queueItem;
                    CancellationTokenSource? currentWorkCts = null;
                    lspServices = work.LspServices;

                    if (CancelInProgressWorkUponMutatingRequest)
                    {
                        try
                        {
                            currentWorkCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
                        }
                        catch (ObjectDisposedException)
                        {
                            // Explicitly ignore this exception as this can occur during the CreateLinkTokenSource call, and means one of the
                            // linked cancellationTokens has been cancelled. If this occurs, skip to the next loop iteration as this 
                            // queueItem requires no processing
                            continue;
                        }

                        // Use the linked cancellation token so it's task can be cancelled if necessary during a mutating request
                        // on a queue that specifies CancelInProgressWorkUponMutatingRequest
                        cancellationToken = currentWorkCts.Token;
                    }

                    // Restore our activity id so that logging/tracking works across asynchronous calls.
                    Trace.CorrelationManager.ActivityId = activityId;

                    // Serially in the queue determine which language is appropriate for handling the request (based on the request URI).
                    //
                    // The client can send us the language associated with a URI in the didOpen notification.  It is important that all prior didOpen
                    // notifications have been completed by the time we attempt to determine the language, so we have the up to date map of URI to language.
                    // Since didOpen notifications are marked as mutating, the queue will not advance to the next request until the server has finished processing
                    // the didOpen, ensuring that this line will only run once all prior didOpens have completed.
                    var language = _languageServer.GetLanguageForRequest(work.MethodName, work.SerializedRequest);

                    // Now that we know the actual language, we can deserialize the request and start creating the request context.
                    var (metadata, handler, methodInfo) = GetHandlerForRequest(work, language);

                    // We now have the actual handler and language, so we can process the work item using the concrete types defined by the metadata.
                    await InvokeProcessCoreAsync(work, metadata, handler, methodInfo, concurrentlyExecutingTasks, currentWorkCts, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Explicitly ignore this exception as cancellation occurred as a result of our linked cancellation token.
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
                await _languageServer.ShutdownAsync("Error processing queue, shutting down").ConfigureAwait(false);
                await _languageServer.ExitAsync().ConfigureAwait(false);
            }

            await DisposeAsync().ConfigureAwait(false);
            return;
        }
    }

    /// <summary>
    /// Reflection invokes <see cref="ProcessQueueCoreAsync{TRequest, TResponse}(IQueueItem{TRequestContext}, IMethodHandler, RequestHandlerMetadata, ConcurrentDictionary{Task, CancellationTokenSource}, CancellationTokenSource?, CancellationToken)"/>
    /// using the concrete types defined by the handler's metadata.
    /// </summary>
    private async Task InvokeProcessCoreAsync(
        IQueueItem<TRequestContext> work,
        RequestHandlerMetadata metadata,
        IMethodHandler handler,
        MethodInfo methodInfo,
        ConcurrentDictionary<Task, CancellationTokenSource> concurrentlyExecutingTasks,
        CancellationTokenSource? currentWorkCts,
        CancellationToken cancellationToken)
    {
        var result = methodInfo.Invoke(this, [work, handler, metadata, concurrentlyExecutingTasks, currentWorkCts, cancellationToken]);
        if (result is null)
        {
            throw new InvalidOperationException($"ProcessQueueCoreAsync result task cannot be null");
        }

        var task = (Task)result;
        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Given a concrete handler and types, this dispatches the current work item to the appropriate handler,
    /// waiting or not waiting on results as defined by the handler.
    /// </summary>
    private async Task ProcessQueueCoreAsync<TRequest, TResponse>(
        IQueueItem<TRequestContext> work,
        IMethodHandler handler,
        RequestHandlerMetadata metadata,
        ConcurrentDictionary<Task, CancellationTokenSource> concurrentlyExecutingTasks,
        CancellationTokenSource? currentWorkCts,
        CancellationToken cancellationToken)
    {
        // The request context must be created serially inside the queue to so that requests always run
        // on the correct snapshot as of the last request.
        var contextInfo = await work.CreateRequestContextAsync<TRequest>(handler, metadata, _languageServer, cancellationToken).ConfigureAwait(false);
        if (contextInfo is null)
        {
            // We failed to create the context in a non-mutating request, we can't process this item so just return.
            return;
        }

        var (context, deserializedRequest) = contextInfo.Value;

        // Run anything in before request before we start handling the request (for example setting the UI culture).
        BeforeRequest(deserializedRequest);

        if (handler.MutatesSolutionState)
        {
            if (CancelInProgressWorkUponMutatingRequest)
            {
                // Cancel all concurrently executing tasks
                var concurrentlyExecutingTasksArray = concurrentlyExecutingTasks.ToArray();
                for (var i = 0; i < concurrentlyExecutingTasksArray.Length; i++)
                {
                    concurrentlyExecutingTasksArray[i].Value.Cancel();
                }

                // wait for all pending tasks to complete their cancellation, ignoring any exceptions
                await Task.WhenAll(concurrentlyExecutingTasksArray.Select(kvp => kvp.Key)).NoThrowAwaitable(captureContext: false);
            }

            Debug.Assert(!concurrentlyExecutingTasks.Any(t => !t.Key.IsCompleted), "The tasks should have all been drained before continuing");
            // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
            // Since we're explicitly awaiting exceptions to mutating requests will bubble up here.
            await WrapStartRequestTaskAsync(work.StartRequestAsync<TRequest, TResponse>(deserializedRequest, context, handler, metadata.Language, cancellationToken), rethrowExceptions: true).ConfigureAwait(false);
        }
        else
        {
            // Non mutating are fire-and-forget because they are by definition read-only. Any errors
            // will be sent back to the client but they can also be captured via HandleNonMutatingRequestError,
            // though these errors don't put us into a bad state as far as the rest of the queue goes.
            // Furthermore we use Task.Run here to protect ourselves against synchronous execution of work
            // blocking the request queue for longer periods of time (it enforces parallelizability).
            var currentWorkTask = WrapStartRequestTaskAsync(Task.Run(() => work.StartRequestAsync<TRequest, TResponse>(deserializedRequest, context, handler, metadata.Language, cancellationToken), cancellationToken), rethrowExceptions: false);

            if (CancelInProgressWorkUponMutatingRequest)
            {
                if (currentWorkCts is null)
                {
                    throw new InvalidOperationException($"unexpected null value for {nameof(currentWorkCts)}");
                }

                if (!concurrentlyExecutingTasks.TryAdd(currentWorkTask, currentWorkCts))
                {
                    throw new InvalidOperationException($"unable to add {nameof(currentWorkTask)} into {nameof(concurrentlyExecutingTasks)}");
                }

                _ = currentWorkTask.ContinueWith(t =>
                {
                    if (!concurrentlyExecutingTasks.TryRemove(t, out var concurrentlyExecutingTaskCts))
                    {
                        throw new InvalidOperationException($"unexpected failure to remove task from {nameof(concurrentlyExecutingTasks)}");
                    }

                    concurrentlyExecutingTaskCts.Dispose();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }
    }

    /// <summary>
    /// Allows an action to happen before the request runs, for example setting the current thread culture.
    /// </summary>
    protected internal virtual void BeforeRequest<TRequest>(TRequest request)
    {
        return;
    }

    private (RequestHandlerMetadata Metadata, IMethodHandler Handler, MethodInfo MethodInfo) GetHandlerForRequest(IQueueItem<TRequestContext> work, string language)
    {
        var handlersForMethod = _handlerInfoMap[work.MethodName];
        if (handlersForMethod.TryGetValue(language, out var lazyData) ||
            handlersForMethod.TryGetValue(LanguageServerConstants.DefaultLanguageName, out lazyData))
        {
            return lazyData.Value;
        }

        throw new InvalidOperationException($"Missing default or language handler for {work.MethodName} and language {language}");
    }

    /// <summary>
    /// Provides an extensibility point to log or otherwise inspect errors thrown from non-mutating requests,
    /// which would otherwise be lost to the fire-and-forget task in the queue.
    /// </summary>
    /// <param name="nonMutatingRequestTask">The task to be inspected.</param>
    /// <param name="rethrowExceptions">If exceptions should be re-thrown.</param>
    /// <returns>The task from <paramref name="nonMutatingRequestTask"/>, to allow chained calls if needed.</returns>
    public virtual Task WrapStartRequestTaskAsync(Task nonMutatingRequestTask, bool rethrowExceptions)
    {
        return nonMutatingRequestTask;
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

        public AbstractHandlerProvider GetHandlerProvider() => _queue._handlerProvider;

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
                var (_, _, cancellationToken) = await _queue._queue.DequeueAsync().ConfigureAwait(false);
                if (!cancellationToken.IsCancellationRequested)
                    return false;
            }

            return true;
        }
    }
    #endregion
}
