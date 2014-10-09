// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This class is a ValueSource that manages storing an item in a cache,
    /// saving the item's state when it is evicted from the cache and recovering
    /// it from its saved state when the value source is accessed again.
    /// </summary>
    internal abstract class RecoverableCachedObjectSource<T> : ValueSource<T> where T : class
    {
        private readonly IObjectCache<T> cache;
        private readonly IWeakAction<T> evictAction;

        private AsyncSemaphore gateDoNotAccessDirectly; // Lazily created. Access via the Gate property
        private bool saved;
        private WeakReference<T> weakInstance;
        private ValueSource<T> recoverySource;

        private static readonly WeakReference<T> NoReference = new WeakReference<T>(null);

        public RecoverableCachedObjectSource(ValueSource<T> initialValue, IObjectCache<T> cache)
        {
            this.weakInstance = NoReference;
            this.recoverySource = initialValue;
            this.cache = cache;

            this.evictAction = new WeakAction<RecoverableCachedObjectSource<T>, T>(this, (o, d) => o.OnEvicted(d));
        }

        /// <summary>
        /// Override this to save the state of the instance so it can be recovered.
        /// This method will only ever be called once.
        /// </summary>
        protected abstract Task SaveAsync(T instance, CancellationToken cancellationToken);

        /// <summary>
        /// Override this method to implement asynchronous recovery semantics.
        /// This method may be called multiple times.
        /// </summary>
        protected abstract Task<T> RecoverAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Override this method to implement synchronous recovery semantics.
        /// This method may be called multiple times.
        /// </summary>
        protected abstract T Recover(CancellationToken cancellationToken);

        // enfore saving in a queue so save's don't overload the thread pool.
        private static Task latestTask = SpecializedTasks.EmptyTask;
        private static readonly NonReentrantLock taskGuard = new NonReentrantLock();

        private AsyncSemaphore Gate
        {
            get
            {
                return LazyInitialization.EnsureInitialized(ref this.gateDoNotAccessDirectly, AsyncSemaphore.Factory);
            }
        }

        private async void OnEvicted(T instance)
        {
            if (!saved)
            {
                using (await this.Gate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(false))
                {
                    if (!saved)
                    {
                        Task saveTask;

                        using (taskGuard.DisposableWait())
                        {
                            // force all save tasks to be in sequence so we don't hog all the threads
                            saveTask = latestTask = latestTask.SafeContinueWithFromAsync(t =>
                                this.SaveAsync(instance, CancellationToken.None), CancellationToken.None, TaskScheduler.Default);
                        }

                        // wait for this save to be done
                        await saveTask.ConfigureAwait(false);

                        this.saved = true;
                    }
                }
            }
        }

        public override bool TryGetValue(out T value)
        {
            if (this.weakInstance.TryGetTarget(out value))
            {
                // let the cache know the instance was accessed
                this.cache.AddOrAccess(value, this.evictAction);

                return true;
            }

            return false;
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            T instance;
            if (!this.weakInstance.TryGetTarget(out instance))
            {
                using (this.Gate.DisposableWait())
                {
                    if (!this.weakInstance.TryGetTarget(out instance))
                    {
                        instance = this.recoverySource.GetValue(cancellationToken);
                        this.weakInstance = new WeakReference<T>(instance);
                        this.recoverySource = new AsyncLazy<T>(this.RecoverAsync, this.Recover, cacheResult: false);
                    }
                }
            }

            // let the cache know the instance was accessed
            this.cache.AddOrAccess(instance, this.evictAction);

            return instance;
        }

        public override async Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            T instance;
            if (!this.weakInstance.TryGetTarget(out instance))
            {
                using (await this.Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!this.weakInstance.TryGetTarget(out instance))
                    {
                        // attempt to access the initial value or recover it
                        instance = await this.recoverySource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        this.weakInstance = new WeakReference<T>(instance);
                        this.recoverySource = new AsyncLazy<T>(this.RecoverAsync, this.Recover, cacheResult: false);
                    }
                }
            }

            // let the cache know the instance was accessed
            this.cache.AddOrAccess(instance, this.evictAction);

            return instance;
        }
    }
}