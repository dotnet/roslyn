// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.SQLite.v2;

using static SQLitePersistentStorageConstants;

internal partial class SQLitePersistentStorage
{
    protected override Task<bool> ChecksumMatchesAsync(DocumentKey documentKey, Document? document, string name, Checksum checksum, CancellationToken cancellationToken)
        => _documentAccessor.ChecksumMatchesAsync(documentKey, name, checksum, cancellationToken);

    protected override Task<Stream?> ReadStreamAsync(DocumentKey documentKey, Document? document, string name, Checksum? checksum, CancellationToken cancellationToken)
        => _documentAccessor.ReadStreamAsync(documentKey, name, checksum, cancellationToken);

    protected override Task<bool> WriteStreamAsync(DocumentKey documentKey, Document? document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => _documentAccessor.WriteStreamAsync(documentKey, name, stream, checksum, cancellationToken);

    private readonly record struct DocumentPrimaryKey(ProjectPrimaryKey ProjectPrimaryKey, int DocumentFolderId, int DocumentNameId);

    /// <summary>
    /// <see cref="Accessor{TKey, TDatabaseId}"/> responsible for storing and 
    /// retrieving data from <see cref="DocumentDataTableName"/>.
    /// </summary>
    private sealed class DocumentAccessor(SQLitePersistentStorage storage) : Accessor<DocumentKey, DocumentPrimaryKey>(Table.Document,
              storage,
              (ProjectPathIdColumnName, SQLiteIntegerType),
              (ProjectNameIdColumnName, SQLiteIntegerType),
              (DocumentFolderIdColumnName, SQLiteIntegerType),
              (DocumentNameIdColumnName, SQLiteIntegerType))
    {
        protected override DocumentPrimaryKey? TryGetDatabaseKey(SqlConnection connection, DocumentKey key, bool allowWrite)
            => Storage.TryGetDocumentPrimaryKey(connection, key, allowWrite);

        protected override void BindAccessorSpecificPrimaryKeyParameters(SqlStatement statement, DocumentPrimaryKey primaryKey)
        {
            var ((projectPathId, projectNameId), documentFolderId, documentNameId) = primaryKey;

            statement.BindInt64Parameter(parameterIndex: 1, projectPathId);
            statement.BindInt64Parameter(parameterIndex: 2, projectNameId);
            statement.BindInt64Parameter(parameterIndex: 3, documentFolderId);
            statement.BindInt64Parameter(parameterIndex: 4, documentNameId);
        }
    }
}
