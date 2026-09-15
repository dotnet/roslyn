// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

/// <summary>
/// Toggles whether this process's standard input, output, and error handles are inheritable by child processes. Used
/// around both stages of the daemon double launch (this thin client launching the bootstrap, and the bootstrap
/// launching the daemon).
/// <para>
/// On Windows a <see cref="System.Diagnostics.Process"/> started with any redirected stream is created with
/// <c>CreateProcess(bInheritHandles: true)</c>, which leaks <em>all</em> of this process's inheritable handles - in
/// particular its own standard handles - to the child. In the daemon launch chain (thin client → bootstrap → daemon)
/// those standard handles are the editor's LSP stdio pipes; if the long-lived daemon inherits copies of them it holds
/// them open after this process exits (so the editor's <c>WaitForExit</c>/output draining never sees EOF) and, in
/// stdio mode, corrupts the editor's LSP channel. Marking the standard handles non-inheritable across the launch
/// prevents the child from receiving them, while the freshly created redirection pipes (which the runtime sets up
/// separately) are unaffected. A no-op off Windows, where redirected children don't leak the parent's standard handles.
/// </para>
/// TODO - Switch to ProcessStartInfo.InheritedHandles when we upgrade to .NET 11 which allows us to configure handle inheritance directly.
/// </summary>
internal static class DaemonHandleInheritance
{
    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;
    private const int STD_ERROR_HANDLE = -12;
    private const uint HANDLE_FLAG_INHERIT = 0x00000001;

    private static readonly IntPtr s_invalidHandleValue = new(-1);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

    /// <summary>
    /// On Windows, sets whether this process's standard input, output, and error handles are inheritable by child
    /// processes. Must be paired (set <see langword="false"/> before <c>Process.Start</c>, restore <see langword="true"/>
    /// after). A no-op off Windows.
    /// </summary>
    public static void SetStandardHandlesInheritable(bool inheritable)
    {
        if (!OperatingSystem.IsWindows())
            return;

        var flags = inheritable ? HANDLE_FLAG_INHERIT : 0u;
        SetInheritable(STD_INPUT_HANDLE, flags);
        SetInheritable(STD_OUTPUT_HANDLE, flags);
        SetInheritable(STD_ERROR_HANDLE, flags);

        static void SetInheritable(int stdHandle, uint flags)
        {
            var handle = GetStdHandle(stdHandle);
            if (handle != IntPtr.Zero && handle != s_invalidHandleValue)
                _ = SetHandleInformation(handle, HANDLE_FLAG_INHERIT, flags);
        }
    }
}
