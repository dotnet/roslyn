// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Storage;
using SQLite;
using static System.FormattableString;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal class SQLitePersistentStorage : AbstractPersistentStorage
    {
        /// <summary>
        /// A persistent connection we keep around to make async requests to the db.
        /// </summary>
        private readonly SQLiteAsyncConnection _connection;

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
            _connection = new SQLiteAsyncConnection(databaseFile);
        }

        public override void Initialize()
        {
            // Create a sync connection to the DB and ensure it has tables for the types we care about. 
            using (var syncConnection = new SQLiteConnection(DatabaseFile))
            {
                syncConnection.CreateTable<StringInfo>();
                syncConnection.CreateTable<SolutionData>();
                syncConnection.CreateTable<ProjectData>();
                syncConnection.CreateTable<DocumentData>();

                // Also getthe known set of string-to-id mappings we already have in the DB.
                foreach (var v in syncConnection.Table<StringInfo>())
                {
                    _stringToIdMap.TryAdd(v.Value, v.Id);
                }
            }
        }

        private static Stream GetStream(byte[] bytes)
            => bytes == null ? null : new MemoryStream(bytes, writable: false);

        private string GetProjectDataId(int id, string name)
            => Invariant($"{id}-{name}");

        private string GetDocumentDataId(int id, string name)
            => Invariant($"{id}-{name}");

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

        public override async Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            SolutionData data = null;
            try
            {
                data = await _connection.FindAsync<SolutionData>(name).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }

            return GetStream(data?.Data);
        }

        public override async Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bytes = GetBytes(stream);

            try
            {
                await _connection.InsertOrReplaceAsync(new SolutionData { Id = name, Data = bytes }).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
                return false;
            }
        }

        #endregion

        #region Project Serialization

        public override async Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            var id = await TryGetProjectIdAsync(project).ConfigureAwait(false);
            if (id == null)
            {
                return null;
            }

            ProjectData projectData = null;
            try
            {
                projectData = await _connection.GetAsync<ProjectData>(GetProjectDataId(id.Value, name)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }

            return GetStream(projectData?.Data);
        }

        public override async Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bytes = GetBytes(stream);
            var id = await TryGetProjectIdAsync(project).ConfigureAwait(false);

            if (id != null)
            {
                try
                {
                    await _connection.InsertOrReplaceAsync(
                        new ProjectData { Id = GetProjectDataId(id.Value, name), Data = bytes }).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    StorageDatabaseLogger.LogException(ex);
                }
            }

            return false;
        }

        #endregion

        #region Project Serialization

        public override async Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            var id = await TryGetDocumentIdAsync(document).ConfigureAwait(false);
            if (id == null)
            {
                return null;
            }

            DocumentData documentData = null;
            try
            {
                documentData = await _connection.GetAsync<DocumentData>(GetProjectDataId(id.Value, name)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }

            return GetStream(documentData?.Data);
        }

        public override async Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default(CancellationToken))
        {
            var bytes = GetBytes(stream);
            var id = await TryGetDocumentIdAsync(document).ConfigureAwait(false);

            if (id != null)
            {
                try
                {
                    await _connection.InsertOrReplaceAsync(
                        new DocumentData { Id = GetDocumentDataId(id.Value, name), Data = bytes }).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    StorageDatabaseLogger.LogException(ex);
                }
            }

            return false;
        }

        private async Task<int?> TryGetProjectIdAsync(Project project)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_projectIdToIdMap.TryGetValue(project.Id, out var existingId))
            {
                return existingId;
            }

            var id = await TryGetProjectIdFromDatabaseAsync(project).ConfigureAwait(false);
            if (id != null)
            {
                // Cache the value locally so we don't need to go back to the DB in the future.
                _projectIdToIdMap.TryAdd(project.Id, id.Value);
            }

            return id;
        }

        private async Task<int?> TryGetProjectIdFromDatabaseAsync(Project project)
        {
            // Key the project off both its path and name.  That way we work properly
            // in host and test scenarios.
            var projectPathId = await TryGetStringIdAsync(project.FilePath).ConfigureAwait(false);
            var projectNameId = await TryGetStringIdAsync(project.Name).ConfigureAwait(false);

            if (projectPathId == null || projectNameId == null)
            {
                return null;
            }

            // Unique identify the project through the key:  P-projectPathId-projectNameId
            return await TryGetStringIdAsync(
                Invariant($"P-{projectPathId.Value}-{projectNameId.Value}")).ConfigureAwait(false);
        }

        private async Task<int?> TryGetDocumentIdAsync(Document document)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_documentIdToIdMap.TryGetValue(document.Id, out var existingId))
            {
                return existingId;
            }

            var id = await TryGetDocumentIdFromDatabaseAsync(document).ConfigureAwait(false);
            if (id != null)
            {
                // Cache the value locally so we don't need to go back to the DB in the future.
                _documentIdToIdMap.TryAdd(document.Id, id.Value);
            }

            return id;
        }

        private async Task<int?> TryGetDocumentIdFromDatabaseAsync(Document document)
        {
            var projectId = await TryGetProjectIdAsync(document.Project).ConfigureAwait(false);
            if (projectId == null)
            {
                return null;
            }

            // Key the document off its project id, and its path and name.  That way we work properly
            // in host and test scenarios.
            var documentPathId = await TryGetStringIdAsync(document.FilePath).ConfigureAwait(false);
            var documentNameId = await TryGetStringIdAsync(document.Name).ConfigureAwait(false);

            if (documentPathId == null || documentNameId == null)
            {
                return null;
            }

            // Unique identify the document through the key:  D-projectId-documentPathId-documentNameId
            return await TryGetStringIdAsync(
                Invariant($"D-{projectId.Value}-{documentPathId.Value}-{documentNameId.Value}")).ConfigureAwait(false);
        }

        #endregion

        private async Task<int?> TryGetStringIdAsync(string value)
        {
            // First see if we've cached the ID for this value locally.  If so, just return
            // what we already have.
            if (_stringToIdMap.TryGetValue(value, out int existingId))
            {
                return existingId;
            }

            // Otherwise, try to get or add the string to the string table in the database.
            var id = await TryGetStringIdFromDatabaseAsync(value).ConfigureAwait(false);
            if (id != null)
            {
                _stringToIdMap.TryAdd(value, id.Value);
            }

            return id;
        }

        private async Task<int?> TryGetStringIdFromDatabaseAsync(string value)
        {
            // First, check if we can find that string in the string table.
            var stringInfo = await TryGetStringIdFromDatabaseWorkerAsync(value, canReturnNull: true).ConfigureAwait(false);
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
                var newId = await _connection.InsertAsync(new StringInfo { Value = value }).ConfigureAwait(false);

                // Successfully added the string.  Return the ID it was given.
                return newId;
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint)
            {
                // We got a constraint violation.  This means someone else beat us to adding this
                // string to the string-table.  We should always be able to find the string now.
                stringInfo = await TryGetStringIdFromDatabaseWorkerAsync(value, canReturnNull: false).ConfigureAwait(false);
                return stringInfo?.Id;
            }
            catch (Exception ex)
            {
                // Some other error occurred.  Log it and return nothing.
                StorageDatabaseLogger.LogException(ex);
            }

            return null;
        }

        private async Task<StringInfo> TryGetStringIdFromDatabaseWorkerAsync(string value, bool canReturnNull)
        {
            StringInfo stringInfo = null;

            try
            {
                stringInfo = await _connection.Table<StringInfo>().Where(i => i.Value == value).FirstOrDefaultAsync().ConfigureAwait(false);
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
        public string Id { get; set; }

        public byte[] Data { get; set; }
    }

    internal class DocumentData
    {
        [PrimaryKey]
        public string Id { get; set; }

        public byte[] Data { get; set; }
    }
}