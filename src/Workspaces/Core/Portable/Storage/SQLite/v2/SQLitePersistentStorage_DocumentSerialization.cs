// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    using static SQLitePersistentStorageConstants;

    internal partial class SQLitePersistentStorage
    {
        protected override Task<bool> ChecksumMatchesAsync(DocumentKey documentKey, Document? document, string name, Checksum checksum, CancellationToken cancellationToken)
            => _documentAccessor.ChecksumMatchesAsync((documentKey, name), checksum, cancellationToken);

        protected override Task<Stream?> ReadStreamAsync(DocumentKey documentKey, Document? document, string name, Checksum? checksum, CancellationToken cancellationToken)
            => _documentAccessor.ReadStreamAsync((documentKey, name), checksum, cancellationToken);

        protected override Task<bool> WriteStreamAsync(DocumentKey documentKey, Document? document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => _documentAccessor.WriteStreamAsync((documentKey, name), stream, checksum, cancellationToken);

        /// <summary>
        /// <see cref="Accessor{TKey, TWriteQueueKey, TDatabaseId}"/> responsible for storing and 
        /// retrieving data from <see cref="DocumentDataTableName"/>.
        /// </summary>
        private class DocumentAccessor : Accessor<
            (DocumentKey documentKey, string name),
            (DocumentId, string),
            long>
        {
            public DocumentAccessor(SQLitePersistentStorage storage) : base(storage)
            {
            }

            protected override Table Table => Table.Document;

            protected override (DocumentId, string) GetWriteQueueKey((DocumentKey documentKey, string name) key)
                => (key.documentKey.Id, key.name);

            protected override bool TryGetDatabaseId(SqlConnection connection, (DocumentKey documentKey, string name) key, out long dataId)
                => Storage.TryGetDocumentDataId(connection, key.documentKey, key.name, out dataId);

            protected override void BindFirstParameter(SqlStatement statement, long dataId)
                => statement.BindInt64Parameter(parameterIndex: 1, value: dataId);

            protected override bool TryGetRowId(SqlConnection connection, Database database, long dataId, out long rowId)
                => GetAndVerifyRowId(connection, database, dataId, out rowId);
        }
    }
}
