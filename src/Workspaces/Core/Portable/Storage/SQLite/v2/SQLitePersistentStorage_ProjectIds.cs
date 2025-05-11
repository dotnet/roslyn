// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.SQLite.v2.Interop;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal sealed partial class SQLitePersistentStorage
{
    /// <summary>
    /// Mapping from the workspace's ID for a project, to the ID we use in the DB for the project.
    /// Kept locally so we don't have to hit the DB for the common case of trying to determine the 
    /// DB id for a project.
    /// </summary>
    private readonly ConcurrentDictionary<ProjectId, ProjectPrimaryKey> _projectIdToPrimaryKeyMap = [];

    /// <summary>
    /// Given a project, and the name of a stream to read/write, gets the integral DB ID to 
    /// use to find the data inside the ProjectData table.
    /// </summary>
    private ProjectPrimaryKey? TryGetProjectPrimaryKey(SqlConnection connection, ProjectKey projectKey, bool allowWrite)
    {
        // First see if we've cached the ID for this value locally.  If so, just return
        // what we already have.
        if (!_projectIdToPrimaryKeyMap.TryGetValue(projectKey.Id, out var existingId))
        {
            // Store the project as its folder and project name.  The folder is relative to the solution path so
            // that we're not dependent on file-system location.
            var projectPath =
                projectKey.FilePath != null && PathUtilities.GetRelativePath(_solutionDirectory, projectKey.FilePath) is { Length: > 0 } relativePath
                    ? relativePath
                    : projectKey.FilePath;

            // Key the project off both its path and name.  That way we work properly
            // in host and test scenarios.
            if (TryGetStringId(connection, projectPath, allowWrite) is not int projectPathId ||
                TryGetStringId(connection, projectKey.Name, allowWrite) is not int projectNameId)
            {
                return null;
            }

            // Cache the value locally so we don't need to go back to the DB in the future.
            existingId = new ProjectPrimaryKey(projectPathId, projectNameId);
            _projectIdToPrimaryKeyMap.TryAdd(projectKey.Id, existingId);
        }

        return existingId;
    }
}
