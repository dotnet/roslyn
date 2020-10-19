// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
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
    /// it is added to a queue, and a queue item will not be retreived while a mutating request is running. Before
    /// any request is handled the solution state is created by merging workspace solution state, which could have
    /// changes from non-LSP means (eg, adding a project reference), with the current "mutated" state.
    /// When a non-mutating work item is retrieved from the queue, it is given the current solution state, but then
    /// run in a fire-and-forget fashion.
    /// </para>
    /// <para>
    /// Regardless of whether a request is mutating or not, or blocking or not, is an implementation detail of this class
    /// and any consumers observing the results of the task returned from <see cref="ExecuteAsync{TRequestType, TResponseType}(bool, IRequestHandler{TRequestType, TResponseType}, TRequestType, ClientCapabilities, string?, CancellationToken)"/>
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
        private readonly ILspSolutionProvider _solutionProvider;
        private readonly AsyncQueue<QueueItem> _queue;
        private readonly CancellationTokenSource _cancelSource;

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

        public RequestExecutionQueue(ILspSolutionProvider solutionProvider)
        {
            _solutionProvider = solutionProvider;
            _queue = new AsyncQueue<QueueItem>();
            _cancelSource = new CancellationTokenSource();

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
        /// <param name="requestCancellationToken">A cancellation token that will cancel the handing of this request.
        /// The request could also be cancelled by the queue shutting down.</param>
        /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
        public Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(
            bool mutatesSolutionState,
            IRequestHandler<TRequestType, TResponseType> handler,
            TRequestType request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken requestCancellationToken) where TRequestType : class
        {
            // Create a task completion source that will represent the processing of this request to the caller
            var completion = new TaskCompletionSource<TResponseType>();

            // Note: If the queue is not accepting any more items then TryEnqueue below will fail.

            var textDocument = handler.GetTextDocumentIdentifier(request);
            var item = new QueueItem(mutatesSolutionState, clientCapabilities, clientName, textDocument,
                callbackAsync: async (context, cancellationToken) =>
                {
                    // Check if cancellation was requested while this was waiting in the queue
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.SetCanceled();

                        // Tell the queue to ignore any mutations from this request, not that we've given it a chance
                        // to make any
                        return false;
                    }

                    try
                    {
                        var result = await handler.HandleRequestAsync(request, context, cancellationToken).ConfigureAwait(false);
                        completion.SetResult(result);
                        // Tell the queue that this was successful so that mutations (if any) can be applied
                        return true;
                    }
                    catch (OperationCanceledException ex)
                    {
                        completion.TrySetCanceled(ex.CancellationToken);
                    }
                    catch (Exception exception)
                    {
                        // Pass the exception to the task completion source, so the caller of the ExecuteAsync method can observe but
                        // don't let it escape from this callback, so it doesn't affect the queue processing.
                        completion.SetException(exception);
                    }

                    // Tell the queue to ignore any mutations from this request
                    return false;
                }, requestCancellationToken);

            var didEnqueue = _queue.TryEnqueue(item);

            // If the queue has been shut down the enqueue will fail, so we just fault the task immediately.
            // The queue itself is threadsafe (_queue.TryEnqueue and _queue.Complete use the same lock).
            if (!didEnqueue)
            {
                completion.SetException(new InvalidOperationException("Server was requested to shut down."));
            }

            return completion.Task;
        }

        private async Task ProcessQueueAsync()
        {
            // Keep track of solution state modifications made by LSP requests
            Solution? lastMutatedSolution = null;

            try
            {
                while (!_cancelSource.IsCancellationRequested)
                {
                    var work = await _queue.DequeueAsync().ConfigureAwait(false);

                    // Create a linked cancellation token to cancel any requests in progress when this shuts down
                    var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(_cancelSource.Token, work.CancellationToken).Token;

                    // The "current" solution can be updated by non-LSP actions, so we need it, but we also need
                    // to merge in the changes from any mutations that have been applied to open documents
                    var (document, solution) = _solutionProvider.GetDocumentAndSolution(work.TextDocument, work.ClientName);
                    solution = MergeChanges(solution, lastMutatedSolution);

                    if (work.MutatesSolutionState)
                    {
                        Solution? mutatedSolution = null;
                        var context = new RequestContext(solution, work.ClientCapabilities, work.ClientName, document, s => mutatedSolution = s);

                        // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                        var ranToCompletion = false;
                        try
                        {
                            ranToCompletion = await work.CallbackAsync(context, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Since the cancellationToken passed to the callback is a linked source it could be cancelled
                            // without the queue token being cancelled, but ranToCompletion will be false so no need to
                            // do anything special here.
                            RoslynDebug.Assert(ranToCompletion == false);
                        }

                        // If the handling of the request failed, the exception will bubble back up to the caller, but we
                        // still need to react to it here by throwing away solution updates
                        if (ranToCompletion)
                        {
                            lastMutatedSolution = mutatedSolution ?? lastMutatedSolution;
                        }
                        else
                        {
                            OnRequestServerShutdown($"An error occured processing a mutating request and the solution is in an invalid state. Check LSP client logs for any error information.");
                            break;
                        }
                    }
                    else
                    {
                        var context = new RequestContext(solution, work.ClientCapabilities, work.ClientName, document, null);

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
                OnRequestServerShutdown($"Error occured processing queue: {e.Message}.");
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

        private static Solution MergeChanges(Solution solution, Solution? mutatedSolution)
        {
            // TODO: Merge in changes to the solution that have been received from didChange LSP methods
            // https://github.com/dotnet/roslyn/issues/45427
            return mutatedSolution ?? solution;
        }
    }
}
