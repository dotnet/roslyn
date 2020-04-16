// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SQLite.v1.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v1
{
    internal partial class SQLitePersistentStorage : AbstractPersistentStorage
    {
        private struct PooledConnection : IDisposable
        {
            private readonly SQLitePersistentStorage sqlitePersistentStorage;
            public readonly SqlConnection Connection;

            public PooledConnection(SQLitePersistentStorage sqlitePersistentStorage, SqlConnection sqlConnection)
            {
                this.sqlitePersistentStorage = sqlitePersistentStorage;
                Connection = sqlConnection;
            }

            public void Dispose()
                => sqlitePersistentStorage.ReleaseConnection(Connection);
        }
    }
}
