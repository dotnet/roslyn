// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.SQLite.v2;

using static SQLitePersistentStorageConstants;

internal sealed partial class SQLitePersistentStorage
{
    protected override Task<bool> ChecksumMatchesAsync(ProjectKey projectKey, Project? project, string name, Checksum checksum, CancellationToken cancellationToken)
        => _projectAccessor.ChecksumMatchesAsync(projectKey, name, checksum, cancellationToken);

    protected override Task<Stream?> ReadStreamAsync(ProjectKey projectKey, Project? project, string name, Checksum? checksum, CancellationToken cancellationToken)
        => _projectAccessor.ReadStreamAsync(projectKey, name, checksum, cancellationToken);

    protected override Task<bool> WriteStreamAsync(ProjectKey projectKey, Project? project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => _projectAccessor.WriteStreamAsync(projectKey, name, stream, checksum, cancellationToken);

    private readonly record struct ProjectPrimaryKey(int ProjectPathId, int ProjectNameId);

    /// <summary>
    /// <see cref="Accessor{TKey, TDatabaseId}"/> responsible for storing and
    /// retrieving data from <see cref="ProjectDataTableName"/>.
    /// </summary>
    private sealed class ProjectAccessor(SQLitePersistentStorage storage) : Accessor<ProjectKey, ProjectPrimaryKey>(Table.Project,
              storage,
              (ProjectPathIdColumnName, SQLiteIntegerType),
              (ProjectNameIdColumnName, SQLiteIntegerType))
    {
        protected override ProjectPrimaryKey? TryGetDatabaseKey(SqlConnection connection, ProjectKey projectKey, bool allowWrite)
            => Storage.TryGetProjectPrimaryKey(connection, projectKey, allowWrite);

        protected override void BindAccessorSpecificPrimaryKeyParameters(SqlStatement statement, ProjectPrimaryKey primaryKey)
        {
            statement.BindInt64Parameter(parameterIndex: 1, primaryKey.ProjectPathId);
            statement.BindInt64Parameter(parameterIndex: 2, primaryKey.ProjectNameId);
        }
    }
}
