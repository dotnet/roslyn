// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NET

#pragma warning disable CA1068 // CancellationToken parameters must come last
#pragma warning disable IDE0007 // Use implicit type
#pragma warning disable IDE2003 // Blank line required between block and subsequent statement
#pragma warning disable IDE2004 // Blank line not allowed after constructor initializer colon
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods

// Ported from
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Threading.Tasks.Parallel/src/System/Threading/Tasks/Parallel.ForEachAsync.cs
// With only changes to make the code work on NetFx.  Where changes have been made, the original code is kept around in
// an ifdef'ed block to see what it was doing.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static partial class RoslynParallel
{
    private static class NetFramework
    {
        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        /// <remarks>The operation will execute at most <see cref="Environment.ProcessorCount"/> operations in parallel.</remarks>
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
        {
#if false
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);
#endif

            return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, default(CancellationToken), body);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="cancellationToken">A cancellation token that may be used to cancel the for each operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        /// <remarks>The operation will execute at most <see cref="Environment.ProcessorCount"/> operations in parallel.</remarks>
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        {
#if false
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);
#endif

            return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, cancellationToken, body);
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="parallelOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        public static Task ForEachAsync<TSource>(IEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
        {
#if false
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);

            return ForEachAsync(source, parallelOptions.EffectiveMaxConcurrencyLevel, parallelOptions.EffectiveTaskScheduler, parallelOptions.CancellationToken, body);
#else
            return ForEachAsync(source, EffectiveMaxConcurrencyLevel(parallelOptions), EffectiveTaskScheduler(parallelOptions), parallelOptions.CancellationToken, body);
#endif
        }

        // Copied from https://github.com/dotnet/runtime/blob/6f18b5ef46a8fbc6675b07d4b256c35b36fc4e3c/src/libraries/System.Threading.Tasks.Parallel/src/System/Threading/Tasks/Parallel.cs#L64
        // Convenience property used by TPL logic
        private static TaskScheduler EffectiveTaskScheduler(ParallelOptions options) => options.TaskScheduler ?? TaskScheduler.Current;

        private static int EffectiveMaxConcurrencyLevel(ParallelOptions options)
        {
            int rval = options.MaxDegreeOfParallelism;
            int schedulerMax = EffectiveTaskScheduler(options).MaximumConcurrencyLevel;
            if ((schedulerMax > 0) && (schedulerMax != int.MaxValue))
            {
                rval = (rval == -1) ? schedulerMax : Math.Min(schedulerMax, rval);
            }
            return rval;
        }

        /// <summary>Executes a for each operation on an <see cref="IEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An enumerable data source.</param>
        /// <param name="dop">A integer indicating how many operations to allow to run in parallel.</param>
        /// <param name="scheduler">The task scheduler on which all code should execute.</param>
        /// <param name="cancellationToken">A cancellation token that may be used to cancel the for each operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The<paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        private static Task ForEachAsync<TSource>(IEnumerable<TSource> source, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        {
            Debug.Assert(source != null);
            Debug.Assert(scheduler != null);
            Debug.Assert(body != null);

            // One fast up-front check for cancellation before we start the whole operation.
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            // The worker body. Each worker will execute this same body.
            Func<object, Task> taskBody = static async o =>
            {
                var state = (SyncForEachAsyncState<TSource>)o;
                bool launchedNext = false;

#pragma warning disable CA2007 // Explicitly don't use ConfigureAwait, as we want to perform all work on the specified scheduler that's now current
                try
                {
                    // Continue to loop while there are more elements to be processed.
                    while (!state.Cancellation.IsCancellationRequested)
                    {
                        // Get the next element from the enumerator.  This requires asynchronously locking around MoveNext/Current.
                        TSource element;
                        await state.AcquireLock();
                        try
                        {
                            if (state.Cancellation.IsCancellationRequested || // check now that the lock has been acquired
                                !state.Enumerator.MoveNext())
                            {
                                break;
                            }

                            element = state.Enumerator.Current;
                        }
                        finally
                        {
                            state.ReleaseLock();
                        }

                        // If the remaining dop allows it and we've not yet queued the next worker, do so now.  We wait
                        // until after we've grabbed an item from the enumerator to a) avoid unnecessary contention on the
                        // serialized resource, and b) avoid queueing another work if there aren't any more items.  Each worker
                        // is responsible only for creating the next worker, which in turn means there can't be any contention
                        // on creating workers (though it's possible one worker could be executing while we're creating the next).
                        if (!launchedNext)
                        {
                            launchedNext = true;
                            state.QueueWorkerIfDopAvailable();
                        }

                        // Process the loop body.
                        await state.LoopBody(element, state.Cancellation.Token);
                    }
                }
                catch (Exception e)
                {
                    // Record the failure and then don't let the exception propagate.  The last worker to complete
                    // will propagate exceptions as is appropriate to the top-level task.
                    state.RecordException(e);
                }
                finally
                {
                    // If we're the last worker to complete, clean up and complete the operation.
                    if (state.SignalWorkerCompletedIterating())
                    {
                        try
                        {
                            state.Dispose();
                        }
                        catch (Exception e)
                        {
                            state.RecordException(e);
                        }

                        // Finally, complete the task returned to the ForEachAsync caller.
                        // This must be the very last thing done.
                        state.Complete();
                    }
                }
#pragma warning restore CA2007
            };

            try
            {
                // Construct a state object that encapsulates all state to be passed and shared between
                // the workers, and queues the first worker.
                var state = new SyncForEachAsyncState<TSource>(source, taskBody, dop, scheduler, cancellationToken, body);
                state.QueueWorkerIfDopAvailable();
                return state.Task;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An asynchronous enumerable data source.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        /// <remarks>The operation will execute at most <see cref="Environment.ProcessorCount"/> operations in parallel.</remarks>
        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, ValueTask> body)
        {
#if false
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);
#endif

            return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, default(CancellationToken), body);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An asynchronous enumerable data source.</param>
        /// <param name="cancellationToken">A cancellation token that may be used to cancel the for each operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        /// <remarks>The operation will execute at most <see cref="Environment.ProcessorCount"/> operations in parallel.</remarks>
        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        {
#if false
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(body);
#endif

            return ForEachAsync(source, DefaultDegreeOfParallelism, TaskScheduler.Default, cancellationToken, body);
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An asynchronous enumerable data source.</param>
        /// <param name="parallelOptions">An object that configures the behavior of this operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        public static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, ParallelOptions parallelOptions, Func<TSource, CancellationToken, ValueTask> body)
        {
#if false
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(parallelOptions);
            ArgumentNullException.ThrowIfNull(body);
#endif

#if false
            return ForEachAsync(source, parallelOptions.EffectiveMaxConcurrencyLevel, parallelOptions.EffectiveTaskScheduler, parallelOptions.CancellationToken, body);
#else
            return ForEachAsync(source, EffectiveMaxConcurrencyLevel(parallelOptions), EffectiveTaskScheduler(parallelOptions), parallelOptions.CancellationToken, body);
#endif
        }

        /// <summary>Executes a for each operation on an <see cref="IAsyncEnumerable{TSource}"/> in which iterations may run in parallel.</summary>
        /// <typeparam name="TSource">The type of the data in the source.</typeparam>
        /// <param name="source">An asynchronous enumerable data source.</param>
        /// <param name="dop">A integer indicating how many operations to allow to run in parallel.</param>
        /// <param name="scheduler">The task scheduler on which all code should execute.</param>
        /// <param name="cancellationToken">A cancellation token that may be used to cancel the for each operation.</param>
        /// <param name="body">An asynchronous delegate that is invoked once per element in the data source.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="source"/> argument or <paramref name="body"/> argument is <see langword="null"/>.</exception>
        /// <returns>A task that represents the entire for each operation.</returns>
        private static Task ForEachAsync<TSource>(IAsyncEnumerable<TSource> source, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
        {
            Debug.Assert(source != null);
            Debug.Assert(scheduler != null);
            Debug.Assert(body != null);

            // One fast up-front check for cancellation before we start the whole operation.
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            // The worker body. Each worker will execute this same body.
            Func<object, Task> taskBody = static async o =>
            {
                var state = (AsyncForEachAsyncState<TSource>)o;
                bool launchedNext = false;

#pragma warning disable CA2007 // Explicitly don't use ConfigureAwait, as we want to perform all work on the specified scheduler that's now current
                try
                {
                    // Continue to loop while there are more elements to be processed.
                    while (!state.Cancellation.IsCancellationRequested)
                    {
                        // Get the next element from the enumerator.  This requires asynchronously locking around MoveNextAsync/Current.
                        TSource element;
                        await state.AcquireLock();
                        try
                        {
                            if (state.Cancellation.IsCancellationRequested || // check now that the lock has been acquired
                                !await state.Enumerator.MoveNextAsync())
                            {
                                break;
                            }

                            element = state.Enumerator.Current;
                        }
                        finally
                        {
                            state.ReleaseLock();
                        }

                        // If the remaining dop allows it and we've not yet queued the next worker, do so now.  We wait
                        // until after we've grabbed an item from the enumerator to a) avoid unnecessary contention on the
                        // serialized resource, and b) avoid queueing another work if there aren't any more items.  Each worker
                        // is responsible only for creating the next worker, which in turn means there can't be any contention
                        // on creating workers (though it's possible one worker could be executing while we're creating the next).
                        if (!launchedNext)
                        {
                            launchedNext = true;
                            state.QueueWorkerIfDopAvailable();
                        }

                        // Process the loop body.
                        await state.LoopBody(element, state.Cancellation.Token);
                    }
                }
                catch (Exception e)
                {
                    // Record the failure and then don't let the exception propagate.  The last worker to complete
                    // will propagate exceptions as is appropriate to the top-level task.
                    state.RecordException(e);
                }
                finally
                {
                    // If we're the last worker to complete, clean up and complete the operation.
                    if (state.SignalWorkerCompletedIterating())
                    {
                        try
                        {
                            await state.DisposeAsync();
                        }
                        catch (Exception e)
                        {
                            state.RecordException(e);
                        }

                        // Finally, complete the task returned to the ForEachAsync caller.
                        // This must be the very last thing done.
                        state.Complete();
                    }
                }
#pragma warning restore CA2007
            };

            try
            {
                // Construct a state object that encapsulates all state to be passed and shared between
                // the workers, and queues the first worker.
                var state = new AsyncForEachAsyncState<TSource>(source, taskBody, dop, scheduler, cancellationToken, body);
                state.QueueWorkerIfDopAvailable();
                return state.Task;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        /// <summary>Gets the default degree of parallelism to use when none is explicitly provided.</summary>
        private static int DefaultDegreeOfParallelism => Environment.ProcessorCount;

        /// <summary>Stores the state associated with a ForEachAsync operation, shared between all its workers.</summary>
        /// <typeparam name="TSource">Specifies the type of data being enumerated.</typeparam>
#if false
        private abstract class ForEachAsyncState<TSource> : TaskCompletionSource, IThreadPoolWorkItem
#else
        private abstract class ForEachAsyncState<TSource> : TaskCompletionSource<VoidResult>
#endif
        {
            /// <summary>The caller-provided cancellation token.</summary>
            private readonly CancellationToken _externalCancellationToken;
            /// <summary>Registration with caller-provided cancellation token.</summary>
            protected readonly CancellationTokenRegistration _registration;
            /// <summary>
            /// The delegate to invoke on each worker to run the enumerator processing loop.
            /// </summary>
            /// <remarks>
            /// This could have been an action rather than a func, but it returns a task so that the task body is an async Task
            /// method rather than async void, even though the worker body catches all exceptions and the returned Task is ignored.
            /// </remarks>
            private readonly Func<object, Task> _taskBody;
            /// <summary>The <see cref="TaskScheduler"/> on which all work should be performed.</summary>
            private readonly TaskScheduler _scheduler;
#if false
            /// <summary>The <see cref="ExecutionContext"/> present at the time of the ForEachAsync invocation.  This is only used if on the default scheduler.</summary>
            private readonly ExecutionContext? _executionContext;
#endif
            /// <summary>Semaphore used to provide exclusive access to the enumerator.</summary>
            private readonly SemaphoreSlim? _lock;

            /// <summary>The number of outstanding workers.  When this hits 0, the operation has completed.</summary>
            private int _completionRefCount;
            /// <summary>Any exceptions incurred during execution.</summary>
            private List<Exception>? _exceptions;
            /// <summary>The number of workers that may still be created.</summary>
            private int _remainingDop;

            /// <summary>The delegate to invoke for each element yielded by the enumerator.</summary>
            public readonly Func<TSource, CancellationToken, ValueTask> LoopBody;
            /// <summary>The internal token source used to cancel pending work.</summary>
            public readonly CancellationTokenSource Cancellation = new CancellationTokenSource();

            /// <summary>Initializes the state object.</summary>
            protected ForEachAsyncState(Func<object, Task> taskBody, bool needsLock, int dop, TaskScheduler scheduler, CancellationToken cancellationToken, Func<TSource, CancellationToken, ValueTask> body)
            {
                _taskBody = taskBody;
                _lock = needsLock ? new SemaphoreSlim(initialCount: 1, maxCount: 1) : null;
                _remainingDop = dop < 0 ? DefaultDegreeOfParallelism : dop;
                LoopBody = body;
                _scheduler = scheduler;

#if false
                if (scheduler == TaskScheduler.Default)
                {
                    _executionContext = ExecutionContext.Capture();
                }
#endif

                _externalCancellationToken = cancellationToken;
#if false
                _registration = cancellationToken.UnsafeRegister(static o => ((ForEachAsyncState<TSource>)o!).Cancellation.Cancel(), this);
#else
                _registration = cancellationToken.Register(static o => ((ForEachAsyncState<TSource>)o!).Cancellation.Cancel(), this);
#endif
            }

            /// <summary>Queues another worker if allowed by the remaining degree of parallelism permitted.</summary>
            /// <remarks>This is not thread-safe and must only be invoked by one worker at a time.</remarks>
            public void QueueWorkerIfDopAvailable()
            {
                if (_remainingDop > 0)
                {
                    _remainingDop--;

                    // Queue the invocation of the worker/task body.  Note that we explicitly do not pass a cancellation token here,
                    // as the task body is what's responsible for completing the ForEachAsync task, for decrementing the reference count
                    // on pending tasks, and for cleaning up state.  If a token were passed to StartNew (which simply serves to stop the
                    // task from starting to execute if it hasn't yet by the time cancellation is requested), all of that logic could be
                    // skipped, and bad things could ensue, e.g. deadlocks, leaks, etc.  Also note that we need to increment the pending
                    // work item ref count prior to queueing the worker in order to avoid race conditions that could lead to temporarily
                    // and erroneously bouncing at zero, which would trigger completion too early.
                    Interlocked.Increment(ref _completionRefCount);
#if false
                    if (_scheduler == TaskScheduler.Default)
                    {
                        // If the scheduler is the default, we can avoid the overhead of the StartNew Task by just queueing
                        // this state object as the work item.
                        ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: false);
                    }
                    else
#endif
                    {
                        // We're targeting a non-default TaskScheduler, so queue the task body to it.
                        System.Threading.Tasks.Task.Factory.StartNew(_taskBody!, this, default(CancellationToken), TaskCreationOptions.DenyChildAttach, _scheduler);
                    }
                }
            }

            /// <summary>Signals that the worker has completed iterating.</summary>
            /// <returns>true if this is the last worker to complete iterating; otherwise, false.</returns>
            public bool SignalWorkerCompletedIterating() => Interlocked.Decrement(ref _completionRefCount) == 0;

            /// <summary>Asynchronously acquires exclusive access to the enumerator.</summary>
            public Task AcquireLock()
            {
                // We explicitly don't pass this.Cancellation to WaitAsync.  Doing so adds overhead, and it isn't actually
                // necessary. All of the operations that monitor the lock are part of the same ForEachAsync operation, and the Task
                // returned from ForEachAsync can't complete until all of the constituent operations have completed, including whoever
                // holds the lock while this worker is waiting on the lock.  Thus, the lock will need to be released for the overall
                // operation to complete.  Passing the token would allow the overall operation to potentially complete a bit faster in
                // the face of cancellation, in exchange for making it a bit slower / more overhead in the common case of cancellation
                // not being requested.  We want to optimize for the latter.  This also then avoids an exception throw / catch when
                // cancellation is requested.
                Debug.Assert(_lock is not null, "Should only be invoked when _lock is non-null");
                return _lock.WaitAsync(CancellationToken.None);
            }

            /// <summary>Relinquishes exclusive access to the enumerator.</summary>
            public void ReleaseLock()
            {
                Debug.Assert(_lock is not null, "Should only be invoked when _lock is non-null");
                _lock.Release();
            }

            /// <summary>Stores an exception and triggers cancellation in order to alert all workers to stop as soon as possible.</summary>
            /// <param name="e">The exception.</param>
            public void RecordException(Exception e)
            {
                // Store the exception.
                lock (this)
                {
                    (_exceptions ??= new List<Exception>()).Add(e);
                }

                // Trigger cancellation of all workers.  If cancellation has already been triggered
                // due to a previous exception occurring, this is a nop.
                try
                {
                    Cancellation.Cancel();
                }
                catch (AggregateException ae)
                {
                    // If cancellation callbacks erroneously throw exceptions, include those exceptions in the list.
                    lock (this)
                    {
                        _exceptions.AddRange(ae.InnerExceptions);
                    }
                }
            }

            /// <summary>Completes the ForEachAsync task based on the status of this state object.</summary>
            public void Complete()
            {
                Debug.Assert(_completionRefCount == 0, $"Expected {nameof(_completionRefCount)} == 0, got {_completionRefCount}");

                bool taskSet;
                if (_externalCancellationToken.IsCancellationRequested)
                {
                    // The externally provided token had cancellation requested. Assume that any exceptions
                    // then are due to that, and just cancel the resulting task.
                    taskSet = TrySetCanceled(_externalCancellationToken);
                }
                else if (_exceptions is null)
                {
                    // Everything completed successfully.
                    Debug.Assert(!Cancellation.IsCancellationRequested);
#if false
                    taskSet = TrySetResult();
#else
                    taskSet = TrySetResult(default(VoidResult));
#endif
                }
                else
                {
                    // Fail the task with the resulting exceptions.  The first should be the initial
                    // exception that triggered the operation to shut down.  The others, if any, may
                    // include cancellation exceptions from other concurrent operations being canceled
                    // in response to the primary exception.
                    taskSet = TrySetException(_exceptions);
                }

                Debug.Assert(taskSet, "Complete should only be called once.");
            }

#if false
            /// <summary>Executes the task body using the <see cref="ExecutionContext"/> captured when ForEachAsync was invoked.</summary>
            void IThreadPoolWorkItem.Execute()
            {
                Debug.Assert(_scheduler == TaskScheduler.Default, $"Expected {nameof(_scheduler)} == TaskScheduler.Default, got {_scheduler}");

                if (_executionContext is null)
                {
                    _taskBody(this);
                }
                else
                {
                    ExecutionContext.Run(_executionContext, static o => ((ForEachAsyncState<TSource>)o!)._taskBody(o), this);
                }
            }
#endif
        }

        /// <summary>Stores the state associated with an IEnumerable ForEachAsync operation, shared between all its workers.</summary>
        /// <typeparam name="TSource">Specifies the type of data being enumerated.</typeparam>
        private sealed class SyncForEachAsyncState<TSource> : ForEachAsyncState<TSource>, IDisposable
        {
            public readonly IEnumerator<TSource> Enumerator;

            public SyncForEachAsyncState(
                IEnumerable<TSource> source, Func<object, Task> taskBody,
                int dop, TaskScheduler scheduler, CancellationToken cancellationToken,
                Func<TSource, CancellationToken, ValueTask> body) :
                base(taskBody, needsLock: true, dop, scheduler, cancellationToken, body)
            {
#if false
                Enumerator = source.GetEnumerator() ?? throw new InvalidOperationException(SR.Parallel_ForEach_NullEnumerator);
#else
                Enumerator = source.GetEnumerator() ?? throw new InvalidOperationException();
#endif
            }

            public void Dispose()
            {
                _registration.Dispose();
                Enumerator.Dispose();
            }
        }

        /// <summary>Stores the state associated with an IAsyncEnumerable ForEachAsync operation, shared between all its workers.</summary>
        /// <typeparam name="TSource">Specifies the type of data being enumerated.</typeparam>
        private sealed class AsyncForEachAsyncState<TSource> : ForEachAsyncState<TSource>, IAsyncDisposable
        {
            public readonly IAsyncEnumerator<TSource> Enumerator;

            public AsyncForEachAsyncState(
                IAsyncEnumerable<TSource> source, Func<object, Task> taskBody,
                int dop, TaskScheduler scheduler, CancellationToken cancellationToken,
                Func<TSource, CancellationToken, ValueTask> body) :
                base(taskBody, needsLock: true, dop, scheduler, cancellationToken, body)
            {
#if false
                Enumerator = source.GetAsyncEnumerator(Cancellation.Token) ?? throw new InvalidOperationException(SR.Parallel_ForEach_NullEnumerator);
#else
                Enumerator = source.GetAsyncEnumerator(Cancellation.Token) ?? throw new InvalidOperationException();
#endif
            }

            public ValueTask DisposeAsync()
            {
                _registration.Dispose();
                return Enumerator.DisposeAsync();
            }
        }

        /// <summary>Stores the state associated with an IAsyncEnumerable ForEachAsync operation, shared between all its workers.</summary>
        /// <typeparam name="T">Specifies the type of data being enumerated.</typeparam>
        private sealed class ForEachState<T> : ForEachAsyncState<T>, IDisposable
        {
            public T NextAvailable;
            public readonly T ToExclusive;

            public ForEachState(
                T fromExclusive, T toExclusive, Func<object, Task> taskBody,
                bool needsLock, int dop, TaskScheduler scheduler, CancellationToken cancellationToken,
                Func<T, CancellationToken, ValueTask> body) :
                base(taskBody, needsLock, dop, scheduler, cancellationToken, body)
            {
                NextAvailable = fromExclusive;
                ToExclusive = toExclusive;
            }

            public void Dispose() => _registration.Dispose();
        }
    }
}

#endif
