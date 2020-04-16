// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using SQLitePCL;

namespace Microsoft.CodeAnalysis.SQLite.Interop
{
    internal sealed class SafeSqliteStatementHandle : SafeSqliteChildHandle<sqlite3_stmt>
    {
        public SafeSqliteStatementHandle(SafeSqliteHandle sqliteHandle, sqlite3_stmt? wrapper)
            : base(sqliteHandle, wrapper?.ptr ?? IntPtr.Zero, wrapper)
        {
        }

        protected override bool ReleaseChildHandle()
        {
            var result = (Result)raw.sqlite3_finalize(Wrapper);
            return result == Result.OK;
        }
    }
}
