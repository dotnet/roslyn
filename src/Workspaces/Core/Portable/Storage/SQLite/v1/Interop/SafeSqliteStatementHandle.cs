// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.v1.Interop
{
    internal sealed class SafeSqliteStatementHandle : SafeHandle
    {
        private readonly SafeHandleLease _lease;
        private readonly sqlite3_stmt _wrapper;

        public SafeSqliteStatementHandle(SafeSqliteHandle handle, sqlite3_stmt rawStatement)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            _lease = handle.Lease();
            _wrapper = rawStatement;
            SetHandle(rawStatement.ptr);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public new sqlite3_stmt DangerousGetHandle()
            => _wrapper;

        protected override bool ReleaseHandle()
        {
            using var _ = _lease;

            raw.sqlite3_finalize(_wrapper);
            return true;
        }
    }
}
