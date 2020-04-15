// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SQLite.v1.Interop;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal static class NativeMethods
    {
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Name chosen to match SQLitePCL.raw")]
        public static SafeSqliteHandle sqlite3_open_v2(string filename, int flags, string vfs, out Result result)
        {
            result = (Result)raw.sqlite3_open_v2(filename, out var wrapper, flags, vfs);
            if (result != Result.OK)
            {
                wrapper = null;
            }

            try
            {
                return new SafeSqliteHandle(wrapper);
            }
            catch
            {
                raw.sqlite3_close(wrapper);
                throw;
            }
        }

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Name chosen to match SQLitePCL.raw")]
        public static SafeSqliteStatementHandle sqlite3_prepare_v2(SafeSqliteHandle db, string sql, out Result result)
        {
            using var _ = db.Lease();

            result = (Result)raw.sqlite3_prepare_v2(db.DangerousGetHandle(), sql, out var wrapper);
            if (result != (int)Result.OK)
            {
                wrapper = null;
            }

            try
            {
                return new SafeSqliteStatementHandle(db, wrapper);
            }
            catch
            {
                raw.sqlite3_finalize(wrapper);
                throw;
            }
        }

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Name chosen to match SQLitePCL.raw")]
        public static SafeSqliteBlobHandle sqlite3_blob_open(SafeSqliteHandle db, string sdb, string table, string col, long rowid, int flags, out Result result)
        {
            using var _ = db.Lease();

            result = (Result)raw.sqlite3_blob_open(db.DangerousGetHandle(), sdb, table, col, rowid, flags, out var wrapper);
            if (result != (int)Result.OK)
            {
                wrapper = null;
            }

            try
            {
                return new SafeSqliteBlobHandle(db, wrapper);
            }
            catch
            {
                raw.sqlite3_blob_close(wrapper);
                throw;
            }
        }
    }
}
