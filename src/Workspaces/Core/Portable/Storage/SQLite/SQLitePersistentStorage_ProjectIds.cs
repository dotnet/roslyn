// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.SQLite.Interop;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        /// <summary>
        /// Mapping from the workspace's ID for a project, to the ID we use in the DB for the project.
        /// Kept locally so we don't have to hit the DB for the common case of trying to determine the 
        /// DB id for a project.
        /// </summary>
        private readonly ConcurrentDictionary<ProjectId, int> _projectIdToIdMap = new ConcurrentDictionary<ProjectId, int>();

        /// <summary>
        /// Given a project, and the name of a stream to read/write, gets the integral DB ID to 
        /// use to find the data inside the ProjectData table.
        /// </summary>
        private bool TryGetProjectDataId(SqlConnection connection, Project project, string name, out long dataId)
        {
            dataId = 0;

            // First, try to get all the IDs for this project in sync with the DB.
            // This will only be expensive the first time we do this.  But will save
            // us from tons of back-and-forth as any BG analyzer processes all the
            // documents in a solution.
            BulkPopulateProjectIds(connection, project, fetchStringTable: true);

            var projectId = TryGetProjectId(connection, project);
            var nameId = TryGetStringId(connection, name);
            if (projectId == null || nameId == null)
            {
                return false;
            }

            // Our data ID is just a 64bit int combining the two 32bit values of our projectId and nameId.
            dataId = CombineInt32ValuesToInt64(projectId.Value, nameId.Value);
            return true;
        }

        private int? TryGetProjectId(SqlConnection connection, Project project)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_projectIdToIdMap.TryGetValue(project.Id, out var existingId))
            {
                return existingId;
            }

            var id = TryGetProjectIdFromDatabase(connection, project);
            if (id != null)
            {
                // Cache the value locally so we don't need to go back to the DB in the future.
                _projectIdToIdMap.TryAdd(project.Id, id.Value);
            }

            return id;
        }

        private int? TryGetProjectIdFromDatabase(SqlConnection connection, Project project)
        {
            // Key the project off both its path and name.  That way we work properly
            // in host and test scenarios.
            var projectPathId = TryGetStringId(connection, project.FilePath);
            var projectNameId = TryGetStringId(connection, project.Name);

            if (projectPathId == null || projectNameId == null)
            {
                return null;
            }

            return TryGetStringId(
                connection,
                GetProjectIdString(projectPathId.Value, projectNameId.Value));
        }
    }
}
