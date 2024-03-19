// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class SafeHandleExtensions
{
    /// <summary>
    /// Acquires a lease on a safe handle. The lease increments the reference count of the <see cref="SafeHandle"/>
    /// to ensure the handle is not released prior to the lease being released.
    /// </summary>
    /// <remarks>
    /// This method is intended to be used in the initializer of a <c>using</c> statement. Failing to release the
    /// lease will permanently prevent the underlying <see cref="SafeHandle"/> from being released by the garbage
    /// collector.
    /// </remarks>
    /// <param name="handle">The <see cref="SafeHandle"/> to lease.</param>
    /// <returns>A <see cref="SafeHandleLease"/>, which must be disposed to release the resource.</returns>
    /// <exception cref="ObjectDisposedException">If the lease could not be acquired.</exception>
    public static SafeHandleLease Lease(this SafeHandle handle)
    {
        RoslynDebug.AssertNotNull(handle);

        var success = false;
        try
        {
            handle.DangerousAddRef(ref success);
            Debug.Assert(success, $"{nameof(SafeHandle.DangerousAddRef)} does not return when {nameof(success)} is false.");

            return new SafeHandleLease(handle);
        }
        catch
        {
            if (success)
                handle.DangerousRelease();

            throw;
        }
    }
}
