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
    internal sealed class SafeSqliteBlobHandle : SafeHandle
    {
        private readonly SafeHandleLease _lease;
        private readonly sqlite3_blob _wrapper;

        public SafeSqliteBlobHandle(SafeSqliteHandle handle, sqlite3_blob blob)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            _lease = handle.Lease();
            _wrapper = blob;
            SetHandle(blob.ptr);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public new sqlite3_blob DangerousGetHandle()
            => _wrapper;

        protected override bool ReleaseHandle()
        {
            using var _ = _lease;

            raw.sqlite3_blob_close(_wrapper);
            return true;
        }
    }
}
