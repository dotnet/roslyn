// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

/// <summary>
/// The "client" mutex held by a connecting thin client to serialize the check-server-then-launch sequence so
/// two clients can't race to start two daemons.
/// <para>
/// <see cref="WaitHandle.WaitOne(TimeSpan)"/> / <see cref="Mutex.ReleaseMutex"/> must occur on the same thread, so
/// callers must keep the acquire/release on a single thread (i.e. avoid <c>await</c> between
/// <see cref="TryAcquire"/> and <see cref="Dispose"/>).
/// </para>
/// </summary>
internal sealed class DaemonClientMutex : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    private DaemonClientMutex(Mutex mutex)
    {
        _mutex = mutex;
    }

    /// <summary>
    /// Attempts to acquire the mutex within the given timeout. The returned instance owns the mutex and must be
    /// disposed on the same thread.
    /// </summary>
    public static bool TryAcquire(string pipeName, TimeSpan timeout, [NotNullWhen(true)] out DaemonClientMutex? mutex)
    {
        var candidate = new Mutex(initiallyOwned: false, DaemonPipeName.GetClientMutexName(pipeName), DaemonPipeName.MutexOptions);

        try
        {
            try
            {
                if (!candidate.WaitOne(timeout))
                {
                    candidate.Dispose();
                    mutex = null;
                    return false;
                }
            }
            catch (AbandonedMutexException)
            {
                // The previous owner exited without releasing the mutex; we now own it.
            }

            mutex = new DaemonClientMutex(candidate);
            return true;
        }
        catch
        {
            candidate.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        try
        {
            _mutex.ReleaseMutex();
        }
        finally
        {
            _mutex.Dispose();
        }
    }
}
