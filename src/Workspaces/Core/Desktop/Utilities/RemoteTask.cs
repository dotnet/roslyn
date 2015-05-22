// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal interface IRemoteTask<T>
    {
        void Cancel();
        void SetCompletion(IRemoteTaskCompletion<T> completion);
        T GetResult();
    }

    internal interface IRemoteTaskCompletion<T>
    {
        void Complete(T result);
        void Fail(Exception exception);
    }

    internal static class RemoteTask
    {
        public static Task<TResult> ToTask<TResult>(this IRemoteTask<TResult> asyncTask, CancellationToken cancellationToken)
        {
            return new RemoteTaskCompletion<TResult>(asyncTask, cancellationToken).Task;
        }
    }

    internal class RemoteTaskCompletion<T> : MarshalByRefObject, IRemoteTaskCompletion<T>
    {
        private readonly TaskCompletionSource<T> _taskSource;

        public RemoteTaskCompletion(IRemoteTask<T> asyncTask, CancellationToken cancellationToken)
        {
            _taskSource = new TaskCompletionSource<T>();

            asyncTask.SetCompletion(this);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    asyncTask.Cancel();
                    _taskSource.TrySetCanceled();
                });
            }
        }

        public Task<T> Task
        {
            get { return _taskSource.Task; }
        }

        void IRemoteTaskCompletion<T>.Complete(T result)
        {
            _taskSource.TrySetResult(result);
        }

        void IRemoteTaskCompletion<T>.Fail(Exception exception)
        {
            _taskSource.TrySetException(exception);
        }
    }

    internal class RemoteTask<TResult> : MarshalByRefObject, IRemoteTask<TResult>
    {
        private readonly Task<TResult> _task;
        private readonly CancellationTokenSource _cancellationSource;

        public RemoteTask(Func<CancellationToken, Task<TResult>> factory)
        {
            _cancellationSource = new CancellationTokenSource();
            _task = factory(_cancellationSource.Token);
        }

        public CancellationToken CancellationToken
        {
            get { return _cancellationSource.Token; }
        }

        void IRemoteTask<TResult>.SetCompletion(IRemoteTaskCompletion<TResult> completion)
        {
            _task.ContinueWith(t =>
            {
                try
                {
                    completion.Complete(t.Result);
                }
                catch (Exception e)
                {
                    var ae = e as AggregateException;
                    if (ae != null && ae.InnerExceptions.Count == 1)
                    {
                        e = ae.InnerException;
                    }

                    completion.Fail(e);
                }
            }, this.CancellationToken, TaskContinuationOptions.NotOnCanceled, TaskScheduler.Default);
        }

        void IRemoteTask<TResult>.Cancel()
        {
            _cancellationSource.Cancel();
        }

        TResult IRemoteTask<TResult>.GetResult()
        {
            try
            {
                return _task.Result;
            }
            catch (AggregateException e) when (e.InnerExceptions.Count == 1)
            {
                throw e.InnerException;
            }
        }
    }
}
