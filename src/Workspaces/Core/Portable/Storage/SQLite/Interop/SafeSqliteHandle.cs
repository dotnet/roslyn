// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal sealed class SafeSqliteHandle : IDisposable
    {
        private readonly sqlite3? _wrapper;

        public SafeSqliteHandle(sqlite3? wrapper)
        {
            _wrapper = wrapper;
        }

        public SafeHandleLease Lease()
            => _wrapper == null ? default : _wrapper.Lease();

        public sqlite3 DangerousGetWrapper()
            => _wrapper!;

        public void Dispose()
        {
            raw.sqlite3_close(_wrapper);
        }
    }
}
