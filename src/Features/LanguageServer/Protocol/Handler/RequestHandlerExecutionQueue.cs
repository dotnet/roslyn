using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal class RequestHandlerExecutionQueue
    {
        private class QueueItem
        {
            public Func<object, Task> Callback { get; internal set; }
            public object State { get; internal set; }
            public RequestProcessingMode Type { get; internal set; }
        }

        private readonly AsyncQueue<QueueItem> _queue;

        public RequestHandlerExecutionQueue()
        {
            _queue = new AsyncQueue<QueueItem>();
            _ = ProcessQueueAsync();
        }

        internal Task<TResult> ExecuteParallel<TResult>(Func<Task<TResult>> asyncFunction)
        {
            var completion = new RequestHandlerSynchronizationTaskCompletionSource<Func<Task<TResult>>, TResult>(asyncFunction);

            Enqueue(RequestProcessingMode.Parallel, async (state) =>
            {
                var completion = (RequestHandlerSynchronizationTaskCompletionSource<Func<Task<TResult>>, TResult>)state;
                try
                {
                    var result = await completion.Callback().ConfigureAwait(false);
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
            }, completion);

            return completion.Task;
        }

        internal Task<TResult> ExecuteSerial<TResult>(Func<Task<TResult>> asyncFunction)
        {
            var completion = new RequestHandlerSynchronizationTaskCompletionSource<Func<Task<TResult>>, TResult>(asyncFunction);

            Enqueue(RequestProcessingMode.Serial, async (state) =>
            {
                var completion = (RequestHandlerSynchronizationTaskCompletionSource<Func<Task<TResult>>, TResult>)state;
                try
                {
                    var result = await completion.Callback().ConfigureAwait(false);
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
            }, completion);

            return completion.Task;
        }

        private void Enqueue(RequestProcessingMode type, Func<object, Task> p, object state)
        {
            var item = new QueueItem
            {
                Type = type,
                Callback = p,
                State = state
            };
            _queue.Enqueue(item);
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

                    await work.Callback(work.State).ConfigureAwait(false);
                }
                else
                {
                    runningParallelTasks.Add(work.Callback(work.State));
                }
            }
        }

        internal class RequestHandlerSynchronizationTaskCompletionSource<TCallback, TResult> : TaskCompletionSource<TResult>
        {
            public RequestHandlerSynchronizationTaskCompletionSource(TCallback callback)
            {
                Callback = callback;
            }

            public TCallback Callback { get; }
        }
    }
}
