// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities
{
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
    internal sealed class AsyncLazy<T> : ValueSource<T>
    {
        /// <summary>
        /// The underlying function that starts an asynchronous computation of the resulting value.
        /// Null'ed out once we've computed the result and we've been asked to cache it.  Otherwise,
        /// it is kept around in case the value needs to be computed again.
        /// </summary>
        private Func<CancellationToken, Task<T>> asynchronousComputeFunction;

        /// <summary>
        /// The underlying function that starts an synchronous computation of the resulting value.
        /// Null'ed out once we've computed the result and we've been asked to cache it, or if we
        /// didn't get any synchronous function given to us in the first place.
        /// </summary>
        private Func<CancellationToken, T> synchronousComputeFunction;

        /// <summary>
        /// Whether or not we should keep the value around once we've computed it.
        /// </summary>
        private readonly bool cacheResult;

        /// <summary>
        /// The Task that holds the cached result.
        /// </summary>
        private Task<T> cachedResult;

        /// <summary>
        /// Mutex used to protect reading and writing to all mutable objects and fields.  Traces
        /// indicate that there's negligible contention on this lock, hence we can save some memory
        /// by using a single lock for all AsyncLazy instances.  Only trivial and non-reentrant work
        /// should be done while holding the lock.
        /// </summary>
        private static readonly NonReentrantLock gate = new NonReentrantLock(useThisInstanceForSynchronization: true);

        /// <summary>
        /// The hash set of all currently outstanding asynchronous requests. Null if there are no requests,
        /// and will never be empty.
        /// </summary>
        private HashSet<Request> requests = null;

        /// <summary>
        /// If an asynchronous request is active, the CancellationTokenSource that allows for
        /// cancelling the underlying computation.
        /// </summary>
        private CancellationTokenSource asynchronousComputationCancellationSource = null;

        /// <summary>
        /// Whether a computation is active or queued on any thread, whether synchronous or
        /// asynchronous.
        /// </summary>
        private bool computationActive = false;

        /// <summary>
        /// Creates an AsyncLazy that always returns the value, analogous to <see cref="Task.FromResult{T}" />.
        /// </summary>
        public AsyncLazy(T value)
        {
            this.cacheResult = true;
            this.cachedResult = Task.FromResult(value);
        }

        /// <summary>
        /// Important: callers of this constructor should ensure that the compute function returns
        /// a task in a non-blocking fashion.  i.e. the function should *not* synchronously compute
        /// a value and then return it using Task.FromResult.  Instead, it should return an actual
        /// task that operates asynchronously.  If this function synchronously computes a value
        /// then that will cause locks to be held in this type for excessive periods of time.
        /// </summary>
        public AsyncLazy(Func<CancellationToken, Task<T>> asynchronousComputeFunction, bool cacheResult)
            : this(asynchronousComputeFunction, synchronousComputeFunction: null, cacheResult: cacheResult)
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
        /// <param name="cacheResult">Whether the result should be cached once the computation is
        /// complete.</param>
        public AsyncLazy(Func<CancellationToken, Task<T>> asynchronousComputeFunction, Func<CancellationToken, T> synchronousComputeFunction, bool cacheResult)
        {
            Contract.ThrowIfNull(asynchronousComputeFunction);

            this.asynchronousComputeFunction = asynchronousComputeFunction;
            this.synchronousComputeFunction = synchronousComputeFunction;
            this.cacheResult = cacheResult;
        }

        #region Lock Wrapper for Invariant Checking

        /// <summary>
        /// Takes the lock for this object and if acquired validates the invariants of this class.
        /// </summary>
        private WaitThatValidatesInvariants TakeLock(CancellationToken cancellationToken)
        {
            gate.Wait(cancellationToken);
            AssertInvariants_NoLock();
            return new WaitThatValidatesInvariants(this);
        }

        private struct WaitThatValidatesInvariants : IDisposable
        {
            private readonly AsyncLazy<T> asyncLazy;

            public WaitThatValidatesInvariants(AsyncLazy<T> asyncLazy)
            {
                this.asyncLazy = asyncLazy;
            }

            public void Dispose()
            {
                asyncLazy.AssertInvariants_NoLock();
                gate.Release();
            }
        }

        private void AssertInvariants_NoLock()
        {
            // Invariant #1: thou shalt never have an asynchronous computation running without it
            // being considered a computation
            Contract.ThrowIfTrue(this.asynchronousComputationCancellationSource != null &&
                                 !this.computationActive);

            // Invariant #2: thou shalt never waste memory holding onto empty HashSets
            Contract.ThrowIfTrue(this.requests != null &&
                                 this.requests.Count == 0);

            // Invariant #3: thou shalt never have an request if there is not
            // something trying to compute it
            Contract.ThrowIfTrue(this.requests != null &&
                                 !this.computationActive);

            // Invariant #4: thou shalt never have a cached value and any computation function
            Contract.ThrowIfTrue(this.cachedResult != null &&
                                 (this.synchronousComputeFunction != null || this.asynchronousComputeFunction != null));

            // Invariant #5: thou shalt never have a synchronous computation function but not an
            // asynchronous one
            Contract.ThrowIfTrue(this.asynchronousComputeFunction == null && this.synchronousComputeFunction != null);
        }

        #endregion

        public override bool TryGetValue(out T result)
        {
            // No need to lock here since this is only a fast check to 
            // see if the result is already computed.
            if (cachedResult != null)
            {
                result = cachedResult.Result;
                return true;
            }

            result = default(T);
            return false;
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Request request = null;
            AsynchronousComputationToStart? newAsynchronousComputation = null;

            using (TakeLock(cancellationToken))
            {
                // If cached, get immediately
                if (cachedResult != null)
                {
                    return cachedResult.Result;
                }

                // If there is an existing computation active, we'll just create another request
                if (computationActive)
                {
                    request = CreateNewRequest_NoLock();
                }
                else if (synchronousComputeFunction == null)
                {
                    // A synchronous request, but we have no synchronous function. Start off the async work
                    request = CreateNewRequest_NoLock();

                    newAsynchronousComputation = RegisterAsynchronousComputation_NoLock();
                }
                else
                {
                    // We will do the computation here
                    this.computationActive = true;
                }
            }

            // If we simply created a new asynchronous request, so wait for it. Yes, we're blocking the thread
            // but we don't want multiple threads attempting to compute the same thing.
            if (request != null)
            {
                request.RegisterForCancellation(OnAsynchronousRequestCancelled, cancellationToken);

                // Since we already registered for cancellation, it's possible that the registration has
                // cancelled this new computation if we were the only requestor.
                if (newAsynchronousComputation != null)
                {
                    StartAsynchronousComputation(newAsynchronousComputation.Value, requestToCompleteSynchronously: request);
                }

                return request.Task.WaitAndGetResult(cancellationToken);
            }
            else
            {
                T result;

                // We are the active computation, so let's go ahead and compute.
                try
                {
                    result = synchronousComputeFunction(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // This cancelled for some reason. We don't care why, but
                    // it means anybody else waiting for this result isn't going to get it
                    // from us.
                    using (TakeLock(CancellationToken.None))
                    {
                        this.computationActive = false;

                        if (requests != null)
                        {
                            // There's a possible improvement here: there might be another synchronous caller who
                            // also wants the value. We might consider stealing their thread rather than punting
                            // to the thread pool.
                            newAsynchronousComputation = RegisterAsynchronousComputation_NoLock();
                        }
                    }

                    if (newAsynchronousComputation != null)
                    {
                        StartAsynchronousComputation(newAsynchronousComputation.Value, requestToCompleteSynchronously: null);
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    // We faulted for some unknown reason. We should simply fault everything.
                    TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
                    tcs.SetException(ex);
                    CompleteWithTask(tcs.Task, CancellationToken.None);

                    throw;
                }

                // We have a value, so complete
                CompleteWithTask(Task.FromResult(result), CancellationToken.None);

                return result;
            }
        }

        private Request CreateNewRequest_NoLock()
        {
            if (this.requests == null)
            {
                this.requests = new HashSet<Request>();
            }

            Request request = new Request();
            this.requests.Add(request);
            return request;
        }

        public override Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            // Optimization: if we're already cancelled, do not pass go
            if (cancellationToken.IsCancellationRequested)
            {
                return new Task<T>(() => default(T), cancellationToken);
            }

            Request request;
            AsynchronousComputationToStart? newAsynchronousComputation = null;

            using (TakeLock(cancellationToken))
            {
                // If cached, get immediately
                if (cachedResult != null)
                {
                    return cachedResult;
                }

                request = CreateNewRequest_NoLock();

                // If we have either synchronous or asynchronous work current in flight, we don't need to do anything.
                // Otherwise, we shall start an asynchronous computation for this
                if (!computationActive)
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
                StartAsynchronousComputation(newAsynchronousComputation.Value, requestToCompleteSynchronously: request);
            }

            return request.Task;
        }

        private AsynchronousComputationToStart RegisterAsynchronousComputation_NoLock()
        {
            Contract.ThrowIfTrue(this.computationActive);

            this.asynchronousComputationCancellationSource = new CancellationTokenSource();
            this.computationActive = true;

            return new AsynchronousComputationToStart(this.asynchronousComputeFunction, this.asynchronousComputationCancellationSource);
        }

        private struct AsynchronousComputationToStart
        {
            public readonly Func<CancellationToken, Task<T>> AsynchronousComputeFunction;
            public readonly CancellationTokenSource CancellationTokenSource;

            public AsynchronousComputationToStart(Func<CancellationToken, Task<T>> asynchronousComputeFunction, CancellationTokenSource cancellationTokenSource)
            {
                this.AsynchronousComputeFunction = asynchronousComputeFunction;
                this.CancellationTokenSource = cancellationTokenSource;
            }
        }

        private void StartAsynchronousComputation(AsynchronousComputationToStart computationToStart, Request requestToCompleteSynchronously)
        {
            var cancellationToken = computationToStart.CancellationTokenSource.Token;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // DO NOT ACCESS ANY FIELDS OR STATE BEYOND THIS POINT. Since this function
                // runs unsynchronized, it's possible that during this function this request
                // might be cancelled, and then a whole additional request might start and
                // complete inline, and cache the result. By grabbing state before we check
                // the cancellation token, we can be assured that we are only operating on
                // a state that was complete.
                try
                {
                    // We avoid creating a full closure just to pass the token along
                    // Also, use TaskContinuationOptions.ExecuteSynchronously so that we inline 
                    // the continuation if asynchronousComputeFunction completes synchronously
                    var task = computationToStart.AsynchronousComputeFunction(cancellationToken);

                    task.ContinueWith(
                        (t, s) => CompleteWithTask(t, ((CancellationTokenSource)s).Token),
                        computationToStart.CancellationTokenSource,
                        cancellationToken,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);

                    if (requestToCompleteSynchronously != null && task.IsCompleted)
                    {
                        using (TakeLock(CancellationToken.None))
                        {
                            task = GetCachedValueAndCacheThisValueIfNoneCached_NoLock(task);
                        }

                        requestToCompleteSynchronously.CompleteFromTaskSynchronously(task);
                    }
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }
            catch (OperationCanceledException oce) when (CrashIfCanceledWithDifferentToken(oce, cancellationToken))
            {
                // As long as it's the right token, this means that our thread was the first thread
                // to start an asynchronous computation, but the requestor cancelled as we were starting up
                // the computation.
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool CrashIfCanceledWithDifferentToken(OperationCanceledException exception, CancellationToken cancellationToken)
        {
            if (exception.CancellationToken != cancellationToken)
            {
                FatalError.Report(exception);
            }

            return false;
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
                requestsToComplete = this.requests ?? SpecializedCollections.EmptyEnumerable<Request>();
                this.requests = null;

                // The computations are done
                this.asynchronousComputationCancellationSource = null;
                this.computationActive = false;
                task = GetCachedValueAndCacheThisValueIfNoneCached_NoLock(task);
            }

            foreach (var requestToComplete in requestsToComplete)
            {
                requestToComplete.CompleteFromTaskAsynchronously(task);
            }
        }

        private Task<T> GetCachedValueAndCacheThisValueIfNoneCached_NoLock(Task<T> task)
        {
            if (this.cachedResult != null)
            {
                return this.cachedResult;
            }
            else
            {
                if (cacheResult && task.Status == TaskStatus.RanToCompletion)
                {
                    // Hold onto the completed task. We can get rid of the computation functions for good
                    this.cachedResult = task;
                    this.asynchronousComputeFunction = null;
                    this.synchronousComputeFunction = null;
                }

                return task;
            }
        }

        private void OnAsynchronousRequestCancelled(object state)
        {
            var request = (Request)state;
            CancellationTokenSource cancellationTokenSource = null;

            using (TakeLock(CancellationToken.None))
            {
                // Now try to remove it. It's possible that requests may already be null. You could
                // imagine that cancellation was requested, but before we could aquire the lock
                // here the computation completed and the entire CompleteWithTask synchronized
                // block ran. In that case, the requests collection may already be null, or it
                // (even scarier!) may have been replaced with another collection because another
                // computation has started.
                if (this.requests != null)
                {
                    if (this.requests.Remove(request))
                    {
                        if (this.requests.Count == 0)
                        {
                            this.requests = null;

                            if (this.asynchronousComputationCancellationSource != null)
                            {
                                cancellationTokenSource = this.asynchronousComputationCancellationSource;
                                this.asynchronousComputationCancellationSource = null;
                                this.computationActive = false;
                            }
                        }
                    }
                }
            }

            request.CancelAsynchronously();

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        // Using inheritance instead of wrapping a TaskCompletionSource to avoid a second allocation
        private class Request : TaskCompletionSource<T>
        {
            private CancellationTokenRegistration cancellationTokenRegistration;

            public void RegisterForCancellation(Action<object> callback, CancellationToken cancellationToken)
            {
                cancellationTokenRegistration = cancellationToken.Register(callback, this);
            }

            public void CompleteFromTaskAsynchronously(Task<T> task)
            {
                System.Threading.Tasks.Task.Factory.StartNew(CompleteFromTaskSynchronouslyStub, task, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            }

            private void CompleteFromTaskSynchronouslyStub(object task)
            {
                CompleteFromTaskSynchronously((Task<T>)task);
            }

            public void CompleteFromTaskSynchronously(Task<T> task)
            {
                if (task.Status == TaskStatus.RanToCompletion)
                {
                    if (TrySetResult(task.Result))
                    {
                        cancellationTokenRegistration.Dispose();
                    }
                }
                else if (task.Status == TaskStatus.Faulted)
                {
                    if (TrySetException(task.Exception))
                    {
                        cancellationTokenRegistration.Dispose();
                    }
                }
                else
                {
                    CancelSynchronously();
                }
            }

            public void CancelAsynchronously()
            {
                // Since there could be synchronous continuations on the TaskCancellationSource, we queue this to the threadpool
                // to avoid inline running of other operations.
                System.Threading.Tasks.Task.Factory.StartNew(CancelSynchronously, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            }

            private void CancelSynchronously()
            {
                if (TrySetCanceled())
                {
                    // Paranoia: the only reason we should ever get here is if the CancellationToken that
                    // we registered against was cancelled, but just in case, dispose the registration
                    cancellationTokenRegistration.Dispose();
                }
            }
        }
    }
}