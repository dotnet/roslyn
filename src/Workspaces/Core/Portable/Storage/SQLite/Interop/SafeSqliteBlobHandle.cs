// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop;

internal sealed class SafeSqliteBlobHandle : SafeHandle
{
    private readonly sqlite3_blob? _wrapper;
    private readonly SafeHandleLease _lease;
    private readonly SafeHandleLease _sqliteLease;

    public SafeSqliteBlobHandle(SafeSqliteHandle sqliteHandle, sqlite3_blob? wrapper)
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

        _sqliteLease = sqliteHandle.Lease();
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    public sqlite3_blob DangerousGetWrapper()
        => _wrapper!;

    protected override bool ReleaseHandle()
    {
        try
        {
            using var _ = _wrapper;

            _lease.Dispose();
            SetHandle(IntPtr.Zero);
            return true;
        }
        finally
        {
            _sqliteLease.Dispose();
        }
    }
}
