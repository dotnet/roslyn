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
    internal sealed class SafeSqliteBlobHandle : SafeHandle
    {
        private readonly sqlite3_blob? _wrapper;
        private readonly SafeHandleLease _lease;

        public SafeSqliteBlobHandle(SafeSqliteHandle sqliteHandle, sqlite3_blob? wrapper)
            : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
        {
            _wrapper = wrapper;
            SetHandle(wrapper?.ptr ?? IntPtr.Zero);
            _lease = sqliteHandle.Lease();
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public sqlite3_blob DangerousGetWrapper()
            => _wrapper!;

        protected override bool ReleaseHandle()
        {
            try
            {
                var result = (Result)raw.sqlite3_blob_close(_wrapper);
                SetHandle(IntPtr.Zero);
                return result == Result.OK;
            }
            finally
            {
                _lease.Dispose();
            }
        }
    }
}
