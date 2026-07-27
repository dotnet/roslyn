// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal static class DaemonProcessDetach
{
    /// <summary>
    /// On Unix, moves the daemon into a new session and process group via <c>setsid(2)</c>, detaching it from the
    /// controlling terminal and from the launching client's process group. Without this, a terminal close (SIGHUP) or
    /// a signal sent to the launching client's process group would also tear down the shared daemon. This is in
    /// addition to the thin client's bootstrap orphaning the daemon out of the editor's process tree, which is what
    /// protects it from process-tree teardowns on every platform. A no-op on Windows, which has no
    /// equivalent session concept and where the bootstrap orphaning is sufficient.
    /// </summary>
    public static void DetachIntoNewSessionIfUnix(ILogger logger)
    {
        if (OperatingSystem.IsWindows())
            return;

        var sessionId = setsid();
        if (sessionId == -1)
        {
            // The most likely cause is that this process is already a process-group leader (EPERM). The daemon still
            // functions; it just isn't isolated from the launching client's session, so it falls back to relying on
            // the OS not terminating children when their parent exits.
            var errno = Marshal.GetLastPInvokeError();
            logger.LogWarning("Could not detach the daemon into a new session (setsid failed with errno {errno}).", errno);
        }
        else
        {
            logger.LogInformation("Detached daemon into new session {sessionId}.", sessionId);
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int setsid();
}
