// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
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
    /// </remarks>
    internal partial class RequestExecutionQueue : IDisposable
    {
        private readonly ILspSolutionProvider _solutionProvider;
        private AsyncQueue<QueueItem>? _queue;
        private CancellationTokenSource? _cancelSource;

        public RequestExecutionQueue(ILspSolutionProvider solutionProvider)
        {
            _solutionProvider = solutionProvider;
        }

        public void Initialize()
        {
            // If the queue is already running, do nothing because we don't want to run multiple or lose any queued messages
            if (_cancelSource == null || _cancelSource.IsCancellationRequested)
            {
                _queue = new AsyncQueue<QueueItem>();
                _cancelSource = new CancellationTokenSource();

                // Start the queue processing
                _ = ProcessQueueAsync();
            }
        }

        public void Shutdown()
        {
            _queue = null;
            _cancelSource?.Cancel();
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

            // If the queue is not set up, we just fauly immediately
            if (_queue == null)
            {
                completion.SetException(new InvalidOperationException("Server has not been initialized, or was requested to shut down."));
                return completion.Task;
            }

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
                    catch (OperationCanceledException)
                    {
                        completion.SetCanceled();
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
            _queue.Enqueue(item);

            return completion.Task;
        }

        private async Task ProcessQueueAsync()
        {
            Contract.ThrowIfNull(_cancelSource, "Queue should not run without a cancellation token source to stop it");

            // Keep track of solution state modifications made by LSP requests
            Solution? lastMutatedSolution = null;
            var queueToken = _cancelSource.Token;

            while (!queueToken.IsCancellationRequested)
            {
                var work = await _queue.DequeueAsync().ConfigureAwait(false);

                // Create a linked cancellation token to cancel any requests in progress when this shuts down
                var cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(queueToken, work.CancellationToken).Token;

                // The "current" solution can be updated by non-LSP actions, so we need it, but we also need
                // to merge in the changes from any mutations that have been applied to open documents
                var (document, solution) = _solutionProvider.GetDocumentAndSolution(work.TextDocument, work.ClientName);
                solution = MergeChanges(solution, lastMutatedSolution);

                Solution? mutatedSolution = null;
                var context = new RequestContext(solution, work.ClientCapabilities, work.ClientName, document, s => mutatedSolution = s);

                if (work.MutatesSolutionState)
                {
                    // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                    var ranToCompletion = await work.CallbackAsync(context, cancellationToken).ConfigureAwait(false);

                    // If the handling of the request failed, the exception will bubble back up to the caller, but we
                    // still need to react to it here by throwing away solution updates
                    if (ranToCompletion)
                    {
                        lastMutatedSolution = mutatedSolution ?? lastMutatedSolution;
                    }
                }
                else
                {
                    // Non mutating request get given the current solution state, but are otherwise fire-and-forget
                    _ = work.CallbackAsync(context, cancellationToken);
                }
            }
        }

        private static Solution MergeChanges(Solution solution, Solution? mutatedSolution)
        {
            // TODO: Merge in changes to the solution that have been received from didChange LSP methods
            // https://github.com/dotnet/roslyn/issues/45427
            return mutatedSolution ?? solution;
        }

        public void Dispose()
        {
            _cancelSource?.Cancel();
            _cancelSource?.Dispose();
        }
    }
}
