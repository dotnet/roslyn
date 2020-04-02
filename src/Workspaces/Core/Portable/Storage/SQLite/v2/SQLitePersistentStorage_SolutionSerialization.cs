// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage
    {
        public override Task<Checksum> ReadChecksumAsync(string name, CancellationToken cancellationToken)
            => _solutionAccessor.ReadChecksumAsync(name, cancellationToken);

        public override Task<Stream> ReadStreamAsync(string name, Checksum checksum, CancellationToken cancellationToken)
            => _solutionAccessor.ReadStreamAsync(name, checksum, cancellationToken);

        public override Task<bool> WriteStreamAsync(string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
            => _solutionAccessor.WriteStreamAsync(name, stream, checksum, cancellationToken);

        /// <summary>
        /// <see cref="Accessor{TKey, TWriteQueueKey, TDatabaseId}"/> responsible for storing and 
        /// retrieving data from <see cref="SolutionDataTableName"/>.  Note that with the Solution 
        /// table there is no need for key->id translation.  i.e. the key acts as the ID itself.
        /// </summary>
        private class SolutionAccessor : Accessor<string, string, string>
        {
            public SolutionAccessor(SQLitePersistentStorage storage) : base(storage)
            {
            }

            protected override string DataTableName => SolutionDataTableName;

            protected override string GetWriteQueueKey(string key)
                => key;

            protected override bool TryGetDatabaseId(SqlConnection connection, string key, out string dataId)
            {
                // For the SolutionDataTable the key itself acts as the data-id.
                dataId = key;
                return true;
            }

            protected override bool TryGetRowId(SqlConnection connection, Database database, string dataId, out long rowId)
            {
                // For the solution table, we have whatever user string that was passed in as our 'key'.  So we actually
                // have to  go to the DB to find the row for this.
                return GetActualRowIdFromDatabase(connection, database, dataId, out rowId);
            }

            protected override void BindFirstParameter(SqlStatement statement, string dataId)
                => statement.BindStringParameter(parameterIndex: 1, value: dataId);
        }
    }
}
