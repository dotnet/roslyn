// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
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
    internal abstract class WeaklyCachedRecoverableValueSource<T> : ValueSource<T> where T : class
    {
        private SemaphoreSlim? _lazyGate; // Lazily created. Access via the Gate property
        private bool _saved;
        private WeakReference<T>? _weakReference;
        private ValueSource<T> _recoverySource;

        public WeaklyCachedRecoverableValueSource(ValueSource<T> initialValue)
            => _recoverySource = initialValue;

        public WeaklyCachedRecoverableValueSource(WeaklyCachedRecoverableValueSource<T> savedSource)
        {
            Contract.ThrowIfFalse(savedSource._saved);
            Contract.ThrowIfFalse(savedSource.GetType() == GetType());

            _saved = true;
            _recoverySource = new AsyncLazy<T>(RecoverAsync, Recover, cacheResult: false);
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

        private SemaphoreSlim Gate => LazyInitialization.EnsureInitialized(ref _lazyGate, SemaphoreSlimFactory.Instance);

#pragma warning disable CS8610 // Nullability of reference types in type of parameter doesn't match overridden member. (The compiler incorrectly identifies this as a change.)
        public override bool TryGetValue([NotNullWhen(true)] out T? value)
#pragma warning restore CS8610 // Nullability of reference types in type of parameter doesn't match overridden member.
        {
            // It has 2 fields that can hold onto the value. if we only check weakInstance, we will
            // return false for the initial case where weakInstance is set to s_noReference even if
            // value can be retrieved from _recoverySource. so we check both here.
            var weakReference = _weakReference;
            return weakReference != null && weakReference.TryGetTarget(out value) ||
                   _recoverySource.TryGetValue(out value);
        }

        public override T GetValue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var weakReference = _weakReference;
            if (weakReference == null || !weakReference.TryGetTarget(out var instance))
            {
                Task? saveTask = null;
                using (Gate.DisposableWait(cancellationToken))
                {
                    if (_weakReference == null || !_weakReference.TryGetTarget(out instance))
                    {
                        instance = _recoverySource.GetValue(cancellationToken);
                        saveTask = EnsureInstanceIsSavedAsync(instance);
                    }
                }

                if (saveTask != null)
                {
                    ResetRecoverySource(saveTask, instance);
                }
            }

            return instance;
        }

        public override async Task<T> GetValueAsync(CancellationToken cancellationToken)
        {
            var weakReference = _weakReference;
            if (weakReference == null || !weakReference.TryGetTarget(out var instance))
            {
                Task? saveTask = null;
                using (await Gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_weakReference == null || !_weakReference.TryGetTarget(out instance))
                    {
                        instance = await _recoverySource.GetValueAsync(cancellationToken).ConfigureAwait(false);
                        saveTask = EnsureInstanceIsSavedAsync(instance);
                    }
                }

                if (saveTask != null)
                {
                    ResetRecoverySource(saveTask, instance);
                }
            }

            return instance;
        }

        private void ResetRecoverySource(Task saveTask, T instance)
        {
            saveTask.SafeContinueWith(t =>
            {
                using (Gate.DisposableWait(CancellationToken.None))
                {
                    _recoverySource = new AsyncLazy<T>(RecoverAsync, Recover, cacheResult: false);

                    // Need to keep instance alive until recovery source is updated.
                    GC.KeepAlive(instance);
                }
            }, TaskScheduler.Default);
        }

        private Task? EnsureInstanceIsSavedAsync(T instance)
        {
            if (_weakReference == null)
            {
                _weakReference = new WeakReference<T>(instance);
            }
            else
            {
                _weakReference.SetTarget(instance);
            }

            if (!_saved)
            {
                _saved = true;
                using (s_taskGuard.DisposableWait())
                {
                    // force all save tasks to be in sequence so we don't hog all the threads
                    s_latestTask = s_latestTask.SafeContinueWithFromAsync(t =>
                         SaveAsync(instance, CancellationToken.None), CancellationToken.None, TaskScheduler.Default);
                    return s_latestTask;
                }
            }

#pragma warning disable VSTHRD114 // Avoid returning a null Task (False positive: https://github.com/microsoft/vs-threading/issues/637)
            return null;
#pragma warning restore VSTHRD114 // Avoid returning a null Task
        }
    }
}
