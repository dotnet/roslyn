// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    /// <summary>
    /// The base handle type for an SQLite resource that exists within the context of a parent handle, and should always
    /// be released prior to the parent handle.
    /// </summary>
    /// <typeparam name="T">The SQLite resource wrapper type.</typeparam>
    internal abstract class SafeSqliteChildHandle<T> : SafeSqliteHandle<T>
        where T : class
    {
        private readonly SafeHandleLease _lease;

        protected SafeSqliteChildHandle(SafeHandle parentHandle, IntPtr handle, T? wrapper)
            : base(handle, wrapper)
        {
            _lease = parentHandle.Lease();
        }

        protected abstract bool ReleaseChildHandle();

        protected sealed override bool ReleaseHandle()
        {
            using var _ = _lease;
            return ReleaseChildHandle();
        }
    }
}
