// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static class SemaphoreExtensions
    {
        public static SemaphoreDisposer DisposableWait(this Semaphore semaphore, CancellationToken cancellationToken)
        {
            if (cancellationToken.CanBeCanceled)
            {
                int signalledIndex = WaitHandle.WaitAny(new[] { semaphore, cancellationToken.WaitHandle });
                if (signalledIndex != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw ExceptionUtilities.Unreachable;
                }
            }
            else
            {
                semaphore.WaitOne();
            }

            return new SemaphoreDisposer(semaphore);
        }

        public static Task<SemaphoreDisposer> DisposableWaitAsync(this Semaphore semaphore, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(
                () => DisposableWait(semaphore, cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        internal struct SemaphoreDisposer : IDisposable
        {
            private readonly Semaphore _semaphore;

            public SemaphoreDisposer(Semaphore semaphore)
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
