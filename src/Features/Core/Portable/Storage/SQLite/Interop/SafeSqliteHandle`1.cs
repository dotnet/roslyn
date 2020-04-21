// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal abstract class SafeSqliteHandle<T> : SafeHandle
        where T : class
    {
        protected readonly T? Wrapper;

        public SafeSqliteHandle(IntPtr handle, T? wrapper)
            : base(invalidHandleValue: IntPtr.Zero, ownsHandle: true)
        {
            Wrapper = wrapper;
            SetHandle(handle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        public new T DangerousGetHandle()
            => Wrapper!;
    }
}
