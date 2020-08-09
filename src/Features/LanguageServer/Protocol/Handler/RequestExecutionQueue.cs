// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Export(typeof(RequestExecutionQueue)), Shared]
    internal partial class RequestExecutionQueue
    {
        private readonly AsyncQueue<QueueItem> _queue = new AsyncQueue<QueueItem>();
        private readonly ILspSolutionProvider _solutionProvider;
        private Solution? _lastMutatedSolution;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RequestExecutionQueue(ILspSolutionProvider solutionProvider)
        {
            _solutionProvider = solutionProvider;

            // Start the queue processing
            _ = ProcessQueueAsync();
        }

        /// <summary>
        /// Queues a request to be handled by the specified handler, with mutating requests blocking future requests
        /// from starting until the mutation is complete.
        /// </summary>
        /// <param name="mutatesSolutionState">Whether or not the specified request needs to mutate the solution.</param>
        /// <param name="handler">The handler that will handle the request.</param>
        /// <param name="request">The request to hanel.</param>
        /// <param name="clientCapabilities">The client capabilities.</param>
        /// <param name="clientName">The client name.</param>
        /// <param name="cancellationToken">A cancellation token that will cancel the handing of this request.</param>
        /// <returns>A task that can be awaited to observe the results of the handing of this request.</returns>
        public Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(bool mutatesSolutionState, IRequestHandler<TRequestType, TResponseType> handler, TRequestType request,
            ClientCapabilities clientCapabilities, string? clientName, CancellationToken cancellationToken) where TRequestType : class
        {
            // Create a task completion source that will represent the processing of this request to the caller
            var completion = new TaskCompletionSource<TResponseType>();

            var item = new QueueItem(mutatesSolutionState, clientCapabilities, clientName,
                callback: async (context) =>
                {
                    // Check if cancellation was requested while this was waiting in the queue
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.SetCanceled();
                        return false;
                    }

                    try
                    {
                        var result = await handler.HandleRequestAsync(request, context, cancellationToken).ConfigureAwait(false);
                        completion.SetResult(result);
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                        completion.SetCanceled();
                    }
                    catch (Exception exception)
                    {
                        // Pass the exception to the task completion source, so the caller of the ExecuteAsync method can observe but
                        // don't let it escape from this callback, so it doens't affect the queue processing.
                        completion.SetException(exception);
                    }
                    return false;
                });
            _queue.Enqueue(item);

            return completion.Task;
        }

        private async Task ProcessQueueAsync()
        {
            while (true)
            {
                var work = await _queue.DequeueAsync().ConfigureAwait(false);

                var solution = GetCurrentSolution();
                solution = MergeChanges(solution, _lastMutatedSolution);

                Solution? mutatedSolution = null;
                var context = new RequestContext(solution, s => mutatedSolution = s, work.ClientCapabilities, work.ClientName);

                if (work.MutatesSolutionState)
                {
                    // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                    var ranToCompletion = await work.Callback(context).ConfigureAwait(false);

                    // If the handling of the request failed, the exception will bubble back up to the caller, but we
                    // still need to react to it here by throwing away solution updates
                    if (ranToCompletion)
                    {
                        _lastMutatedSolution = mutatedSolution ?? _lastMutatedSolution;
                    }
                }
                else
                {
                    // Non mutating request get given the current solution state, but are otherwise fire-and-forget
                    _ = work.Callback(context);
                }
            }
        }

        private static Solution MergeChanges(Solution solution, Solution? mutatedSolution)
        {
            // TODO: Merge in changes to the solution that have been received from didChange LSP methods
            // https://github.com/dotnet/roslyn/issues/45427
            return mutatedSolution ?? solution;
        }

        private Solution GetCurrentSolution()
            => _solutionProvider.GetCurrentSolutionForMainWorkspace();
    }
}
