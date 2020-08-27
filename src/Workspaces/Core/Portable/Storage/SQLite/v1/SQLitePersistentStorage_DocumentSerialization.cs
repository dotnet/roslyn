// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.SQLite.v1.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v1
{
    internal partial class SQLitePersistentStorage
    {
        protected override Task<Checksum> ReadChecksumAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, CancellationToken cancellationToken)
            => _documentAccessor.ReadChecksumAsync((documentKey, bulkLoadSnapshot, name), cancellationToken);

        protected override Task<Stream> ReadStreamAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken)
            => _documentAccessor.ReadStreamAsync((documentKey, bulkLoadSnapshot, name), checksum, cancellationToken);

        public override Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => _documentAccessor.WriteStreamAsync(((DocumentKey)document, document, name), stream, checksum, cancellationToken);

        /// <summary>
        /// <see cref="Accessor{TKey, TWriteQueueKey, TDatabaseId}"/> responsible for storing and 
        /// retrieving data from <see cref="DocumentDataTableName"/>.
        /// </summary>
        private class DocumentAccessor : Accessor<
            (DocumentKey documentKey, Document? bulkLoadSnapshot, string name),
            (DocumentId, string),
            long>
        {
            public DocumentAccessor(SQLitePersistentStorage storage) : base(storage)
            {
            }

            protected override string DataTableName => DocumentDataTableName;

            protected override (DocumentId, string) GetWriteQueueKey((DocumentKey documentKey, Document? bulkLoadSnapshot, string name) key)
                => (key.documentKey.Id, key.name);

            protected override bool TryGetDatabaseId(SqlConnection connection, (DocumentKey documentKey, Document? bulkLoadSnapshot, string name) key, out long dataId)
                => Storage.TryGetDocumentDataId(connection, key.documentKey, key.bulkLoadSnapshot, key.name, out dataId);

            protected override void BindFirstParameter(SqlStatement statement, long dataId)
                => statement.BindInt64Parameter(parameterIndex: 1, value: dataId);

            protected override bool TryGetRowId(SqlConnection connection, long dataId, out long rowId)
                => GetAndVerifyRowId(connection, dataId, out rowId);
        }
    }
}
