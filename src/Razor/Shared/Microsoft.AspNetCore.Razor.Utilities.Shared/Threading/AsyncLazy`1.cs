// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Threading;

// NOTE: This code is copied and modified from dotnet/roslyn:
// https://github.com/dotnet/roslyn/blob/192c9ccc0e43791e8145c7b4cc09e665993551fc/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Utilities/AsyncLazy%601.cs

// Of particular note, the synchronous computation feature has been removed. Roslyn needs this to provide
// a GetValue() method to support synchronous requests for syntax trees. Razor does not need this, so the
// implementation can be simplified.

internal abstract class AsyncLazy<T>
{
    public abstract bool TryGetValue([MaybeNullWhen(false)] out T result);
    public abstract Task<T> GetValueAsync(CancellationToken cancellationToken);

    public static AsyncLazy<T> Create<TArg>(
        Func<TArg, CancellationToken, Task<T>> asynchronousComputeFunction,
        TArg data)
        => AsyncLazyImpl<TArg>.CreateImpl(asynchronousComputeFunction, data);

    public static AsyncLazy<T> Create<TArg>(T value)
        => AsyncLazyImpl<VoidResult>.CreateImpl(value);

    /// <summary>
    /// Represents a value that can be retrieved asynchronously by many clients. The value will be
    /// computed on-demand the moment the first client asks for it. While being computed, more clients
    /// can request the value. As long as there are outstanding clients the underlying computation will
    /// proceed.  If all outstanding clients cancel their request then the underlying value computation
    /// will be cancelled as well.
    /// 
    /// Creators of an <see cref="AsyncLazy{T}" /> can specify whether the result of the computation is
    /// cached for future requests or not. Choosing to not cache means the computation function is kept
    /// alive, whereas caching means the value (but not function) is kept alive once complete.
    /// </summary>
    private sealed class AsyncLazyImpl<TData> : AsyncLazy<T>
    {
        /// <summary>
        /// The underlying function that starts an asynchronous computation of the resulting value.
        /// Null'ed out once we've computed the result and we've been asked to cache it.  Otherwise,
        /// it is kept around in case the value needs to be computed again.
        /// </summary>
        private Func<TData, CancellationToken, Task<T>>? _asynchronousComputeFunction;

        /// <summary>
        /// The Task that holds the cached result.
        /// </summary>
        private Task<T>? _cachedResult;

        /// <summary>
        /// Mutex used to protect reading and writing to all mutable objects and fields.  Traces indicate that there's
        /// negligible contention on this lock (and on any particular async-lazy in general), hence we can save some
        /// memory by using ourselves as the lock, even though this may inhibit cancellation.  Work done while holding
        /// the lock should be kept to a minimum.
        /// </summary>
        private object SyncObject => this;

        /// <summary>
        /// The hash set of all currently outstanding asynchronous requests. Null if there are no requests,
        /// and will never be empty.
        /// </summary>
        private HashSet<Request>? _requests;

        /// <summary>
        /// If an asynchronous request is active, the CancellationTokenSource that allows for
        /// cancelling the underlying computation.
        /// </summary>
        private CancellationTokenSource? _asynchronousComputationCancellationSource;

        /// <summary>
        /// Whether a computation is active or queued on any thread, whether synchronous or
        /// asynchronous.
        /// </summary>
        private bool _computationActive;

        private TData _data;

        /// <summary>
        /// Creates an AsyncLazy that always returns the value, analogous to <see cref="Task.FromResult{T}" />.
        /// </summary>
        private AsyncLazyImpl(T value)
        {
            _cachedResult = Task.FromResult(value);
            _data = default!;
        }

        /// <summary>
        /// Creates an AsyncLazy that supports both asynchronous computation and inline synchronous
        /// computation.
        /// </summary>
        /// <param name="asynchronousComputeFunction">A function called to start the asynchronous
        /// computation. This function should be cheap and non-blocking.</param>
        /// <param name="data"></param>
        private AsyncLazyImpl(
            Func<TData, CancellationToken, Task<T>> asynchronousComputeFunction,
            TData data)
        {
            ArgHelper.ThrowIfNull(asynchronousComputeFunction);
            _asynchronousComputeFunction = asynchronousComputeFunction;
            _data = data;
        }

        public static AsyncLazy<T> CreateImpl(T value)
            => new AsyncLazyImpl<VoidResult>(value);

        public static AsyncLazy<T> CreateImpl(
            Func<TData, CancellationToken, Task<T>> asynchronousComputeFunction,
            TData data)
        {
            return new AsyncLazyImpl<TData>(asynchronousComputeFunction, data);
        }

        #region Lock Wrapper for Invariant Checking

        /// <summary>
        /// Takes the lock for this object and if acquired validates the invariants of this class.
        /// </summary>
        private WaitThatValidatesInvariants TakeLock(CancellationToken cancellationToken)
        {
            Assumed.False(Monitor.IsEntered(SyncObject), "Attempt to take the lock while already holding it!");

            cancellationToken.ThrowIfCancellationRequested();
            Monitor.Enter(SyncObject);
            AssertInvariants_NoLock();
            return new WaitThatValidatesInvariants(this);
        }

        private readonly struct WaitThatValidatesInvariants(AsyncLazyImpl<TData> asyncLazy) : IDisposable
        {
            public void Dispose()
            {
                asyncLazy.AssertInvariants_NoLock();
                Assumed.True(Monitor.IsEntered(asyncLazy.SyncObject));
                Monitor.Exit(asyncLazy.SyncObject);
            }
        }

        private void AssertInvariants_NoLock()
        {
            // Invariant #1: thou shalt never have an asynchronous computation running without it
            // being considered a computation
            Assumed.False(_asynchronousComputationCancellationSource != null &&
                          !_computationActive);

            // Invariant #2: thou shalt never waste memory holding onto empty HashSets
            Assumed.False(_requests != null &&
                          _requests.Count == 0);

            // Invariant #3: thou shalt never have an request if there is not
            // something trying to compute it
            Assumed.False(_requests != null &&
                          !_computationActive);

            // Invariant #4: thou shalt never have a cached value and any computation function
            Assumed.False(_cachedResult != null &&
                          (_asynchronousComputeFunction != null));
        }

        #endregion

        public override bool TryGetValue([MaybeNullWhen(false)] out T result)
        {
            // No need to lock here since this is only a fast check to 
            // see if the result is already computed.
            if (_cachedResult != null)
            {
                result = _cachedResult.Result;
                return true;
            }

            result = default;
            return false;
        }

        private Request CreateNewRequest_NoLock()
        {
            _requests ??= [];

            var request = new Request();
            _requests.Add(request);
            return request;
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            // Optimization: if we're already cancelled, do not pass go
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(cancellationToken);
            }

            // Avoid taking the lock if a cached value is available
            var cachedResult = _cachedResult;
            if (cachedResult != null)
            {
                return cachedResult;
            }

            Request request;
            AsynchronousComputationToStart? newAsynchronousComputation = null;

            using (TakeLock(cancellationToken))
            {
                // If cached, get immediately
                if (_cachedResult != null)
                {
                    return _cachedResult;
                }

                request = CreateNewRequest_NoLock();

                // If we have either synchronous or asynchronous work current in flight, we don't need to do anything.
                // Otherwise, we shall start an asynchronous computation for this
                if (!_computationActive)
                {
                    newAsynchronousComputation = RegisterAsynchronousComputation_NoLock();
                }
            }

            // We now have the request counted for, register for cancellation. It is critical this is
            // done outside the lock, as our registration may immediately fire and we want to avoid the
            // reentrancy
            request.RegisterForCancellation(OnAsynchronousRequestCancelled, cancellationToken);

            if (newAsynchronousComputation != null)
            {
                StartAsynchronousComputation(newAsynchronousComputation.Value, requestToCompleteSynchronously: request, callerCancellationToken: cancellationToken);
            }

            return request.Task;
        }

        private AsynchronousComputationToStart RegisterAsynchronousComputation_NoLock()
        {
            Assumed.False(_computationActive);
            Assumed.NotNull(_asynchronousComputeFunction);

            _asynchronousComputationCancellationSource = new CancellationTokenSource();
            _computationActive = true;

            return new(_asynchronousComputeFunction, _asynchronousComputationCancellationSource);
        }

        private readonly struct AsynchronousComputationToStart(
            Func<TData, CancellationToken, Task<T>> asynchronousComputeFunction,
            CancellationTokenSource cancellationTokenSource)
        {
            public readonly Func<TData, CancellationToken, Task<T>> AsynchronousComputeFunction = asynchronousComputeFunction;
            public readonly CancellationTokenSource CancellationTokenSource = cancellationTokenSource;
        }

        private void StartAsynchronousComputation(
            AsynchronousComputationToStart computationToStart,
            Request? requestToCompleteSynchronously,
            CancellationToken callerCancellationToken)
        {
            var cancellationToken = computationToStart.CancellationTokenSource.Token;

            // DO NOT ACCESS ANY FIELDS OR STATE BEYOND THIS POINT. Since this function
            // runs unsynchronized, it's possible that during this function this request
            // might be cancelled, and then a whole additional request might start and
            // complete inline, and cache the result. By grabbing state before we check
            // the cancellation token, we can be assured that we are only operating on
            // a state that was complete.
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var task = computationToStart.AsynchronousComputeFunction(_data, cancellationToken);

                // As an optimization, if the task is already completed, mark the 
                // request as being completed as well.
                //
                // Note: we want to do this before we do the .ContinueWith below. That way, 
                // when the async call to CompleteWithTask runs, it sees that we've already
                // completed and can bail immediately. 
                if (requestToCompleteSynchronously != null && task.IsCompleted)
                {
                    using (TakeLock(CancellationToken.None))
                    {
#pragma warning disable CA2025 // task is already completed so we're not disposing too early here
                        task = GetCachedValueAndCacheThisValueIfNoneCached_NoLock(task);
#pragma warning restore
                    }

                    requestToCompleteSynchronously.CompleteFromTask(task);
                }

                // We avoid creating a full closure just to pass the token along
                // Also, use TaskContinuationOptions.ExecuteSynchronously so that we inline 
                // the continuation if asynchronousComputeFunction completes synchronously
                task.ContinueWith(
                    (t, s) => CompleteWithTask(t, ((CancellationTokenSource)s!).Token),
                    computationToStart.CancellationTokenSource,
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            catch (OperationCanceledException e) when (e.CancellationToken == cancellationToken)
            {
                // The underlying computation cancelled with the correct token, but we must ourselves ensure that the caller
                // on our stack gets an OperationCanceledException thrown with the right token
                callerCancellationToken.ThrowIfCancellationRequested();

                // We can only be here if the computation was cancelled, which means all requests for the value
                // must have been cancelled. Therefore, the ThrowIfCancellationRequested above must have thrown
                // because that token from the requester was cancelled.
                Assumed.Unreachable();
            }
        }

        private void CompleteWithTask(Task<T> task, CancellationToken cancellationToken)
        {
            IEnumerable<Request> requestsToComplete;

            using (TakeLock(cancellationToken))
            {
                // If the underlying computation was cancelled, then all state was already updated in OnAsynchronousRequestCancelled
                // and there is no new work to do here. We *must* use the local one since this completion may be running far after
                // the background computation was cancelled and a new one might have already been enqueued. We must do this
                // check here under the lock to ensure proper synchronization with OnAsynchronousRequestCancelled.
                cancellationToken.ThrowIfCancellationRequested();

                // The computation is complete, so get all requests to complete and null out the list. We'll create another one
                // later if it's needed
                requestsToComplete = _requests ?? (IEnumerable<Request>)[];
                _requests = null;

                // The computations are done
                _asynchronousComputationCancellationSource = null;
                _computationActive = false;
                task = GetCachedValueAndCacheThisValueIfNoneCached_NoLock(task);
            }

            // Complete the requests outside the lock. It's not necessary to do this (none of this is touching any shared state)
            // but there's no reason to hold the lock so we could reduce any theoretical lock contention.
            foreach (var requestToComplete in requestsToComplete)
            {
                requestToComplete.CompleteFromTask(task);
            }
        }

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        private Task<T> GetCachedValueAndCacheThisValueIfNoneCached_NoLock(Task<T> task)
        {
            if (_cachedResult != null)
            {
                return _cachedResult;
            }

            if (task.Status == TaskStatus.RanToCompletion)
            {
                // Hold onto the completed task. We can get rid of the computation functions for good
                _cachedResult = task;

                _asynchronousComputeFunction = null;
                _data = default!;
            }

            return task;
        }

        private void OnAsynchronousRequestCancelled(object? state)
        {
            var request = (Request)state!;
            CancellationTokenSource? cancellationTokenSource = null;

            using (TakeLock(CancellationToken.None))
            {
                // Now try to remove it. It's possible that requests may already be null. You could
                // imagine that cancellation was requested, but before we could acquire the lock
                // here the computation completed and the entire CompleteWithTask synchronized
                // block ran. In that case, the requests collection may already be null, or it
                // (even scarier!) may have been replaced with another collection because another
                // computation has started.
                if (_requests != null)
                {
                    if (_requests.Remove(request))
                    {
                        if (_requests.Count == 0)
                        {
                            _requests = null;

                            if (_asynchronousComputationCancellationSource != null)
                            {
                                cancellationTokenSource = _asynchronousComputationCancellationSource;
                                _asynchronousComputationCancellationSource = null;
                                _computationActive = false;
                            }
                        }
                    }
                }
            }

            request.Cancel();
            cancellationTokenSource?.Cancel();
        }

        /// <remarks>
        /// This inherits from <see cref="TaskCompletionSource{TResult}"/> to avoid allocating two objects when we can just use one.
        /// The public surface area of <see cref="TaskCompletionSource{TResult}"/> should probably be avoided in favor of the public
        /// methods on this class for correct behavior.
        /// </remarks>
        private sealed class Request : TaskCompletionSource<T>
        {
            /// <summary>
            /// The <see cref="CancellationToken"/> associated with this request. This field will be initialized before
            /// any cancellation is observed from the token.
            /// </summary>
            private CancellationToken _cancellationToken;
            private CancellationTokenRegistration _cancellationTokenRegistration;

            // We want to always run continuations asynchronously. Running them synchronously could result in deadlocks:
            // if we're looping through a bunch of Requests and completing them one by one, and the continuation for the
            // first Request was then blocking waiting for a later Request, we would hang. It also could cause performance
            // issues. If the first request then consumes a lot of CPU time, we're not letting other Requests complete that
            // could use another CPU core at the same time.
            public Request()
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
            }

            public void RegisterForCancellation(Action<object?> callback, CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                _cancellationTokenRegistration = cancellationToken.Register(callback, this);
            }

            public void CompleteFromTask(Task<T> task)
            {
                // As an optimization, we'll cancel the request even we did get a value for it.
                // That way things abort sooner.
                if (task.IsCanceled || _cancellationToken.IsCancellationRequested)
                {
                    Cancel();
                }
                else if (task.IsFaulted)
                {
                    // TrySetException wraps its argument in an AggregateException, so we pass the inner exceptions from
                    // the antecedent to avoid wrapping in two layers of AggregateException.
                    Assumed.NotNull(task.Exception);
                    if (task.Exception.InnerExceptions.Count > 0)
                    {
                        TrySetException(task.Exception.InnerExceptions);
                    }
                    else
                    {
                        TrySetException(task.Exception);
                    }
                }
                else
                {
                    TrySetResult(task.Result);
                }

                _cancellationTokenRegistration.Dispose();
            }

            public void Cancel()
                => TrySetCanceled(_cancellationToken);
        }
    }
}
