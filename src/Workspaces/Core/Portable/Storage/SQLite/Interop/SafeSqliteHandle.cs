// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop;

internal sealed class SafeSqliteHandle : SafeHandle
{
    private readonly sqlite3? _wrapper;
    private readonly SafeHandleLease _lease;

    public SafeSqliteHandle(sqlite3? wrapper)
        : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
    {
        _wrapper = wrapper;
        if (wrapper is not null)
        {
            _lease = wrapper.Lease();
            SetHandle(wrapper.DangerousGetHandle());
        }
        else
        {
            _lease = default;
            SetHandle(IntPtr.Zero);
        }
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public sqlite3 DangerousGetWrapper()
        => _wrapper!;

    protected override bool ReleaseHandle()
    {
        using var _ = _wrapper;

        _lease.Dispose();
        SetHandle(IntPtr.Zero);
        return true;
    }
}
