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
        private AsyncSemaphore gateDoNotAccessDirectly; // Lazily created. Access via the Gate property
        private bool saved;
        private WeakReference<T> weakInstance;
        private ValueSource<T> recoverySource;

        private static readonly WeakReference<T> NoReference = new WeakReference<T>(null);

        public RecoverableCachedObjectSource(ValueSource<T> initialValue)
        {
            this.weakInstance = NoReference;
            this.recoverySource = initialValue;
        }

        public RecoverableCachedObjectSource(RecoverableCachedObjectSource<T> savedSource)
        {
            Contract.ThrowIfFalse(savedSource.saved);
            Contract.ThrowIfFalse(savedSource.GetType() == this.GetType());

            this.saved = true;
            this.weakInstance = NoReference;
            this.recoverySource = new AsyncLazy<T>(this.RecoverAsync, this.Recover, cacheResult: false);
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

        public override bool TryGetValue(out T value)
        {
            return this.weakInstance.TryGetTarget(out value);
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            T instance;
            if (!this.weakInstance.TryGetTarget(out instance))
            {
                Task saveTask = null;
                using (this.Gate.DisposableWait(cancellationToken))
                {
                    if (!this.weakInstance.TryGetTarget(out instance))
                    {
                        instance = this.recoverySource.GetValue(cancellationToken);
                        saveTask = EnsureInstanceIsSaved(instance);
                    }
                }

                ResetRecoverySource(saveTask, instance);
            }

            return instance;
        }

        public override async Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            T instance;
            if (!this.weakInstance.TryGetTarget(out instance))
            {
                Task saveTask = null;
                using (await this.Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!this.weakInstance.TryGetTarget(out instance))
                    {
                        instance = await this.recoverySource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        saveTask = EnsureInstanceIsSaved(instance);
                    }
                }

                ResetRecoverySource(saveTask, instance);
            }

            return instance;
        }

        private void ResetRecoverySource(Task saveTask, T instance)
        {
            if (saveTask != null)
            {
                saveTask.SafeContinueWith(t =>
                {
                    using (this.Gate.DisposableWait())
                    {
                        this.recoverySource = new AsyncLazy<T>(this.RecoverAsync, this.Recover, cacheResult: false);

                        // Need to keep instance alive until recovery source is updated.
                        GC.KeepAlive(instance);
                    }
                }, TaskScheduler.Default);
            }
        }

        private Task EnsureInstanceIsSaved(T instance)
        {
            if (this.weakInstance == NoReference)
            {
                this.weakInstance = new WeakReference<T>(instance);
            }
            else
            {
                this.weakInstance.SetTarget(instance);
            }

            if (!saved)
            {
                this.saved = true;
                using (taskGuard.DisposableWait())
                {
                    // force all save tasks to be in sequence so we don't hog all the threads
                    latestTask = latestTask.SafeContinueWithFromAsync(t =>
                         this.SaveAsync(instance, CancellationToken.None), CancellationToken.None, TaskScheduler.Default);
                    return latestTask;
                }
            }
            else
            {
                return null;
            }
        }
    }
}