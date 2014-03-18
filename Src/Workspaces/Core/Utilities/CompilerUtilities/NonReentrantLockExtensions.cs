// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class NonReentrantLockExtensions
    {
        public static SemaphoreDisposer DisposableWait(this NonReentrantLock semaphore, CancellationToken cancellationToken = default(CancellationToken))
        {
            semaphore.Wait(cancellationToken);
            return new SemaphoreDisposer(semaphore);
        }

        internal struct SemaphoreDisposer : IDisposable
        {
            private readonly NonReentrantLock semaphore;

            public SemaphoreDisposer(NonReentrantLock semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Dispose()
            {
                semaphore.Release();
            }
        }
    }
}
