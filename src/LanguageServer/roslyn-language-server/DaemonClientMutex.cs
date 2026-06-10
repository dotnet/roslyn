// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.LanguageServer.Daemon;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

/// <summary>
/// The "client" mutex briefly held by a connecting client to serialize the check-server-then-launch sequence
/// so two clients can't race to start two daemons. Used only by the thin client.
/// <para>
/// <see cref="TryLock"/> uses <see cref="WaitHandle.WaitOne(int)"/> / <see cref="Mutex.ReleaseMutex"/>, which
/// must occur on the same thread. Callers must keep the acquire/release on a single thread (i.e. avoid
/// <c>await</c> between <see cref="TryLock"/> and <see cref="Dispose"/>).
/// </para>
/// </summary>
internal sealed class DaemonClientMutex : IDisposable
{
    private readonly Mutex _mutex;

    public bool IsDisposed { get; private set; }
    public bool IsLocked { get; private set; }

    public DaemonClientMutex(string pipeName, out bool createdNew)
    {
        var mutexName = DaemonPipeName.GetClientMutexName(pipeName);
        _mutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out createdNew);
        if (createdNew)
            IsLocked = true;
    }

    /// <summary>
    /// Attempts to acquire the mutex within the given timeout. Must be called (and later disposed) on the
    /// same thread.
    /// </summary>
    public bool TryLock(int timeoutMs)
    {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(DaemonClientMutex));
        if (IsLocked)
            throw new InvalidOperationException("Lock already held");

        try
        {
            return IsLocked = _mutex.WaitOne(timeoutMs);
        }
        catch (AbandonedMutexException)
        {
            // The previous owner exited without releasing the mutex; we now own it.
            return IsLocked = true;
        }
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        try
        {
            if (IsLocked)
                _mutex.ReleaseMutex();
        }
        finally
        {
            _mutex.Dispose();
            IsLocked = false;
        }
    }
}
