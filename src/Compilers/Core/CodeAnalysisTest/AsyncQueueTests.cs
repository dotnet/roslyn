// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AsyncQueueTests
    {
        [Fact]
        public void Enqueue()
        {
            var queue = new AsyncQueue<int>();
            queue.Enqueue(42);
            var task = queue.DequeueAsync();
            Assert.True(task.IsCompleted);
            Assert.Equal(42, task.Result);
        }

        [Fact]
        public void EnqueueAfterComplete()
        {
            var queue = new AsyncQueue<int>();
            queue.Complete();
            Assert.Throws(typeof(InvalidOperationException), () => queue.Enqueue(42));
        }

        [Fact]
        public void TryEnqueueAfterComplete()
        {
            var queue = new AsyncQueue<int>();
            Assert.True(queue.TryEnqueue(42));
            queue.Complete();
            Assert.False(queue.TryEnqueue(42));
        }

        [Fact]
        public void TryEnqueueAfterPromisingNotTo()
        {
            var queue = new AsyncQueue<int>();
            Assert.True(queue.TryEnqueue(42));
            queue.PromiseNotToEnqueue();
            Assert.Throws(typeof(InvalidOperationException), () => {
                queue.TryEnqueue(42);
            });
        }

        [Fact]
        public async Task DequeueThenEnqueue()
        {
            var queue = new AsyncQueue<int>();
            var task = queue.DequeueAsync();
            Assert.False(task.IsCompleted);
            queue.Enqueue(13);
            Assert.Equal(13, await task.ConfigureAwait(false));
        }

        [Fact]
        public async Task DequeueManyThenEnqueueMany()
        {
            var queue = new AsyncQueue<int>();
            var count = 4;
            var list = new List<Task<int>>();

            for (var i = 0; i < count; i++)
            {
                list.Add(queue.DequeueAsync());
            }

            for (var i = 0; i < count; i++)
            {
                var task = list[i];
                Assert.False(task.IsCompleted);
                queue.Enqueue(i);
                Assert.Equal(i, await task.ConfigureAwait(false));
            }
        }

        [Fact]
        public async Task DequeueThenComplete()
        {
            var queue = new AsyncQueue<int>();
            var task = queue.DequeueAsync();
            Assert.False(task.IsCompleted);

            queue.Complete();
            var threw = false;
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                threw = true;
            }

            Assert.True(threw);
        }

        [Fact]
        public async Task DequeueManyThenComplete()
        {
            var queue = new AsyncQueue<int>();
            var list = new List<Task<int>>();
            for (var i = 0; i < 4; i++)
            {
                list.Add(queue.DequeueAsync());
            }

            queue.Complete();
            foreach (var task in list)
            {
                var threw = false;
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    threw = true;
                }

                Assert.True(threw);
            }
        }

        [Fact]
        public async Task DequeueAfterCompleteWithData()
        {
            var queue = new AsyncQueue<int>();
            queue.Enqueue(42);
            queue.Complete();
            await queue.WhenCompletedTask.ConfigureAwait(false);
            Assert.Equal(42, await queue.DequeueAsync().ConfigureAwait(false));

            var threw = false;
            try
            {
                await queue.DequeueAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                threw = true;
            }
            Assert.True(threw);
        }

        [Fact]
        public void DequeueAsyncWithCancellation()
        {
            var queue = new AsyncQueue<int>();
            var cts = new CancellationTokenSource();
            var task = queue.DequeueAsync(cts.Token);
            Assert.False(task.IsCanceled);
            cts.Cancel();
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Fact]
        public async Task DequeueAsyncWithCancellationAfterComplete()
        {
            var queue = new AsyncQueue<int>();
            var cts = new CancellationTokenSource();
            var task = queue.DequeueAsync(cts.Token);
            Assert.False(task.IsCompleted);
            queue.Enqueue(42);
            await task.ConfigureAwait(false);
            cts.Cancel();
        }

        [Fact]
        public async Task TaskCompletesAsyncWithComplete()
        {
            var queue = new AsyncQueue<int>();

            var tcs = new TaskCompletionSource<bool>();
            var task = queue.DequeueAsync().ContinueWith(
                t =>
                {
                    tcs.Task.Wait();
                    return 0;
                },
                default(CancellationToken),
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            queue.Complete();
            Assert.False(queue.WhenCompletedTask.IsCompleted);
            tcs.SetResult(true);
            await queue.WhenCompletedTask.ConfigureAwait(false);

            // The AsyncQueue<T>.Task property won't complete until all of the 
            // existing DequeueAsync values have also completed.
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void TryDequeue()
        {
            var queue = new AsyncQueue<int>();
            int value;
            Assert.False(queue.TryDequeue(out value));
            queue.Enqueue(13);
            Assert.True(queue.TryDequeue(out value));
            Assert.Equal(13, value);
        }

        /// <summary>
        /// The analyzer framework explicitly depends on the ability to dequeue existing values
        /// after the <see cref="AsyncQueue{TElement}"/> is completed.
        /// </summary>
        [Fact]
        public async Task TryDequeueAfterComplete()
        {
            var queue = new AsyncQueue<int>();
            queue.Enqueue(13);
            queue.Complete();
            await queue.WhenCompletedTask.ConfigureAwait(false);

            int value;
            Assert.True(queue.TryDequeue(out value));
            Assert.Equal(13, value);
        }
    }
}
