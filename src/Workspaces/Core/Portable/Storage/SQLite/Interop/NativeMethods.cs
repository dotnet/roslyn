// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Name chosen to match SQLitePCL.raw")]
    internal static class NativeMethods
    {
        public static SafeSqliteHandle sqlite3_open_v2(string filename, int flags, string vfs, out Result result)
        {
            result = (Result)raw.sqlite3_open_v2(filename, out var wrapper, flags, vfs);
            if (result != Result.OK)
            {
                wrapper = null;
            }

            try
            {
                // Always return a non-null handle to match default P/Invoke marshaling behavior. SafeHandle.IsInvalid
                // will be true when the handle is not usable, but the handle instance can be disposed either way.
                return new SafeSqliteHandle(wrapper);
            }
            catch
            {
                raw.sqlite3_close(wrapper);
                throw;
            }
        }

        public static SafeSqliteStatementHandle sqlite3_prepare_v2(SafeSqliteHandle db, string sql, out Result result)
        {
            using var _ = db.Lease();

            result = (Result)raw.sqlite3_prepare_v2(db.DangerousGetWrapper(), sql, out var wrapper);
            if (result != (int)Result.OK)
            {
                wrapper = null;
            }

            try
            {
                // Always return a non-null handle to match default P/Invoke marshaling behavior. SafeHandle.IsInvalid
                // will be true when the handle is not usable, but the handle instance can be disposed either way.
                return new SafeSqliteStatementHandle(db, wrapper);
            }
            catch
            {
                raw.sqlite3_finalize(wrapper);
                throw;
            }
        }

        public static SafeSqliteBlobHandle sqlite3_blob_open(SafeSqliteHandle db, string sdb, string table, string col, long rowid, int flags, out Result result)
        {
            using var _ = db.Lease();

            result = (Result)raw.sqlite3_blob_open(db.DangerousGetWrapper(), sdb, table, col, rowid, flags, out var wrapper);
            if (result != (int)Result.OK)
            {
                wrapper = null;
            }

            try
            {
                // Always return a non-null handle to match default P/Invoke marshaling behavior. SafeHandle.IsInvalid
                // will be true when the handle is not usable, but the handle instance can be disposed either way.
                return new SafeSqliteBlobHandle(db, wrapper);
            }
            catch
            {
                raw.sqlite3_blob_close(wrapper);
                throw;
            }
        }

        public static string sqlite3_errmsg(SafeSqliteHandle db)
        {
            using var _ = db.Lease();
            return raw.sqlite3_errmsg(db.DangerousGetWrapper());
        }

        public static string sqlite3_errstr(int rc)
        {
            return raw.sqlite3_errstr(rc);
        }

        public static int sqlite3_extended_errcode(SafeSqliteHandle db)
        {
            using var _ = db.Lease();
            return raw.sqlite3_extended_errcode(db.DangerousGetWrapper());
        }

        public static Result sqlite3_busy_timeout(SafeSqliteHandle db, int ms)
        {
            using var _ = db.Lease();
            return (Result)raw.sqlite3_busy_timeout(db.DangerousGetWrapper(), ms);
        }

        public static long sqlite3_last_insert_rowid(SafeSqliteHandle db)
        {
            using var _ = db.Lease();
            return raw.sqlite3_last_insert_rowid(db.DangerousGetWrapper());
        }

        public static int sqlite3_blob_bytes(SafeSqliteBlobHandle blob)
        {
            using var _ = blob.Lease();
            return raw.sqlite3_blob_bytes(blob.DangerousGetWrapper());
        }

        public static Result sqlite3_blob_read(SafeSqliteBlobHandle blob, byte[] b, int n, int offset)
        {
            using var _ = blob.Lease();
            return (Result)raw.sqlite3_blob_read(blob.DangerousGetWrapper(), b, n, offset);
        }

        public static Result sqlite3_reset(SafeSqliteStatementHandle stmt)
        {
            using var _ = stmt.Lease();
            return (Result)raw.sqlite3_reset(stmt.DangerousGetWrapper());
        }

        public static Result sqlite3_step(SafeSqliteStatementHandle stmt)
        {
            using var _ = stmt.Lease();
            return (Result)raw.sqlite3_step(stmt.DangerousGetWrapper());
        }

        public static Result sqlite3_bind_text(SafeSqliteStatementHandle stmt, int index, string val)
        {
            using var _ = stmt.Lease();
            return (Result)raw.sqlite3_bind_text(stmt.DangerousGetWrapper(), index, val);
        }

        public static Result sqlite3_bind_int64(SafeSqliteStatementHandle stmt, int index, long val)
        {
            using var _ = stmt.Lease();
            return (Result)raw.sqlite3_bind_int64(stmt.DangerousGetWrapper(), index, val);
        }

        public static byte[] sqlite3_column_blob(SafeSqliteStatementHandle stmt, int index)
        {
            using var _ = stmt.Lease();
            return raw.sqlite3_column_blob(stmt.DangerousGetWrapper(), index);
        }

        public static int sqlite3_column_int(SafeSqliteStatementHandle stmt, int index)
        {
            using var _ = stmt.Lease();
            return raw.sqlite3_column_int(stmt.DangerousGetWrapper(), index);
        }

        public static long sqlite3_column_int64(SafeSqliteStatementHandle stmt, int index)
        {
            using var _ = stmt.Lease();
            return raw.sqlite3_column_int64(stmt.DangerousGetWrapper(), index);
        }

        public static string sqlite3_column_text(SafeSqliteStatementHandle stmt, int index)
        {
            using var _ = stmt.Lease();
            return raw.sqlite3_column_text(stmt.DangerousGetWrapper(), index);
        }
    }
}
