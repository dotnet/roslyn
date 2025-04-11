// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

/// <summary>
/// Represents a lease of a <see cref="SafeHandle"/>.
/// </summary>
/// <seealso cref="SafeHandleExtensions.Lease"/>
[NonCopyable]
internal readonly struct SafeHandleLease : IDisposable
{
    private readonly SafeHandle? _handle;

    internal SafeHandleLease(SafeHandle handle)
        => _handle = handle;

    /// <summary>
    /// Releases the <see cref="SafeHandle"/> lease. The behavior of this method is unspecified if called more than
    /// once.
    /// </summary>
    public void Dispose()
        => _handle?.DangerousRelease();
}
