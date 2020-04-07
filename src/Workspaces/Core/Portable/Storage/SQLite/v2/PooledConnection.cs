// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage : AbstractPersistentStorage
    {
        private struct PooledConnection : IDisposable
        {
            private readonly SQLitePersistentStorage _storage;
            public readonly SqlConnection Connection;

            public PooledConnection(SQLitePersistentStorage storage, SqlConnection sqlConnection)
            {
                _storage = storage;
                Connection = sqlConnection;
            }

            public void Dispose()
                => _storage.ReleaseConnection(Connection);
        }
    }
}
