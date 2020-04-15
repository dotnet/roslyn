// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.v1.Interop
{
    internal sealed class SafeSqliteBlobHandle : SafeSqliteChildHandle<sqlite3_blob>
    {
        public SafeSqliteBlobHandle(SafeSqliteHandle sqliteHandle, sqlite3_blob wrapper)
            : base(sqliteHandle, wrapper.ptr, wrapper)
        {
        }

        protected override bool ReleaseChildHandle()
        {
            raw.sqlite3_blob_close(Wrapper);
            return true;
        }
    }
}
