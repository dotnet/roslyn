// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    using static SQLitePersistentStorageConstants;

    internal partial class SQLitePersistentStorage
    {
        protected override Task<bool> ChecksumMatchesAsync(ProjectKey projectKey, Project? project, string name, Checksum checksum, CancellationToken cancellationToken)
            => _projectAccessor.ChecksumMatchesAsync((projectKey, name), checksum, cancellationToken);

        protected override Task<Stream?> ReadStreamAsync(ProjectKey projectKey, Project? project, string name, Checksum? checksum, CancellationToken cancellationToken)
            => _projectAccessor.ReadStreamAsync((projectKey, name), checksum, cancellationToken);

        protected override Task<bool> WriteStreamAsync(ProjectKey projectKey, Project? project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => _projectAccessor.WriteStreamAsync((projectKey, name), stream, checksum, cancellationToken);

        /// <summary>
        /// <see cref="Accessor{TKey, TWriteQueueKey, TDatabaseId}"/> responsible for storing and
        /// retrieving data from <see cref="ProjectDataTableName"/>.
        /// </summary>
        private class ProjectAccessor : Accessor<
            (ProjectKey projectKey, string name),
            (ProjectId projectId, string name),
            long>
        {
            public ProjectAccessor(SQLitePersistentStorage storage) : base(storage)
            {
            }

            protected override Table Table => Table.Project;

            protected override (ProjectId projectId, string name) GetWriteQueueKey((ProjectKey projectKey, string name) key)
                => (key.projectKey.Id, key.name);

            protected override bool TryGetDatabaseId(SqlConnection connection, (ProjectKey projectKey, string name) key, bool allowWrite, out long dataId)
                => Storage.TryGetProjectDataId(connection, key.projectKey, key.name, allowWrite, out dataId);

            protected override void BindFirstParameter(SqlStatement statement, long dataId)
                => statement.BindInt64Parameter(parameterIndex: 1, value: dataId);

            protected override bool TryGetRowId(SqlConnection connection, Database database, long dataId, out long rowId)
                => GetAndVerifyRowId(connection, database, dataId, out rowId);
        }
    }
}
