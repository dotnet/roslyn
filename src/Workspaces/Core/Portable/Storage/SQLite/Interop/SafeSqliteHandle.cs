// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal sealed class SafeSqliteHandle : SafeHandle
    {
        private readonly sqlite3? _wrapper;

        public SafeSqliteHandle(sqlite3? wrapper)
            : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
        {
            _wrapper = wrapper;
            SetHandle(wrapper?.ptr ?? IntPtr.Zero);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public sqlite3 DangerousGetWrapper()
            => _wrapper!;

        protected override bool ReleaseHandle()
        {
            var result = (Result)raw.sqlite3_close(_wrapper);
            SetHandle(IntPtr.Zero);
            return result == Result.OK;
        }
    }
}
