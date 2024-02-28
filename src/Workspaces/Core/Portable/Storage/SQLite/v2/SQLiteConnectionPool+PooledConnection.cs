// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal partial class SQLiteConnectionPool
{
    internal readonly struct PooledConnection(SQLiteConnectionPool connectionPool, SqlConnection sqlConnection) : IDisposable
    {
        public readonly SqlConnection Connection = sqlConnection;

        public void Dispose()
            => connectionPool.ReleaseConnection(Connection);
    }
}
