// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SQLite.Interop;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage : AbstractPersistentStorage
    {
        private struct PooledConnection : IDisposable
        {
            private readonly SQLitePersistentStorage _storage;
            public readonly SqlConnection Connection;

            public PooledConnection(SQLitePersistentStorage sqlitePersistentStorage, SqlConnection sqlConnection)
            {
                this._storage = sqlitePersistentStorage;
                Connection = sqlConnection;
            }

            public void Dispose()
            {
                _storage.ReleaseConnection(Connection);
            }
        }
    }
}
