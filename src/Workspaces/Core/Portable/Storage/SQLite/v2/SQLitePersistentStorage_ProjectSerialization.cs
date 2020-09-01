// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal partial class SQLitePersistentStorage
    {
        protected override Task<Checksum> ReadChecksumAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, CancellationToken cancellationToken)
            => _projectAccessor.ReadChecksumAsync((projectKey, bulkLoadSnapshot, name), cancellationToken);

        protected override Task<Stream> ReadStreamAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken)
            => _projectAccessor.ReadStreamAsync((projectKey, bulkLoadSnapshot, name), checksum, cancellationToken);

        public override Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => _projectAccessor.WriteStreamAsync(((ProjectKey)project, project, name), stream, checksum, cancellationToken);

        /// <summary>
        /// <see cref="Accessor{TKey, TWriteQueueKey, TDatabaseId}"/> responsible for storing and
        /// retrieving data from <see cref="ProjectDataTableName"/>.
        /// </summary>
        private class ProjectAccessor : Accessor<
            (ProjectKey projectKey, Project? bulkLoadSnapshot, string name),
            (ProjectId projectId, string name),
            long>
        {
            public ProjectAccessor(SQLitePersistentStorage storage) : base(storage)
            {
            }

            protected override string DataTableName => ProjectDataTableName;

            protected override (ProjectId projectId, string name) GetWriteQueueKey((ProjectKey projectKey, Project? bulkLoadSnapshot, string name) key)
                => (key.projectKey.Id, key.name);

            protected override bool TryGetDatabaseId(SqlConnection connection, (ProjectKey projectKey, Project? bulkLoadSnapshot, string name) key, out long dataId)
                => Storage.TryGetProjectDataId(connection, key.projectKey, key.bulkLoadSnapshot, key.name, out dataId);

            protected override void BindFirstParameter(SqlStatement statement, long dataId)
                => statement.BindInt64Parameter(parameterIndex: 1, value: dataId);

            protected override bool TryGetRowId(SqlConnection connection, Database database, long dataId, out long rowId)
                => GetAndVerifyRowId(connection, database, dataId, out rowId);
        }
    }
}
