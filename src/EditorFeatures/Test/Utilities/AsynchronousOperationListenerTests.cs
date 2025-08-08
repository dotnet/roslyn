// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;

public sealed class AsynchronousOperationListenerTests
{
    /// <summary>
    /// A delay which is not expected to be reached in practice.
    /// </summary>
    private static readonly TimeSpan s_unexpectedDelay = TimeSpan.FromSeconds(60);

    private sealed class SleepHelper : IDisposable
    {
        private readonly CancellationTokenSource _tokenSource;
        private readonly List<Task> _tasks = [];

        public SleepHelper()
            => _tokenSource = new CancellationTokenSource();

        public void Dispose()
        {
            Task[] tasks;
            lock (_tasks)
            {
                _tokenSource.Cancel();
                tasks = [.. _tasks];
            }

            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException e)
            {
                foreach (var inner in e.InnerExceptions)
                {
                    Assert.IsType<TaskCanceledException>(inner);
                }
            }

            GC.SuppressFinalize(this);
        }

        public void Sleep(TimeSpan timeToSleep)
        {
            Task task;
            lock (_tasks)
            {
                task = Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        _tokenSource.Token.ThrowIfCancellationRequested();
                        Thread.Sleep(TimeSpan.FromMilliseconds(10));
                    }
                }, _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                _tasks.Add(task);
            }

            task.Wait((int)timeToSleep.TotalMilliseconds);
        }
    }

    [Fact]
    public void Operation()
    {
        using var sleepHelper = new SleepHelper();
        var signal = new ManualResetEventSlim();
        var listener = new AsynchronousOperationListener();

        var done = false;
        var asyncToken = listener.BeginAsyncOperation("Test");
        var task = new Task(() =>
            {
                signal.Set();
                sleepHelper.Sleep(TimeSpan.FromSeconds(1));
                done = true;
            });
        task.CompletesAsyncOperation(asyncToken);
        task.Start(TaskScheduler.Default);

        Wait(listener, signal);
        Assert.True(done, "The operation should have completed");
    }

    [Fact]
    public void QueuedOperation()
    {
        using var sleepHelper = new SleepHelper();
        var signal = new ManualResetEventSlim();
        var listener = new AsynchronousOperationListener();

        var done = false;
        var asyncToken1 = listener.BeginAsyncOperation("Test");
        var task = new Task(() =>
            {
                signal.Set();
                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));

                var asyncToken2 = listener.BeginAsyncOperation("Test");
                var queuedTask = new Task(() =>
                    {
                        sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                        done = true;
                    });
                queuedTask.CompletesAsyncOperation(asyncToken2);
                queuedTask.Start(TaskScheduler.Default);
            });

        task.CompletesAsyncOperation(asyncToken1);
        task.Start(TaskScheduler.Default);

        Wait(listener, signal);

        Assert.True(done, "Should have waited for the queued operation to finish!");
    }

    [Fact]
    public void Cancel()
    {
        using var sleepHelper = new SleepHelper();
        var signal = new ManualResetEventSlim();
        var listener = new AsynchronousOperationListener();

        var done = false;
        var continued = false;
        var asyncToken1 = listener.BeginAsyncOperation("Test");
        var task = new Task(() =>
            {
                signal.Set();
                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                var asyncToken2 = listener.BeginAsyncOperation("Test");
                var queuedTask = new Task(() =>
                    {
                        sleepHelper.Sleep(s_unexpectedDelay);
                        continued = true;
                    });
                asyncToken2.Dispose();
                queuedTask.Start(TaskScheduler.Default);
                done = true;
            });
        task.CompletesAsyncOperation(asyncToken1);
        task.Start(TaskScheduler.Default);

        Wait(listener, signal);

        Assert.True(done, "Cancelling should have completed the current task.");
        Assert.False(continued, "Continued Task when it shouldn't have.");
    }

    [Fact]
    public void Nested()
    {
        using var sleepHelper = new SleepHelper();
        var signal = new ManualResetEventSlim();
        var listener = new AsynchronousOperationListener();

        var outerDone = false;
        var innerDone = false;
        var asyncToken1 = listener.BeginAsyncOperation("Test");
        var task = new Task(() =>
            {
                signal.Set();
                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));

                using (listener.BeginAsyncOperation("Test"))
                {
                    sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                    innerDone = true;
                }

                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                outerDone = true;
            });
        task.CompletesAsyncOperation(asyncToken1);
        task.Start(TaskScheduler.Default);

        Wait(listener, signal);

        Assert.True(innerDone, "Should have completed the inner task");
        Assert.True(outerDone, "Should have completed the outer task");
    }

    [Fact]
    public void MultipleEnqueues()
    {
        using var sleepHelper = new SleepHelper();
        var signal = new ManualResetEventSlim();
        var listener = new AsynchronousOperationListener();

        var outerDone = false;
        var firstQueuedDone = false;
        var secondQueuedDone = false;

        var asyncToken1 = listener.BeginAsyncOperation("Test");
        var task = new Task(() =>
            {
                signal.Set();
                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));

                var asyncToken2 = listener.BeginAsyncOperation("Test");
                var firstQueueTask = new Task(() =>
                    {
                        sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                        var asyncToken3 = listener.BeginAsyncOperation("Test");
                        var secondQueueTask = new Task(() =>
                            {
                                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                                secondQueuedDone = true;
                            });
                        secondQueueTask.CompletesAsyncOperation(asyncToken3);
                        secondQueueTask.Start(TaskScheduler.Default);
                        firstQueuedDone = true;
                    });
                firstQueueTask.CompletesAsyncOperation(asyncToken2);
                firstQueueTask.Start(TaskScheduler.Default);
                outerDone = true;
            });
        task.CompletesAsyncOperation(asyncToken1);
        task.Start(TaskScheduler.Default);

        Wait(listener, signal);

        Assert.True(outerDone, "The outer task should have finished!");
        Assert.True(firstQueuedDone, "The first queued task should have finished");
        Assert.True(secondQueuedDone, "The second queued task should have finished");
    }

    [Fact]
    public void IgnoredCancel()
    {
        using var sleepHelper = new SleepHelper();
        var signal = new ManualResetEventSlim();
        var listener = new AsynchronousOperationListener();

        var done = new TaskCompletionSource<VoidResult>();
        var queuedFinished = new TaskCompletionSource<VoidResult>();
        var cancelledFinished = new TaskCompletionSource<VoidResult>();
        var asyncToken1 = listener.BeginAsyncOperation("Test");
        var task = new Task(() =>
        {
            using (listener.BeginAsyncOperation("Test"))
            {
                var cancelledTask = new Task(() =>
                {
                    sleepHelper.Sleep(TimeSpan.FromSeconds(10));
                    cancelledFinished.SetResult(default);
                });

                signal.Set();
                cancelledTask.Start(TaskScheduler.Default);
            }

            // Now that we've canceled the first request, queue another one to make sure we wait for it.
            var asyncToken2 = listener.BeginAsyncOperation("Test");
            var queuedTask = new Task(() =>
            {
                queuedFinished.SetResult(default);
            });
            queuedTask.CompletesAsyncOperation(asyncToken2);
            queuedTask.Start(TaskScheduler.Default);
            done.SetResult(default);
        });
        task.CompletesAsyncOperation(asyncToken1);
        task.Start(TaskScheduler.Default);

        Wait(listener, signal);

        Assert.True(done.Task.IsCompleted, "Cancelling should have completed the current task.");
        Assert.True(queuedFinished.Task.IsCompleted, "Continued didn't run, but it was supposed to ignore the cancel.");
        Assert.False(cancelledFinished.Task.IsCompleted, "We waited for the cancelled task to finish.");
    }

    [Fact]
    public void SecondCompletion()
    {
        using var sleepHelper = new SleepHelper();
        var signal1 = new ManualResetEventSlim();
        var signal2 = new ManualResetEventSlim();
        var listener = new AsynchronousOperationListener();

        var firstDone = false;
        var secondDone = false;

        var asyncToken1 = listener.BeginAsyncOperation("Test");
        var firstTask = Task.Factory.StartNew(() =>
            {
                signal1.Set();
                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                firstDone = true;
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        firstTask.CompletesAsyncOperation(asyncToken1);
        firstTask.Wait();

        var asyncToken2 = listener.BeginAsyncOperation("Test");
        var secondTask = Task.Factory.StartNew(() =>
            {
                signal2.Set();
                sleepHelper.Sleep(TimeSpan.FromMilliseconds(500));
                secondDone = true;
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        secondTask.CompletesAsyncOperation(asyncToken2);

        // give it two signals since second one might not have started when WaitTask.Wait is called - race condition
        Wait(listener, signal1, signal2);

        Assert.True(firstDone, "First didn't finish");
        Assert.True(secondDone, "Should have waited for the second task");
    }

    private static void Wait(AsynchronousOperationListener listener, ManualResetEventSlim signal)
    {
        // Note: WaitTask will return immediately if there is no outstanding work.  Due to
        // threadpool scheduling, we may get here before that other thread has started to run.
        // That's why each task set's a signal to say that it has begun and we first wait for
        // that, and then start waiting.
        Assert.True(signal.Wait(s_unexpectedDelay), "Shouldn't have hit timeout waiting for task to begin");
        var waitTask = listener.ExpeditedWaitAsync();
        Assert.True(waitTask.Wait(s_unexpectedDelay), "Wait shouldn't have needed to timeout");
    }

    private static void Wait(AsynchronousOperationListener listener, ManualResetEventSlim signal1, ManualResetEventSlim signal2)
    {
        // Note: WaitTask will return immediately if there is no outstanding work.  Due to
        // threadpool scheduling, we may get here before that other thread has started to run.
        // That's why each task set's a signal to say that it has begun and we first wait for
        // that, and then start waiting.
        Assert.True(signal1.Wait(s_unexpectedDelay), "Shouldn't have hit timeout waiting for task to begin");
        Assert.True(signal2.Wait(s_unexpectedDelay), "Shouldn't have hit timeout waiting for task to begin");

        var waitTask = listener.ExpeditedWaitAsync();
        Assert.True(waitTask.Wait(s_unexpectedDelay), "Wait shouldn't have needed to timeout");
    }
}
