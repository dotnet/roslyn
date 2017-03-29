// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;
using SQLite;
using static System.FormattableString;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal class SQLitePersistentStorage : AbstractPersistentStorage
    {
        // Caches from local data to the corresponding database ID for that data.
        // Kept locally so we can avoid hitting the DB for common data.
        private readonly ConcurrentDictionary<string, int> _stringToIdMap = new ConcurrentDictionary<string, int>();
        private readonly ConcurrentDictionary<ProjectId, int> _projectIdToIdMap = new ConcurrentDictionary<ProjectId, int>();
        private readonly ConcurrentDictionary<DocumentId, int> _documentIdToIdMap = new ConcurrentDictionary<DocumentId, int>();

        private readonly ConcurrentDictionary<ProjectId, object> _projectBulkPopulatedLock = new ConcurrentDictionary<ProjectId, object>();
        private readonly HashSet<ProjectId> _projectBulkPopulatedMap = new HashSet<ProjectId>();

        public SQLitePersistentStorage(
            IOptionService optionService,
            string workingFolderPath,
            string solutionFilePath,
            string databaseFile,
            Action<AbstractPersistentStorage> disposer)
            : base(optionService, workingFolderPath, solutionFilePath, databaseFile, disposer)
        {
        }

        private SQLiteConnection CreateConnection()
        {
            var connection = new SQLiteConnection(
                this.DatabaseFile,
                SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite);
            connection.BusyTimeout = TimeSpan.FromMinutes(1);
            return connection;
        }

        public override void Initialize(Solution solution)
        {
            // Create a sync connection to the DB and ensure it has tables for the types we care about. 
            var connection = CreateConnection();
            connection.CreateTable<StringInfo>();
            connection.CreateTable<SolutionData>();
            connection.CreateTable<ProjectData>();
            connection.CreateTable<DocumentData>();

            // Also get the known set of string-to-id mappings we already have in the DB.
            FetchStringTable(connection);

            // Try to bulk populate all the IDs we'll need for strings/projects/documents.
            // Bulk population is much faster than trying to do everythign individually.
            BulkPopulateIds(connection, solution);
        }

        private void FetchStringTable(SQLiteConnection connection)
        {
            foreach (var v in connection.Table<StringInfo>())
            {
                AddToStringTable(v);
            }
        }

        private bool AddToStringTable(StringInfo stringInfo)
        {
            // Note that TryAdd won't overwrite an existing string->id pair.  That's what
            // we want.  we don't want the strings we've allocated from the DB to be what
            // we hold onto.  We'd rather hold onto the strings we get from sources like
            // the workspaces, to prevent excessice duplication.
            return _stringToIdMap.TryAdd(stringInfo.Value, stringInfo.Id);
        }

        public override void Close()
        {
        }

        private static Stream GetStream(byte[] bytes)
            => bytes == null ? null : new MemoryStream(bytes, writable: false);

        private long GetProjectDataId(int projectId, int nameId)
            => CombineInt32ToInt64(projectId, nameId);

        private static string GetProjectIdString(int projectPathId, int projectNameId)
            => Invariant($"{projectPathId}-{projectNameId}");

        private long GetDocumentDataId(int documentId, int nameId)
            => CombineInt32ToInt64(documentId, nameId);

        private static string GetDocumentIdString(int projectId, int documentPathId, int documentNameId)
            => Invariant($"{projectId}-{documentPathId}-{documentNameId}");

        private long CombineInt32ToInt64(int v1, int v2)
            => ((long)v1 << 32) | (long)v2;

        private byte[] GetBytes(Stream stream)
        {
            // If we were provided a memory stream to begin with, we can preallocate the right size
            // byte[] to copy into.  Note: this is potentially allocating large buffers.  Those will
            // be GC'ed, but they can have a negative effect on the large object heap as compaction
            // is rarer there.  Unfortunately, the .Net sqlite wrapper library does not expose any
            // way to just get access to the underlying sqlite blob to read/write directly to.
            if (stream is MemoryStream memoryStream)
            {
                var bytes = new byte[memoryStream.Length];
                using (var tempStream = new MemoryStream(bytes))
                {
                    stream.CopyTo(tempStream);
                    return bytes;
                }
            }
            else
            {
                using (var tempStream = new MemoryStream())
                {
                    stream.CopyTo(tempStream);
                    return tempStream.ToArray();
                }
            }
        }

        private void BulkPopulateIds(SQLiteConnection connection, Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                BulkPopulateProjectIds(connection, project);
            }
        }

        private void BulkPopulateProjectIds(SQLiteConnection connection, Project project)
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

                // Ensure our string table is up to date with the DB.
                FetchStringTable(connection);

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
            if (!AddDocumentIds())
            {
                return false;
            }
            
            return true;

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

                    // However, this will have made it so that we're not holding onto a lot of db
                    // strings.  That can lead to a lot of duplication.  So go over and ensure
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
                if (!_stringToIdMap.ContainsKey(value))
                {
                    stringsToAdd.Add(value);
                }
            }
        }

        #region Solution Serialization

        public override Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken)
        {
            SolutionData data = null;
            try
            {
                data = CreateConnection().Find<SolutionData>(name);
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }

            return Task.FromResult(GetStream(data?.Data));
        }

        public override Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
        {
            var bytes = GetBytes(stream);

            try
            {
                CreateConnection().InsertOrReplace(
                    new SolutionData { Id = name, Data = bytes });
                return SpecializedTasks.True;
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
                return SpecializedTasks.False;
            }
        }

        #endregion

        #region Project Serialization

        public override Task<Stream> ReadStreamAsync(
            Project project, string name, CancellationToken cancellationToken)
        {
            var connection = CreateConnection();
            BulkPopulateProjectIds(connection, project);

            var projectID = TryGetProjectId(connection, project);
            var nameId = TryGetStringId(connection, name);
            if (projectID == null || nameId == null)
            {
                return SpecializedTasks.Default<Stream>();
            }

            ProjectData projectData = null;
            try
            {
                projectData = connection.Find<ProjectData>(GetProjectDataId(projectID.Value, nameId.Value));
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }

            return Task.FromResult(GetStream(projectData?.Data));
        }

        public override Task<bool> WriteStreamAsync(
            Project project, string name, Stream stream, CancellationToken cancellationToken)
        {
            var connection = CreateConnection();
            BulkPopulateProjectIds(connection, project);

            var projectId = TryGetProjectId(connection, project);
            var nameId = TryGetStringId(connection, name);

            if (projectId != null && nameId != null)
            {
                var bytes = GetBytes(stream);

                try
                {
                    connection.InsertOrReplace(
                        new ProjectData { Id = GetProjectDataId(projectId.Value, nameId.Value), Data = bytes });
                    return SpecializedTasks.True;
                }
                catch (Exception ex)
                {
                    StorageDatabaseLogger.LogException(ex);
                }
            }

            return SpecializedTasks.False;
        }

        #endregion

        #region Project Serialization

        public override Task<Stream> ReadStreamAsync(
            Document document, string name, CancellationToken cancellationToken)
        {
            var connection = CreateConnection();
            BulkPopulateProjectIds(connection, document.Project);

            var documentID = TryGetDocumentId(connection, document);
            var nameId = TryGetStringId(connection, name);
            if (documentID == null || nameId == null)
            {
                return SpecializedTasks.Default<Stream>();
            }

            DocumentData documentData = null;
            try
            {
                documentData = connection.Find<DocumentData>(
                    GetProjectDataId(documentID.Value, nameId.Value));
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }

            return Task.FromResult(GetStream(documentData?.Data));
        }

        public override Task<bool> WriteStreamAsync(
            Document document, string name, Stream stream, CancellationToken cancellationToken)
        {
            var connection = CreateConnection();
            BulkPopulateProjectIds(connection, document.Project);

            var documentId = TryGetDocumentId(connection, document);
            var nameId = TryGetStringId(connection, name);

            if (documentId != null && nameId != null)
            {
                var bytes = GetBytes(stream);

                try
                {
                    connection.InsertOrReplace(
                        new DocumentData { Id = GetDocumentDataId(documentId.Value, nameId.Value), Data = bytes });
                    return SpecializedTasks.True;
                }
                catch (Exception ex)
                {
                    StorageDatabaseLogger.LogException(ex);
                }
            }

            return SpecializedTasks.False;
        }

        private int? TryGetProjectId(SQLiteConnection connection, Project project)
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

        private int? TryGetProjectIdFromDatabase(SQLiteConnection connection, Project project)
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

        private int? TryGetDocumentId(SQLiteConnection connection, Document document)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_documentIdToIdMap.TryGetValue(document.Id, out var existingId))
            {
                return existingId;
            }

            var id = TryGetDocumentIdFromDatabase(connection, document);
            if (id != null)
            {
                // Cache the value locally so we don't need to go back to the DB in the future.
                _documentIdToIdMap.TryAdd(document.Id, id.Value);
            }

            return id;
        }

        private int? TryGetDocumentIdFromDatabase(SQLiteConnection connection, Document document)
        {
            var projectId = TryGetProjectId(connection, document.Project);
            if (projectId == null)
            {
                return null;
            }

            // Key the document off its project id, and its path and name.  That way we work properly
            // in host and test scenarios.
            var documentPathId = TryGetStringId(connection, document.FilePath);
            var documentNameId = TryGetStringId(connection, document.Name);

            if (documentPathId == null || documentNameId == null)
            {
                return null;
            }

            // Unique identify the document through the key:  D-projectId-documentPathId-documentNameId
            return TryGetStringId(
                connection,
                GetDocumentIdString(projectId.Value, documentPathId.Value, documentNameId.Value));
        }

        #endregion

        private int? TryGetStringId(SQLiteConnection connection, string value)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_stringToIdMap.TryGetValue(value, out int existingId))
            {
                return existingId;
            }

            // Otherwise, try to get or add the string to the string table in the database.
            var id = TryGetStringIdFromDatabase(connection, value);
            if (id != null)
            {
                _stringToIdMap.TryAdd(value, id.Value);
            }

            return id;
        }

        private int? TryGetStringIdFromDatabase(SQLiteConnection connection, string value)
        {
            // First, check if we can find that string in the string table.
            var stringInfo = TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: true);
            if (stringInfo != null)
            {
                // Found the value already in the db.  Another process (or thread) might have added it.
                // We're done at this point.
                return stringInfo.Id;
            }

            // The string wasn't in the db string table.  Add it.  Note: this may fail if some
            // other thread/process beats us there as this table has a 'unique' constraint on the
            // values.
            try
            {
                stringInfo = new StringInfo { Value = value };
                connection.Insert(stringInfo);

                // Successfully added the string.  Return the ID it was given.
                return stringInfo.Id;
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
            {
                // We got a constraint violation.  This means someone else beat us to adding this
                // string to the string-table.  We should always be able to find the string now.
                stringInfo = TryGetStringIdFromDatabaseWorker(connection, value, canReturnNull: false);
                return stringInfo.Id;
            }
            catch (Exception ex)
            {
                // Some other error occurred.  Log it and return nothing.
                StorageDatabaseLogger.LogException(ex);
            }

            return null;
        }

        private StringInfo TryGetStringIdFromDatabaseWorker(
            SQLiteConnection connection, string value, bool canReturnNull)
        {
            StringInfo stringInfo = null;

            try
            {
                stringInfo = connection.Table<StringInfo>()
                    .Where(i => i.Value == value)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                // If we simply failed to even talk to the DB then we have to bail out.  There's
                // nothing we can accomplish at this point.
                StorageDatabaseLogger.LogException(ex);
                return null;
            }

            // If we got a real value then return it. If we got null back, then return it if our caller
            // is ok with that otherwise throw if it was not expected
            if (stringInfo != null)
            {
                return stringInfo;
            }

            if (canReturnNull)
            {
                return null;
            }

            throw new InvalidOperationException();
        }
    }

    internal class StringInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Ensure that strings are unique in our string table so that each unique string maps to a unique id.
        [Unique]
        public string Value { get; set; }
    }

    internal class SolutionData
    {
        [PrimaryKey]
        public string Id { get; set; }

        public byte[] Data { get; set; }
    }

    internal class ProjectData
    {
        [PrimaryKey]
        public long Id { get; set; }

        public byte[] Data { get; set; }
    }

    internal class DocumentData
    {
        [PrimaryKey]
        public long Id { get; set; }

        public byte[] Data { get; set; }
    }
}