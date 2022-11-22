// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    using static SQLitePersistentStorageConstants;

    internal partial class SQLitePersistentStorage
    {
        public override Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken)
            => _solutionAccessor.ChecksumMatchesAsync(name, checksum, cancellationToken);

        public override Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken)
            => _solutionAccessor.ReadStreamAsync(name, checksum, cancellationToken);

        public override Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => _solutionAccessor.WriteStreamAsync(name, stream, checksum, cancellationToken);

        /// <summary>
        /// <see cref="Accessor{TKey, TWriteQueueKey, TDatabaseId}"/> responsible for storing and 
        /// retrieving data from <see cref="SolutionDataTableName"/>.  Note that with the Solution 
        /// table there is no need for key->id translation.  i.e. the key acts as the ID itself.
        /// </summary>
        private class SolutionAccessor : Accessor<string, string, (string key, bool unused)>
        {
            public SolutionAccessor(SQLitePersistentStorage storage)
                : base(storage, ImmutableArray.Create((SolutionDataIdColumnName, "varchar")))
            {
            }

            protected override Table Table => Table.Solution;

            protected override string GetWriteQueueKey(string key)
                => key;

            // For the SolutionDataTable the key itself acts as the data-id.
            protected override (string key, bool unused)? TryGetDatabaseId(SqlConnection connection, string key, bool allowWrite)
                => (key, unused: false);

            protected override void BindPrimaryKeyParameters(SqlStatement statement, (string key, bool unused) dataId)
                => statement.BindStringParameter(parameterIndex: 1, value: dataId.key);
        }
    }
}
