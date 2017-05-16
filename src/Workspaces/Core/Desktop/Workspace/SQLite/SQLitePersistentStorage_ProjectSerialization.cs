﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.Interop;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        public override Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
            => _projectAccessor.ReadStreamAsync((project, name), cancellationToken);

        public override Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
            => _projectAccessor.WriteStreamAsync((project, name), stream, cancellationToken);

        /// <summary>
        /// <see cref="Accessor{TKey, TWriteQueueKey, TDatabaseId}"/> responsible for storing and
        /// retrieving data from <see cref="ProjectDataTableName"/>.
        /// </summary>
        private class ProjectAccessor : Accessor<
            (Project project, string name),
            (ProjectId projectId, string name),
            long>
        {
            public ProjectAccessor(SQLitePersistentStorage storage) : base(storage)
            {
            }

            protected override string DataTableName => ProjectDataTableName;

            protected override (ProjectId projectId, string name) GetWriteQueueKey((Project project, string name) key)
                => (key.project.Id, key.name);

            protected override bool TryGetDatabaseId(SqlConnection connection, (Project project, string name) key, out long dataId)
                => Storage.TryGetProjectDataId(connection, key.project, key.name, out dataId);

            protected override void BindFirstParameter(SqlStatement statement, long dataId)
                => statement.BindInt64Parameter(parameterIndex: 1, value: dataId);
        }
    }
}