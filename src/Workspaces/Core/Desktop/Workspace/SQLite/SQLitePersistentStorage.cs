// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
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
            foreach (var v in connection.Table<StringInfo>())
            {
                _stringToIdMap.TryAdd(v.Value, v.Id);
            }
        }

        public override void Close()
        {
        }

        private static Stream GetStream(byte[] bytes)
            => bytes == null ? null : new MemoryStream(bytes, writable: false);

        private long GetProjectDataId(int projectId, int nameId)
            => CombineInt32ToInt64(projectId, nameId);

        private long GetDocumentDataId(int documentId, int nameId)
            => CombineInt32ToInt64(documentId, nameId);

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

            // Unique identify the project through the key:  P-projectPathId-projectNameId
            return TryGetStringId(
                connection,
                Invariant($"{projectPathId.Value}-{projectNameId.Value}"));
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
                Invariant($"{projectId.Value}-{documentPathId.Value}-{documentNameId.Value}"));
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