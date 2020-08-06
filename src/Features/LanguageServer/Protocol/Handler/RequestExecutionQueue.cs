// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        private readonly AsyncQueue<QueueItem> _queue = new AsyncQueue<QueueItem>();

        public RequestExecutionQueue()
        {
            _ = ProcessQueueAsync();
        }

        public Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(bool mutatesSolutionState, IRequestHandler<TRequestType, TResponseType> handler, TRequestType request,
            ClientCapabilities clientCapabilities, string clientName, CancellationToken cancellationToken) where TRequestType : class
        {
            var completion = new TaskCompletionSource<TResponseType>();

            var item = new QueueItem(mutatesSolutionState, clientCapabilities, clientName,
                callback: async (context) =>
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
                    catch (OperationCanceledException)
                    {
                        completion.SetCanceled();
                    }
                    catch (Exception exception)
                    {
                        completion.SetException(exception);
                    }
                });
            _queue.Enqueue(item);

            return completion.Task;
        }

        private async Task ProcessQueueAsync()
        {
            while (true)
            {

                var work = await _queue.DequeueAsync().ConfigureAwait(false);

                var context = CreateContext(work);

                if (work.MutatesSolutionState)
                {
                    // Mutating requests block other requests from starting to ensure an up to date snapshot is used.
                    await work.Callback(context).ConfigureAwait(false);
                }
                else
                {
                    _ = work.Callback(context);
                }
            }
        }

        private static RequestContext CreateContext(QueueItem work)
            => new RequestContext(work.ClientCapabilities, work.ClientName);
    }
}
