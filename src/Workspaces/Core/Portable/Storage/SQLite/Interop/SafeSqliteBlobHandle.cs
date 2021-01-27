// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal sealed class SafeSqliteBlobHandle : IDisposable
    {
        private readonly sqlite3_blob? _wrapper;
        private readonly SafeHandleLease _lease;

        public SafeSqliteBlobHandle(SafeSqliteHandle sqliteHandle, sqlite3_blob? wrapper)
        {
            _wrapper = wrapper;
            _lease = sqliteHandle.Lease();
        }

        public sqlite3_blob DangerousGetWrapper()
            => _wrapper!;

        public SafeHandleLease Lease()
            => _wrapper == null ? default : _wrapper.Lease();

        public void Dispose()
        {
            try
            {
                if (_wrapper != null)
                    raw.sqlite3_blob_close(_wrapper);
            }
            finally
            {
                _lease.Dispose();
            }
        }
    }
}
