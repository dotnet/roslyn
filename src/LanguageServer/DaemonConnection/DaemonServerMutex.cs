// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.Daemon;

/// <summary>
/// The "server" mutex that signals a language server daemon is running for a given pipe. The daemon holds it
/// open for its lifetime; clients (and tests) check its existence to decide whether a daemon already exists.
/// <para>
/// The mutex is held unlocked (<c>initiallyOwned: false</c>): clients only check existence via
/// <see cref="Mutex.TryOpenExisting(string, out Mutex)"/>, which is satisfied by an open handle. Holding it
/// unlocked also avoids <see cref="Mutex"/>'s thread-affinity requirement on release, which is incompatible
/// with the daemon's async accept loop resuming on arbitrary threads (disposing the handle is thread-safe).
/// </para>
/// </summary>
internal static class DaemonServerMutex
{
    /// <summary>
    /// Attempts to become the daemon for <paramref name="pipeName"/> by opening the server mutex. Returns
    /// <see langword="true"/> and the held mutex if this process is the first; <see langword="false"/> if a
    /// daemon already owns it. The returned mutex must be disposed when the daemon shuts down.
    /// </summary>
    public static bool TryAcquire(string pipeName, [NotNullWhen(true)] out Mutex? mutex)
    {
        var mutexName = DaemonPipeName.GetServerMutexName(pipeName);
        var candidate = new Mutex(initiallyOwned: false, mutexName, out var createdNew);
        if (!createdNew)
        {
            candidate.Dispose();
            mutex = null;
            return false;
        }

        mutex = candidate;
        return true;
    }

    /// <summary>
    /// Returns whether a daemon currently holds the server mutex for <paramref name="pipeName"/>.
    /// </summary>
    public static bool IsRunning(string pipeName)
    {
        var mutexName = DaemonPipeName.GetServerMutexName(pipeName);
        Mutex? mutex = null;
        try
        {
            return Mutex.TryOpenExisting(mutexName, out mutex);
        }
        catch
        {
            // If we failed to open the mutex for any reason, assume no daemon is running.
            return false;
        }
        finally
        {
            mutex?.Dispose();
        }
    }
}
