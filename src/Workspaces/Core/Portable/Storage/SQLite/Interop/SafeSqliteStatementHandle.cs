// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal sealed class SafeSqliteStatementHandle : IDisposable
    {
        private readonly sqlite3_stmt? _wrapper;
        private readonly SafeHandleLease _lease;

        public SafeSqliteStatementHandle(SafeSqliteHandle sqliteHandle, sqlite3_stmt? wrapper)
        {
            _wrapper = wrapper;
            _lease = sqliteHandle.Lease();
        }

        public SafeHandleLease Lease()
            => _wrapper == null ? default : _wrapper.Lease();

        public sqlite3_stmt DangerousGetWrapper()
            => _wrapper!;

        public void Dispose()
        {
            try
            {
                raw.sqlite3_finalize(_wrapper);
            }
            finally
            {
                _lease.Dispose();
            }
        }
    }
}
