// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Storage;
using SQLite;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        private readonly ConcurrentDictionary<ProjectId, object> _projectBulkPopulatedLock = new ConcurrentDictionary<ProjectId, object>();
        private readonly HashSet<ProjectId> _projectBulkPopulatedMap = new HashSet<ProjectId>();

        private void BulkPopulateIds(SQLiteConnection connection, Solution solution, bool fetchStringTable)
        {
            foreach (var project in solution.Projects)
            {
                BulkPopulateProjectIds(connection, project, fetchStringTable);
            }
        }

        private void BulkPopulateProjectIds(SQLiteConnection connection, Project project, bool fetchStringTable)
        {
            // Ensure that only one caller is trying to bulk populate a project at a time.
            var gate = _projectBulkPopulatedLock.GetOrAdd(project.Id, _ => new object());
            lock (gate)
            {
                if (_projectBulkPopulatedMap.Contains(project.Id))
                {
                    // We've already bulk processed this project.  No need to do so again.
                    return;
                }

                // Ensure our string table is up to date with the DB.  Note: we want to do this 
                // to prevent the following problem:
                //
                // 1) Process1 and Process2 are concurrently attempting to bulk populate the DB.  Process1
                // ends up populating the DB.  Process2 then tries to do the same, and gets a constraint
                // violation because it is trying to add the same strings as Process1 did.  Because of the
                // contraint violation, Process2 will back off to try again later.  Unless it actually gets
                // the current string table, it will keep having problems trying to bulk populate.
                if (fetchStringTable)
                {
                    FetchStringTable(connection);
                }

                if (!BulkPopulateProjectIdsWorker(connection, project))
                {
                    // Something went wrong.  Try to bulk populate this project later.
                    return;
                }

                // Successfully bulk populated.  Mark as such so we don't bother doing this again.
                _projectBulkPopulatedMap.Add(project.Id);
            }
        }

        /// <summary>
        /// Returns 'true' if the bulk population succeeds, or false if it doesn't.
        /// </summary>
        private bool BulkPopulateProjectIdsWorker(SQLiteConnection connection, Project project)
        {
            // First, in bulk, get string-ids for all the paths and names for the project and documents.
            if (!AddIndividualProjectAndDocumentComponentIds())
            {
                return false;
            }

            // Now, ensure we have the project id known locally.  If this fails for some reason,
            // we can't proceed.
            var projectId = TryGetProjectId(connection, project);
            if (projectId == null)
            {
                return false;
            }

            // Finally, in bulk, get string-ids for all the documents in the project.
            return AddDocumentIds();

            // Local functions below.

            // Use local functions so that other members of this class don't accidently
            // use these.  There are invariants in the context of PopulateProjectIds that
            // these functions can depend on.
            bool AddIndividualProjectAndDocumentComponentIds()
            {
                var stringsToAdd = new HashSet<string>();
                AddIfUnknownId(project.FilePath, stringsToAdd);
                AddIfUnknownId(project.Name, stringsToAdd);

                foreach (var document in project.Documents)
                {
                    AddIfUnknownId(document.FilePath, stringsToAdd);
                    AddIfUnknownId(document.Name, stringsToAdd);
                }

                return AddStrings(stringsToAdd);
            }

            bool AddStrings(HashSet<string> stringsToAdd)
            {
                if (stringsToAdd.Count > 0)
                {
                    var stringInfos = stringsToAdd.Select(s => new StringInfo { Value = s }).ToArray();
                    try
                    {
                        // SQLite will populate the StringInfo.Id property when it inserts these.
                        connection.InsertAll(stringInfos, runInTransaction: true);
                    }
                    catch (Exception ex)
                    {
                        // Something failed. Log the issue, and let the caller know we should stop
                        // with the bulk population.
                        StorageDatabaseLogger.LogException(ex);
                        return false;
                    }

                    // We succeeded inserting all the strings.  Ensure our local cache has all the
                    // values we added.
                    foreach (var stringInfo in stringInfos)
                    {
                        AddToStringTable(stringInfo);
                    }

                    // However, this will have made it so that we're now holding onto a lot of db
                    // strings.  That can lead to a lot of string duplication.  So go over and ensure
                    // that the strings we're actually using as keys are the ones we got from 
                    // the workspace.
                    foreach (var s in stringsToAdd)
                    {
                        _stringToIdMap[s] = _stringToIdMap[s];
                    }
                }

                return true;
            }

            bool AddDocumentIds()
            {
                var stringsToAdd = new HashSet<string>();

                foreach (var document in project.Documents)
                {
                    // Produce the string like "projId-docPathId-docNameId" so that we can get a
                    // unique ID for it.
                    AddIfUnknownId(GetDocumentIdString(document), stringsToAdd);
                }

                // Ensure we have unique IDs for all these document string ids.  If we fail to 
                // bulk import these strings, we can't proceed.
                if (!AddStrings(stringsToAdd))
                {
                    return false;
                }

                foreach (var document in project.Documents)
                {
                    // Get the integral ID for this document.  It's safe to directly index into
                    // the map as we just successfully added these strings to the DB.
                    var id = _stringToIdMap[GetDocumentIdString(document)];
                    _documentIdToIdMap.TryAdd(document.Id, id);
                }

                return true;
            }

            string GetDocumentIdString(Document document)
            {
                // We should always be able to index directly into these maps.  This function is only
                // ever called after we called AddIndividualProjectAndDocumentComponentIds.
                var documentPathId = _stringToIdMap[document.FilePath];
                var documentNameId = _stringToIdMap[document.Name];

                var documentIdString = SQLitePersistentStorage.GetDocumentIdString(
                    projectId.Value, documentPathId, documentNameId);
                return documentIdString;
            }

            void AddIfUnknownId(string value, HashSet<string> stringsToAdd)
            {
                if (!_stringToIdMap.TryGetValue(value, out var id))
                {
                    stringsToAdd.Add(value);
                }
                else
                {
                    // We did know about this string.  However, we want to ensure that the 
                    // actual string instance we're pointing to is the one produced by the
                    // rest of the workspace, and not by the database.  This way we don't
                    // end up having tons of duplicate strings in the storage service.
                    //
                    // So overwrite whatever we have so far in the table so we can release
                    // the DB strings.
                    _stringToIdMap[value] = id;
                }
            }
        }
    }
}