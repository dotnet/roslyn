#pragma warning disable IDE0073 // We are preserving the original copyright header for this file

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This was copied from https://github.com/microsoft/vs-threading/blob/4332894cbeaad95797e24004cf3adc5abc5b9be7/src/Microsoft.VisualStudio.Threading/NoMessagePumpSyncContext.cs
// with some changes to reintroduce the P/Invoke directly since we're not using CsWin32.

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Roslyn.Utilities;

/// <summary>
/// A SynchronizationContext whose synchronously blocking Wait method does not allow
/// any reentrancy via the message pump.
/// </summary>
internal sealed class NoMessagePumpSyncContext : SynchronizationContext
{
    /// <summary>
    /// A shared singleton.
    /// </summary>
    private static readonly SynchronizationContext DefaultInstance = new NoMessagePumpSyncContext();

    /// <summary>
    /// Initializes a new instance of the <see cref="NoMessagePumpSyncContext"/> class.
    /// </summary>
    public NoMessagePumpSyncContext()
    {
        // This is required so that our override of Wait is invoked.
        this.SetWaitNotificationRequired();
    }

    /// <summary>
    /// Gets a shared instance of this class.
    /// </summary>
    public static SynchronizationContext Default
    {
        get { return DefaultInstance; }
    }

    /// <summary>
    /// Synchronously blocks without a message pump.
    /// </summary>
    /// <param name="waitHandles">An array of type <see cref="IntPtr" /> that contains the native operating system handles.</param>
    /// <param name="waitAll">true to wait for all handles; false to wait for any handle.</param>
    /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="Timeout.Infinite" /> (-1) to wait indefinitely.</param>
    /// <returns>
    /// The array index of the object that satisfied the wait.
    /// </returns>
    public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
    {
        // On .NET Framework we must take special care to NOT end up in a call to CoWait (which lets in RPC calls).
        // Off Windows, we can't p/invoke to kernel32, but it appears that .NET Core never calls CoWait, so we can rely on default behavior.
        // We're just going to use the OS as the switch instead of the framework so that (one day) if we drop our .NET Framework specific target,
        // and if .NET Core ever adds CoWait support on Windows, we'll still behave properly.
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            return (int)WaitForMultipleObjects((uint)waitHandles.Length, waitHandles, waitAll, (uint)millisecondsTimeout);
        }
        else
        {
            return WaitHelper(waitHandles, waitAll, millisecondsTimeout);
        }
    }

    /// <summary>
    /// Really truly non pumping wait.
    /// Raw IntPtrs have to be used, because the marshaller does not support arrays of SafeHandle, only
    /// single SafeHandles.
    /// </summary>
    /// <param name="handleCount">The number of handles in the <paramref name="waitHandles"/> array.</param>
    /// <param name="waitHandles">The handles to wait for.</param>
    /// <param name="waitAll">A flag indicating whether all handles must be signaled before returning.</param>
    /// <param name="millisecondsTimeout">A timeout that will cause this method to return.</param>
    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern int WaitForMultipleObjects(uint handleCount, IntPtr[] waitHandles, [MarshalAs(UnmanagedType.Bool)] bool waitAll, uint millisecondsTimeout);
}
