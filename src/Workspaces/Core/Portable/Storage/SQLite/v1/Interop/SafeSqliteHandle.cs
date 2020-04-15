// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.v1.Interop
{
    internal sealed class SafeSqliteHandle : SafeHandle
    {
        private readonly sqlite3 _wrapper;

        public SafeSqliteHandle(sqlite3 handle)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            _wrapper = handle;
            SetHandle(handle.ptr);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public new sqlite3 DangerousGetHandle()
            => _wrapper;

        protected override bool ReleaseHandle()
        {
            raw.sqlite3_close(_wrapper);
            return true;
        }
    }
}
