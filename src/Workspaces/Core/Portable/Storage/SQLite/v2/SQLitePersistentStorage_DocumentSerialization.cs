// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;

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

        private readonly record struct DocumentPrimaryKey(ProjectPrimaryKey ProjectPrimaryKey, int DocumentPathId, int DocumentNameId);

        /// <summary>
        /// <see cref="Accessor{TKey, TDatabaseId}"/> responsible for storing and 
        /// retrieving data from <see cref="DocumentDataTableName"/>.
        /// </summary>
        private sealed class DocumentAccessor : Accessor<
            (DocumentKey documentKey, string name),
            (DocumentPrimaryKey documentkeyId, int dataNameId)>
        {
            public DocumentAccessor(SQLitePersistentStorage storage)
                : base(storage, ImmutableArray.Create(
                    (ProjectPathIdColumnName, SQLiteIntegerType),
                    (ProjectNameIdColumnName, SQLiteIntegerType),
                    (DocumentPathIdColumnName, SQLiteIntegerType),
                    (DocumentNameIdColumnName, SQLiteIntegerType),
                    (DataNameIdColumnName, SQLiteIntegerType)))
            {
            }

            protected override Table Table => Table.Document;

            protected override (DocumentPrimaryKey documentkeyId, int dataNameId)? TryGetDatabaseId(SqlConnection connection, (DocumentKey documentKey, string name) key, bool allowWrite)
                => Storage.TryGetDocumentDataId(connection, key.documentKey, key.name, allowWrite);

            protected override void BindPrimaryKeyParameters(SqlStatement statement, (DocumentPrimaryKey documentkeyId, int dataNameId) dataId)
            {
                var (((projectPathId, projectNameId), documentPathId, documentNameId), dataNameId) = dataId;

                statement.BindInt64Parameter(parameterIndex: 1, projectPathId);
                statement.BindInt64Parameter(parameterIndex: 2, projectNameId);
                statement.BindInt64Parameter(parameterIndex: 3, documentPathId);
                statement.BindInt64Parameter(parameterIndex: 4, documentNameId);
                statement.BindInt64Parameter(parameterIndex: 5, dataNameId);
            }
        }
    }
}
