// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

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
        /// A synchronization object to protect access to the <see cref="owningThread"/> field and to be pulsed
        /// when <see cref="Release"/> is called and during cancellation.
        /// </summary>
        private readonly object syncLock;

        /// <summary>
        /// Indicates which thread currently holds the lock. If null, then the lock is available.
        /// </summary>
        private volatile Thread owningThread;

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
            this.syncLock = useThisInstanceForSynchronization ? this : new object();
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
        public void Wait(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.IsOwnedByMe)
            {
                throw new LockRecursionException();
            }

            CancellationTokenRegistration cancellationTokenRegistration = default(CancellationTokenRegistration);

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Fast path to try and avoid allocations in callback registration.
                lock (this.syncLock)
                {
                    if (!this.IsLocked)
                    {
                        this.TakeOwnership();
                        return;
                    }
                }

                cancellationTokenRegistration = cancellationToken.Register(cancellationTokenCanceledEventHandler, this.syncLock, useSynchronizationContext: false);
            }

            using (cancellationTokenRegistration)
            {
                // PERF: First spin wait for the lock to become available, but only up to the first planned yield.
                // This additional amount of spinwaiting was inherited from SemaphoreSlim's implementation where
                // it showed measurable perf gains in test scenarios.
                SpinWait spin = new SpinWait();
                while (this.IsLocked && !spin.NextSpinWillYield)
                {
                    spin.SpinOnce();
                }

                lock (this.syncLock)
                {
                    while (this.IsLocked)
                    {
                        // If cancelled, we throw. Trying to wait could lead to deadlock.
                        cancellationToken.ThrowIfCancellationRequested();

                        using (Logger.LogBlock(FunctionId.Misc_NonReentrantLock_BlockingWait, cancellationToken))
                        {
                            // Another thread holds the lock. Wait until we get awoken either
                            // by some code calling "Release" or by cancellation.
                            Monitor.Wait(this.syncLock);
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
        /// <exception cref="Contract.ContractFailureException">The lock is not currently held by the calling thread.</exception>
        public void Release()
        {
            AssertHasLock();

            lock (this.syncLock)
            {
                this.ReleaseOwnership();

                // Release one waiter
                Monitor.Pulse(this.syncLock);
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
        /// <exception cref="Contract.ContractFailureException">The lock is not currently held by the calling thread.</exception>
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
                return this.owningThread != null;
            }
        }

        /// <summary>
        /// Checks if the lock is currently held by the calling thread.
        /// </summary>
        private bool IsOwnedByMe
        {
            get
            {
                return this.owningThread == Thread.CurrentThread;
            }
        }

        /// <summary>
        /// Take ownership of the lock (by the calling thread). The lock may not already
        /// be held by any other code.
        /// </summary>
        private void TakeOwnership()
        {
            Contract.Assert(!this.IsLocked);
            this.owningThread = Thread.CurrentThread;
        }

        /// <summary>
        /// Release ownership of the lock. The lock must already be held by the calling thread.
        /// </summary>
        private void ReleaseOwnership()
        {
            Contract.Assert(this.IsOwnedByMe);
            this.owningThread = null;
        }

        /// <summary>
        /// Action object passed to a cancellation token registration.
        /// </summary>
        private static readonly Action<object> cancellationTokenCanceledEventHandler = CancellationTokenCanceledEventHandler;

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
    }
}
