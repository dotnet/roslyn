// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

#if WORKSPACE
using Microsoft.CodeAnalysis.Internal.Log;
#endif

namespace Roslyn.Utilities
{
    /// <summary>
    /// A lightweight mutual exclusion object which supports waiting with cancellation and prevents
    /// recursion (i.e. you may not call Wait if you already hold the lock)
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="NonReentrantLock"/> provides a lightweight mutual exclusion class that doesn't
    /// use Windows kernel synchronization primitives.
    /// </para>
    /// <para>
    /// The implementation is distilled from the workings of <see cref="SemaphoreSlim"/>
    /// The basic idea is that we use a regular sync object (Monitor.Enter/Exit) to guard the setting
    /// of an 'owning thread' field. If, during the Wait, we find the lock is held by someone else
    /// then we register a cancellation callback and enter a "Monitor.Wait" loop. If the cancellation
    /// callback fires, then it "pulses" all the waiters to wake them up and check for cancellation.
    /// Waiters are also "pulsed" when leaving the lock.
    /// </para>
    /// <para>
    /// All public members of <see cref="NonReentrantLock"/> are thread-safe and may be used concurrently
    /// from multiple threads.
    /// </para>
    /// </remarks>
    internal sealed class NonReentrantLock
    {
        /// <summary>
        /// A synchronization object to protect access to the <see cref="_owningThreadId"/> field and to be pulsed
        /// when <see cref="Release"/> is called and during cancellation.
        /// </summary>
        private readonly object _syncLock;

        /// <summary>
        /// The <see cref="Environment.CurrentManagedThreadId" /> of the thread that holds the lock. Zero if no thread is holding
        /// the lock.
        /// </summary>
        private volatile int _owningThreadId;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="useThisInstanceForSynchronization">If false (the default), then the class
        /// allocates an internal object to be used as a sync lock.
        /// If true, then the sync lock object will be the NonReentrantLock instance itself. This
        /// saves an allocation but a client may not safely further use this instance in a call to
        /// Monitor.Enter/Exit or in a "lock" statement.
        /// </param>
        public NonReentrantLock(bool useThisInstanceForSynchronization = false)
        {
            _syncLock = useThisInstanceForSynchronization ? this : new object();
        }

        /// <summary>
        /// Shared factory for use in lazy initialization.
        /// </summary>
        public static readonly Func<NonReentrantLock> Factory = () => new NonReentrantLock(useThisInstanceForSynchronization: true);

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="NonReentrantLock"/>, while observing a
        /// <see cref="CancellationToken"/>.
        /// </summary>
        /// <remarks>
        /// Recursive locking is not supported. i.e. A thread may not call Wait successfully twice without an
        /// intervening <see cref="Release"/>.
        /// </remarks>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> token to
        /// observe.</param>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was
        /// canceled.</exception>
        /// <exception cref="LockRecursionException">The caller already holds the lock</exception>
        public void Wait(CancellationToken cancellationToken = default)
        {
            if (this.IsOwnedByMe)
            {
                throw new LockRecursionException();
            }

            CancellationTokenRegistration cancellationTokenRegistration = default;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Fast path to try and avoid allocations in callback registration.
                lock (_syncLock)
                {
                    if (!this.IsLocked)
                    {
                        this.TakeOwnership();
                        return;
                    }
                }

                cancellationTokenRegistration = cancellationToken.Register(s_cancellationTokenCanceledEventHandler, _syncLock, useSynchronizationContext: false);
            }

            using (cancellationTokenRegistration)
            {
                // PERF: First spin wait for the lock to become available, but only up to the first planned yield.
                // This additional amount of spinwaiting was inherited from SemaphoreSlim's implementation where
                // it showed measurable perf gains in test scenarios.
                var spin = new SpinWait();
                while (this.IsLocked && !spin.NextSpinWillYield)
                {
                    spin.SpinOnce();
                }

                lock (_syncLock)
                {
                    while (this.IsLocked)
                    {
                        // If cancelled, we throw. Trying to wait could lead to deadlock.
                        cancellationToken.ThrowIfCancellationRequested();
#if WORKSPACE
                        using (Logger.LogBlock(FunctionId.Misc_NonReentrantLock_BlockingWait, cancellationToken))
#endif
                        {
                            // Another thread holds the lock. Wait until we get awoken either
                            // by some code calling "Release" or by cancellation.
                            Monitor.Wait(_syncLock);
                        }
                    }

                    // We now hold the lock
                    this.TakeOwnership();
                }
            }
        }

        /// <summary>
        /// Exit the mutual exclusion.
        /// </summary>
        /// <remarks>
        /// The calling thread must currently hold the lock.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The lock is not currently held by the calling thread.</exception>
        public void Release()
        {
            AssertHasLock();

            lock (_syncLock)
            {
                this.ReleaseOwnership();

                // Release one waiter
                Monitor.Pulse(_syncLock);
            }
        }

        /// <summary>
        /// Determine if the lock is currently held by the calling thread.
        /// </summary>
        /// <returns>True if the lock is currently held by the calling thread.</returns>
        public bool LockHeldByMe()
        {
            return this.IsOwnedByMe;
        }

        /// <summary>
        /// Throw an exception if the lock is not held by the calling thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">The lock is not currently held by the calling thread.</exception>
        public void AssertHasLock()
        {
            Contract.ThrowIfFalse(LockHeldByMe());
        }

        /// <summary>
        /// Checks if the lock is currently held.
        /// </summary>
        private bool IsLocked
        {
            get
            {
                return _owningThreadId != 0;
            }
        }

        /// <summary>
        /// Checks if the lock is currently held by the calling thread.
        /// </summary>
        private bool IsOwnedByMe
        {
            get
            {
                return _owningThreadId == Environment.CurrentManagedThreadId;
            }
        }

        /// <summary>
        /// Take ownership of the lock (by the calling thread). The lock may not already
        /// be held by any other code.
        /// </summary>
        private void TakeOwnership()
        {
            Debug.Assert(!this.IsLocked);
            _owningThreadId = Environment.CurrentManagedThreadId;
        }

        /// <summary>
        /// Release ownership of the lock. The lock must already be held by the calling thread.
        /// </summary>
        private void ReleaseOwnership()
        {
            Debug.Assert(this.IsOwnedByMe);
            _owningThreadId = 0;
        }

        /// <summary>
        /// Action object passed to a cancellation token registration.
        /// </summary>
        private static readonly Action<object> s_cancellationTokenCanceledEventHandler = CancellationTokenCanceledEventHandler;

        /// <summary>
        /// Callback executed when a cancellation token is canceled during a Wait.
        /// </summary>
        /// <param name="obj">The syncLock that protects a <see cref="NonReentrantLock"/> instance.</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            lock (obj)
            {
                // Release all waiters to check their cancellation tokens.
                Monitor.PulseAll(obj);
            }
        }

        public SemaphoreDisposer DisposableWait(CancellationToken cancellationToken = default)
        {
            this.Wait(cancellationToken);
            return new SemaphoreDisposer(this);
        }

        /// <summary>
        /// Since we want to avoid boxing the return from <see cref="NonReentrantLock.DisposableWait"/>, this type must be public.
        /// </summary>
        public struct SemaphoreDisposer : IDisposable
        {
            private readonly NonReentrantLock _semaphore;

            public SemaphoreDisposer(NonReentrantLock semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
