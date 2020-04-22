// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal sealed class SafeSqliteStatementHandle : SafeHandle
    {
        private readonly sqlite3_stmt? _wrapper;
        private readonly SafeHandleLease _lease;

        public SafeSqliteStatementHandle(SafeSqliteHandle sqliteHandle, sqlite3_stmt? wrapper)
            : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
        {
            _wrapper = wrapper;
            SetHandle(wrapper?.ptr ?? IntPtr.Zero);
            _lease = sqliteHandle.Lease();
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public new sqlite3_stmt DangerousGetHandle()
            => _wrapper!;

        protected override bool ReleaseHandle()
        {
            using var _ = _lease;
            var result = (Result)raw.sqlite3_finalize(_wrapper);
            return result == Result.OK;
        }
    }
}
