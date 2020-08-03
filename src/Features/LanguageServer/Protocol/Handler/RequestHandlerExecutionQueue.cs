// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RequestHandlerExecutionQueue
    {
        private class QueueItem
        {
            public Func<Task> Callback { get; set; }
            public RequestProcessingMode Type { get; set; }
        }

        private readonly AsyncQueue<QueueItem> _queue;

        public RequestHandlerExecutionQueue()
        {
            _queue = new AsyncQueue<QueueItem>();
            _ = ProcessQueueAsync();
        }

        internal Task<TResult> ExecuteAsync<TResult>(RequestProcessingMode type, Func<Task<TResult>> asyncFunction)
        {
            var completion = new TaskCompletionSource<TResult>();

            var item = new QueueItem
            {
                Type = type,
                Callback = async () =>
                {
                    try
                    {
                        var result = await asyncFunction().ConfigureAwait(false);
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
                },
            };
            _queue.Enqueue(item);

            return completion.Task;
        }

        private async Task ProcessQueueAsync()
        {
            var runningParallelTasks = new List<Task>();

            while (true)
            {
                var work = await _queue.DequeueAsync().ConfigureAwait(false);

                if (work.Type == RequestProcessingMode.Serial)
                {
                    // Wait for any parallel requests to finish first
                    await Task.WhenAll(runningParallelTasks).ConfigureAwait(false);
                    runningParallelTasks.Clear();

                    // Run serial work and block
                    await work.Callback().ConfigureAwait(false);
                }
                else
                {
                    runningParallelTasks.Add(work.Callback());
                }
            }
        }
    }
}
