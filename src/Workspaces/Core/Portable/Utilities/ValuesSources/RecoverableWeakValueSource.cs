// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This class is a <see cref="ValueSource{T}"/> that holds onto a value weakly, 
    /// but can save its value and recover it on demand if needed.
    /// 
    /// The initial value comes from the <see cref="ValueSource{T}"/> specified in the constructor.
    /// Derived types implement SaveAsync and RecoverAsync.
    /// </summary>
    internal abstract class RecoverableWeakValueSource<T> : ValueSource<T> where T : class
    {
        private SemaphoreSlim _gateDoNotAccessDirectly; // Lazily created. Access via the Gate property
        private bool _saved;
        private WeakReference<T> _weakInstance;
        private ValueSource<T> _recoverySource;

        private static readonly WeakReference<T> s_noReference = new WeakReference<T>(null);

        public RecoverableWeakValueSource(ValueSource<T> initialValue)
        {
            _weakInstance = s_noReference;
            _recoverySource = initialValue;
        }

        public RecoverableWeakValueSource(RecoverableWeakValueSource<T> savedSource)
        {
            Contract.ThrowIfFalse(savedSource._saved);
            Contract.ThrowIfFalse(savedSource.GetType() == this.GetType());

            _saved = true;
            _weakInstance = s_noReference;
            _recoverySource = new AsyncLazy<T>(this.RecoverAsync, this.Recover, cacheResult: false);
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

        // enforce saving in a queue so save's don't overload the thread pool.
        private static Task s_latestTask = Task.CompletedTask;
        private static readonly NonReentrantLock s_taskGuard = new NonReentrantLock();

        private SemaphoreSlim Gate => LazyInitialization.EnsureInitialized(ref _gateDoNotAccessDirectly, SemaphoreSlimFactory.Instance);

        public override bool TryGetValue(out T value)
        {
            // it has 2 fields that can hold onto the value. if we only check weakInstance, we will
            // return false for the initial case where weakInstance is set to s_noReference even if
            // value can be retrieved from _recoverySource. so we check both here.
            return _weakInstance.TryGetTarget(out value) ||
                   _recoverySource.TryGetValue(out value);
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_weakInstance.TryGetTarget(out var instance))
            {
                Task saveTask = null;
                using (this.Gate.DisposableWait(cancellationToken))
                {
                    if (!_weakInstance.TryGetTarget(out instance))
                    {
                        instance = _recoverySource.GetValue(cancellationToken);
                        saveTask = EnsureInstanceIsSaved(instance);
                    }
                }

                ResetRecoverySource(saveTask, instance);
            }

            return instance;
        }

        public override async Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            if (!_weakInstance.TryGetTarget(out var instance))
            {
                Task saveTask = null;
                using (await this.Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!_weakInstance.TryGetTarget(out instance))
                    {
                        instance = await _recoverySource.GetValueAsync(cancellationToken).ConfigureAwait(false);
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
                    using (this.Gate.DisposableWait(CancellationToken.None))
                    {
                        _recoverySource = new AsyncLazy<T>(this.RecoverAsync, this.Recover, cacheResult: false);

                        // Need to keep instance alive until recovery source is updated.
                        GC.KeepAlive(instance);
                    }
                }, TaskScheduler.Default);
            }
        }

        private Task EnsureInstanceIsSaved(T instance)
        {
            if (_weakInstance == s_noReference)
            {
                _weakInstance = new WeakReference<T>(instance);
            }
            else
            {
                _weakInstance.SetTarget(instance);
            }

            if (!_saved)
            {
                _saved = true;
                using (s_taskGuard.DisposableWait())
                {
                    // force all save tasks to be in sequence so we don't hog all the threads
                    s_latestTask = s_latestTask.SafeContinueWithFromAsync(t =>
                         this.SaveAsync(instance, CancellationToken.None), CancellationToken.None, TaskScheduler.Default);
                    return s_latestTask;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
