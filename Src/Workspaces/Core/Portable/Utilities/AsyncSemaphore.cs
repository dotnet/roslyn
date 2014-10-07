// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A semaphore that supports a WaitAsync() operation. Unlike SemaphoreSlim, WaitAsync
    /// does not have bugs where the semaphore can be acquired but WaitAsync returns cancelled.
    /// </summary>
    internal sealed class AsyncSemaphore
    {
        private SemaphoreSlim semaphoreDontAccessDirectly; // Initialized lazily. Access via the Semaphore property
        private HeldSemaphore releaserDontAccessDirectly; // Initialized lazily. Access via the Releaser property
        private Task<IDisposable> completedDontAccessDirectly; // Initialized lazily. Access via the Completed property

        public AsyncSemaphore(int initialCount)
        {
            // An initialCount of 1 is very typical and, in that case, the semaphore will be created
            // lazily. Otherwise, create it now.
            if (initialCount != 1)
            {
                semaphoreDontAccessDirectly = new SemaphoreSlim(initialCount);
            }
        }

        /// <summary>
        /// Factory object that may be used for lazy initialization. Creates AsyncSemaphore instances with an initial count of 1.
        /// </summary>
        public static readonly Func<AsyncSemaphore> Factory = () => new AsyncSemaphore(initialCount: 1);

        public Task WaitAsync(CancellationToken cancellationToken)
        {
            // Slimy: just call DisposableWaitAsync, and ignore the fact it is a Task<IDisposable>.
            // The Dispose() method is nothing more than a convenient way to call Release(), so it
            // doesn't have to be called.
            return DisposableWaitAsync(cancellationToken);
        }

        private static readonly Func<SemaphoreSlim> semaphoreFactory = () => new SemaphoreSlim(initialCount: 1);

        private SemaphoreSlim Semaphore
        {
            get
            {
                return LazyInitialization.EnsureInitialized(ref semaphoreDontAccessDirectly, semaphoreFactory);
            }
        }

        private static readonly Func<AsyncSemaphore, HeldSemaphore> releaserFactory = a => new HeldSemaphore(a);

        private HeldSemaphore Releaser
        {
            get
            {
                return LazyInitialization.EnsureInitialized(ref releaserDontAccessDirectly, releaserFactory, this);
            }
        }

        private static readonly Func<AsyncSemaphore, Task<IDisposable>> completedFactory = a => Task.FromResult<IDisposable>(a.Releaser);

        private Task<IDisposable> Completed
        {
            get
            {
                return LazyInitialization.EnsureInitialized(ref completedDontAccessDirectly, completedFactory, this);
            }
        }

        public Task<IDisposable> DisposableWaitAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new Task<IDisposable>(() => null, cancellationToken);
            }

            var t = this.Semaphore.WaitAsync();

            if (t.Status == TaskStatus.RanToCompletion)
            {
                return this.Completed;
            }

            if (!cancellationToken.CanBeCanceled)
            {
                return t.ContinueWith((_, s) => (IDisposable)s, this.Releaser,
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            return DisposableWaitAsyncCore(t, cancellationToken);
        }

        private Task<IDisposable> DisposableWaitAsyncCore(Task asyncWaitTask, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<IDisposable>();
            var reg = cancellationToken.Register(() =>
            {
                if (tcs.TrySetCanceled())
                {
                    // We successfully passed the cancellation off to the caller, but we will still
                    // be acquiring the semaphore. We'll need to release once done.
                    asyncWaitTask.ContinueWith((_, s) => ((SemaphoreSlim)s).Release(), this.Semaphore,
                        CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
            });

            asyncWaitTask.ContinueWith(_ =>
            {
                if (tcs.TrySetResult(this.Releaser))
                {
                    reg.Dispose();
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return tcs.Task;
        }

        public IDisposable DisposableWait(CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Semaphore.Wait(cancellationToken);
            return this.Releaser;
        }

        public void Release()
        {
            this.Semaphore.Release();
        }

        private sealed class HeldSemaphore : IDisposable
        {
            private readonly AsyncSemaphore asyncSemaphore;

            public HeldSemaphore(AsyncSemaphore asyncLock)
            {
                this.asyncSemaphore = asyncLock;
            }

            void IDisposable.Dispose()
            {
                this.asyncSemaphore.Release();
            }
        }
    }
}
