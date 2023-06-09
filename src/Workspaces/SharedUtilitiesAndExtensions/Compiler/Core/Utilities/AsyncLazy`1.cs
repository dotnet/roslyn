// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities
{
    internal static class AsyncLazy
    {
        public static AsyncLazy<T> Create<T>(Func<CancellationToken, Task<T>> asynchronousComputeFunction)
            => new(asynchronousComputeFunction);

        public static AsyncLazy<T> Create<T>(T value)
            => new(value);
    }

    /// <summary>
    /// Represents a value that can be retrieved synchronously or asynchronously by many clients.
    /// The value will be computed on-demand the moment the first client asks for it. While being
    /// computed, more clients can request the value. As long as there are outstanding clients the
    /// underlying computation will proceed.  If all outstanding clients cancel their request then
    /// the underlying value computation will be cancelled as well.
    /// 
    /// Creators of an <see cref="AsyncLazy{T}" /> can specify whether the result of the computation is
    /// cached for future requests or not. Choosing to not cache means the computation functions are kept
    /// alive, whereas caching means the value (but not functions) are kept alive once complete.
    /// </summary>
    internal sealed class AsyncLazy<T>
    {
        /// <summary>
        /// The underlying function that starts an asynchronous computation of the resulting value.
        /// Null'ed out once we've computed the result and we've been asked to cache it.  Otherwise,
        /// it is kept around in case the value needs to be computed again.
        /// </summary>
        private Func<CancellationToken, Task<T>>? _asynchronousComputeFunction;

        /// <summary>
        /// The underlying function that starts a synchronous computation of the resulting value.
        /// Null'ed out once we've computed the result and we've been asked to cache it, or if we
        /// didn't get any synchronous function given to us in the first place.
        /// </summary>
        private Func<CancellationToken, T>? _synchronousComputeFunction;

        /// <summary>
        /// The Task that holds the cached result.
        /// </summary>
        private Task<T>? _cachedResult;

        /// <summary>
        /// Mutex used to protect reading and writing to all mutable objects and fields.  Traces
        /// indicate that there's negligible contention on this lock, hence we can save some memory
        /// by using a single lock for all AsyncLazy instances.  Only trivial and non-reentrant work
        /// should be done while holding the lock.
        /// </summary>
        private static readonly NonReentrantLock s_gate = new(useThisInstanceForSynchronization: true);

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

        /// <summary>
        /// Creates an AsyncLazy that always returns the value, analogous to <see cref="Task.FromResult{T}" />.
        /// </summary>
        public AsyncLazy(T value)
        {
            _cachedResult = Task.FromResult(value);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("'cacheResult' is no longer supported.  Use constructor without it.", error: false)]
        public AsyncLazy(Func<CancellationToken, Task<T>> asynchronousComputeFunction, bool cacheResult)
            : this(asynchronousComputeFunction)
        {
        }
#pragma warning restore IDE0060 // Remove unused parameter

        public AsyncLazy(Func<CancellationToken, Task<T>> asynchronousComputeFunction)
            : this(asynchronousComputeFunction, synchronousComputeFunction: null)
        {
        }

        /// <summary>
        /// Creates an AsyncLazy that supports both asynchronous computation and inline synchronous
        /// computation.
        /// </summary>
        /// <param name="asynchronousComputeFunction">A function called to start the asynchronous
        /// computation. This function should be cheap and non-blocking.</param>
        /// <param name="synchronousComputeFunction">A function to do the work synchronously, which
        /// is allowed to block. This function should not be implemented by a simple Wait on the
        /// asynchronous value. If that's all you are doing, just don't pass a synchronous function
        /// in the first place.</param>
        public AsyncLazy(Func<CancellationToken, Task<T>> asynchronousComputeFunction, Func<CancellationToken, T>? synchronousComputeFunction)
        {
            Contract.ThrowIfNull(asynchronousComputeFunction);
            _asynchronousComputeFunction = asynchronousComputeFunction;
            _synchronousComputeFunction = synchronousComputeFunction;
        }

        #region Lock Wrapper for Invariant Checking

        /// <summary>
        /// Takes the lock for this object and if acquired validates the invariants of this class.
        /// </summary>
        private WaitThatValidatesInvariants TakeLock(CancellationToken cancellationToken)
        {
            s_gate.Wait(cancellationToken);
            AssertInvariants_NoLock();
            return new WaitThatValidatesInvariants(this);
        }

        private readonly struct WaitThatValidatesInvariants : IDisposable
        {
            private readonly AsyncLazy<T> _asyncLazy;

            public WaitThatValidatesInvariants(AsyncLazy<T> asyncLazy)
                => _asyncLazy = asyncLazy;

            public void Dispose()
            {
                _asyncLazy.AssertInvariants_NoLock();
                s_gate.Release();
            }
        }

        private void AssertInvariants_NoLock()
        {
            // Invariant #1: thou shalt never have an asynchronous computation running without it
            // being considered a computation
            Contract.ThrowIfTrue(_asynchronousComputationCancellationSource != null &&
                                 !_computationActive);

            // Invariant #2: thou shalt never waste memory holding onto empty HashSets
            Contract.ThrowIfTrue(_requests != null &&
                                 _requests.Count == 0);

            // Invariant #3: thou shalt never have an request if there is not
            // something trying to compute it
            Contract.ThrowIfTrue(_requests != null &&
                                 !_computationActive);

            // Invariant #4: thou shalt never have a cached value and any computation function
            Contract.ThrowIfTrue(_cachedResult != null &&
                                 (_synchronousComputeFunction != null || _asynchronousComputeFunction != null));

            // Invariant #5: thou shalt never have a synchronous computation function but not an
            // asynchronous one
            Contract.ThrowIfTrue(_asynchronousComputeFunction == null && _synchronousComputeFunction != null);
        }

        #endregion

        public bool TryGetValue([MaybeNullWhen(false)] out T result)
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

        public T GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // If the value is already available, return it immediately
            if (TryGetValue(out var value))
            {
                return value;
            }

            Request? request = null;
            AsynchronousComputationToStart? newAsynchronousComputation = null;

            using (TakeLock(cancellationToken))
            {
                // If cached, get immediately
                if (_cachedResult != null)
                {
                    return _cachedResult.Result;
                }

                // If there is an existing computation active, we'll just create another request
                if (_computationActive)
                {
                    request = CreateNewRequest_NoLock();
                }
                else if (_synchronousComputeFunction == null)
                {
                    // A synchronous request, but we have no synchronous function. Start off the async work
                    request = CreateNewRequest_NoLock();

                    newAsynchronousComputation = RegisterAsynchronousComputation_NoLock();
                }
                else
                {
                    // We will do the computation here
                    _computationActive = true;
                }
            }

            // If we simply created a new asynchronous request, so wait for it. Yes, we're blocking the thread
            // but we don't want multiple threads attempting to compute the same thing.
            if (request != null)
            {
                request.RegisterForCancellation(OnAsynchronousRequestCancelled, cancellationToken);

                // Since we already registered for cancellation, it's possible that the registration has
                // cancelled this new computation if we were the only requester.
                if (newAsynchronousComputation != null)
                {
                    StartAsynchronousComputation(newAsynchronousComputation.Value, requestToCompleteSynchronously: request, callerCancellationToken: cancellationToken);
                }

                // The reason we have synchronous codepaths in AsyncLazy is to support the synchronous requests for syntax trees
                // that we may get from the compiler. Thus, it's entirely possible that this will be requested by the compiler or
                // an analyzer on the background thread when another part of the IDE is requesting the same tree asynchronously.
                // In that case we block the synchronous request on the asynchronous request, since that's better than alternatives.
                return request.Task.WaitAndGetResult_CanCallOnBackground(cancellationToken);
            }
            else
            {
                Contract.ThrowIfNull(_synchronousComputeFunction);

                T result;

                // We are the active computation, so let's go ahead and compute.
                try
                {
                    result = _synchronousComputeFunction(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // This cancelled for some reason. We don't care why, but
                    // it means anybody else waiting for this result isn't going to get it
                    // from us.
                    using (TakeLock(CancellationToken.None))
                    {
                        _computationActive = false;

                        if (_requests != null)
                        {
                            // There's a possible improvement here: there might be another synchronous caller who
                            // also wants the value. We might consider stealing their thread rather than punting
                            // to the thread pool.
                            newAsynchronousComputation = RegisterAsynchronousComputation_NoLock();
                        }
                    }

                    if (newAsynchronousComputation != null)
                    {
                        StartAsynchronousComputation(newAsynchronousComputation.Value, requestToCompleteSynchronously: null, callerCancellationToken: cancellationToken);
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    // We faulted for some unknown reason. We should simply fault everything.
                    CompleteWithTask(Task.FromException<T>(ex), CancellationToken.None);
                    throw;
                }

                // We have a value, so complete
                CompleteWithTask(Task.FromResult(result), CancellationToken.None);

                // Optimization: if they did cancel and the computation never observed it, let's throw so we don't keep
                // processing a value somebody never wanted
                cancellationToken.ThrowIfCancellationRequested();

                return result;
            }
        }

        private Request CreateNewRequest_NoLock()
        {
            _requests ??= new HashSet<Request>();

            var request = new Request();
            _requests.Add(request);
            return request;
        }

        public Task<T> GetValueAsync(CancellationToken cancellationToken)
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
            Contract.ThrowIfTrue(_computationActive);
            Contract.ThrowIfNull(_asynchronousComputeFunction);

            _asynchronousComputationCancellationSource = new CancellationTokenSource();
            _computationActive = true;

            return new AsynchronousComputationToStart(_asynchronousComputeFunction, _asynchronousComputationCancellationSource);
        }

        private readonly struct AsynchronousComputationToStart
        {
            public readonly Func<CancellationToken, Task<T>> AsynchronousComputeFunction;
            public readonly CancellationTokenSource CancellationTokenSource;

            public AsynchronousComputationToStart(Func<CancellationToken, Task<T>> asynchronousComputeFunction, CancellationTokenSource cancellationTokenSource)
            {
                AsynchronousComputeFunction = asynchronousComputeFunction;
                CancellationTokenSource = cancellationTokenSource;
            }
        }

        private void StartAsynchronousComputation(AsynchronousComputationToStart computationToStart, Request? requestToCompleteSynchronously, CancellationToken callerCancellationToken)
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

                var task = computationToStart.AsynchronousComputeFunction(cancellationToken);

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
                        task = GetCachedValueAndCacheThisValueIfNoneCached_NoLock(task);
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
                throw ExceptionUtilities.Unreachable();
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e))
            {
                throw ExceptionUtilities.Unreachable();
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
                requestsToComplete = _requests ?? (IEnumerable<Request>)Array.Empty<Request>();
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
                return _cachedResult;

            if (task.Status == TaskStatus.RanToCompletion)
            {
                // Hold onto the completed task. We can get rid of the computation functions for good
                _cachedResult = task;

                _asynchronousComputeFunction = null;
                _synchronousComputeFunction = null;
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
            public Request() : base(TaskCreationOptions.RunContinuationsAsynchronously)
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
                    RoslynDebug.AssertNotNull(task.Exception);
                    if (task.Exception.InnerExceptions.Count > 0)
                        this.TrySetException(task.Exception.InnerExceptions);
                    else
                        this.TrySetException(task.Exception);
                }
                else
                {
                    this.TrySetResult(task.Result);
                }

                _cancellationTokenRegistration.Dispose();
            }

            public void Cancel()
                => this.TrySetCanceled(_cancellationToken);
        }
    }
}
