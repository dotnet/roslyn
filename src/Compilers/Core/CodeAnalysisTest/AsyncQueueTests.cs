// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            Assert.Throws<InvalidOperationException>(() => queue.Enqueue(42));
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
            Assert.Throws<InvalidOperationException>(() =>
            {
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
            Assert.Equal(13, await task);
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
                Assert.Equal(i, await task);
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
                await task;
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
                    await task;
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
            await queue.WhenCompletedTask;
            Assert.Equal(42, await queue.DequeueAsync());

            var threw = false;
            try
            {
                await queue.DequeueAsync();
            }
            catch (OperationCanceledException)
            {
                threw = true;
            }
            Assert.True(threw);
        }

        [Fact]
        public async Task DequeueAsyncWithCancellation()
        {
            var queue = new AsyncQueue<int>();
            var cts = new CancellationTokenSource();
            var task = queue.DequeueAsync(cts.Token);
            Assert.False(task.IsCanceled);
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Fact]
        public async Task EnqueueAfterDequeueAsyncWithCancellation()
        {
            var queue = new AsyncQueue<int>();
            var cts = new CancellationTokenSource();
            var task = queue.DequeueAsync(cts.Token);
            Assert.False(task.IsCanceled);
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
            Assert.Equal(TaskStatus.Canceled, task.Status);

            queue.Enqueue(1);
            Assert.True(queue.TryDequeue(out var value));
            Assert.Equal(1, value);
            Assert.False(queue.IsCompleted);
        }

        [Fact]
        public async Task DequeueAsyncWithCancellationAfterComplete()
        {
            var queue = new AsyncQueue<int>();
            var cts = new CancellationTokenSource();
            var task = queue.DequeueAsync(cts.Token);
            Assert.False(task.IsCompleted);
            queue.Enqueue(42);
            await task;
            cts.Cancel();
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
            await queue.WhenCompletedTask;

            int value;
            Assert.True(queue.TryDequeue(out value));
            Assert.Equal(13, value);
        }
    }
}
