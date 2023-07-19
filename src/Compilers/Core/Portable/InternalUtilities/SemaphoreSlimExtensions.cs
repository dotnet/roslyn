// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static class SemaphoreSlimExtensions
    {
        public static SemaphoreDisposer DisposableWait(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            semaphore.Wait(cancellationToken);
            return new SemaphoreDisposer(semaphore);
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/36114", OftenCompletesSynchronously = true)]
        public static async ValueTask<SemaphoreDisposer> DisposableWaitAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new SemaphoreDisposer(semaphore);
        }

        [NonCopyable]
        internal struct SemaphoreDisposer : IDisposable
        {
            private SemaphoreSlim? _semaphore;

            public SemaphoreDisposer(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                // Officially, Dispose() being called more than once is allowable, but in this case
                // if that were to ever happen that means something is very, very wrong. Since it's an internal
                // type, better to be strict.

                // Nulling this out also means it's a bit easier to diagnose some async deadlocks; if you have an
                // async deadlock where a SemaphoreSlim is held but you're unsure why, as long all the users of the
                // SemaphoreSlim used the Disposable helpers, you can search memory and find the instance that
                // is pointing to the SemaphoreSlim that hasn't nulled out this field yet; in that case you know
                // that's holding the lock and can figure out who is holding that SemaphoreDisposer.
                var semaphoreToDispose = Interlocked.Exchange(ref _semaphore, null);

                if (semaphoreToDispose is null)
                {
                    throw new ObjectDisposedException($"Somehow a {nameof(SemaphoreDisposer)} is being disposed twice.");
                }

                semaphoreToDispose.Release();
            }
        }
    }
}
