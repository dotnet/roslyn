// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using SQLite;

namespace Microsoft.CodeAnalysis.SQLite
{
    /// <summary>
    /// Implementatoin of an <see cref="IPersistentStorage"/> backed by SQLite.
    /// </summary>
    internal partial class SQLitePersistentStorage : AbstractPersistentStorage
    {
        private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

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

        public override void Close()
        {
            // Notify any outstanding async work that it should stop.
            _shutdownTokenSource.Cancel();
        }

        public override void Initialize(Solution solution)
        {
            // Create a connection to the DB and ensure it has tables for the types we care about. 
            var connection = CreateConnection();
            connection.CreateTable<StringInfo>();
            connection.CreateTable<SolutionData>();
            connection.CreateTable<ProjectData>();
            connection.CreateTable<DocumentData>();

            // Also get the known set of string-to-id mappings we already have in the DB.
            FetchStringTable(connection);

            // Try to bulk populate all the IDs we'll need for strings/projects/documents.
            // Bulk population is much faster than trying to do everything individually.
            // Note: we don't need to fetch the string table as we did it right above this.
            BulkPopulateIds(connection, solution, fetchStringTable: false);
        }
    }

    /// <summary>
    /// Inside the DB we have a table dedicated to storing strings that also provides a unique 
    /// integral ID per string.  This allows us to store data keyed in a much more efficient
    /// manner as we can use those IDs instead of duplicating strings all over the place.  For
    /// example, there may be many pieces of data associated with a file.  We don't want to 
    /// key off the file path in all these places as that would cause a large amount of bloat.
    /// 
    /// Because the string table can map from arbitrary strings to unique IDs, it can also be
    /// used to create IDs for compound objects.  For example, given the IDs for the FilePath
    /// and Name of a Project, we can get an ID that represents the project itself by just
    /// creating a compound key of those two IDs.  This ID can then be used in other compound
    /// situations.  For example, a Document's ID is creating by compounding its Project's 
    /// ID, along with the IDs for the Document's FilePath and Name.
    /// </summary>
    internal class StringInfo
    {
        /// <summary>
        /// The unique ID given by the DB for the given <see cref="Value"/>.  Each time we
        /// add a new string, we'll get a fresh ID that autoincrements from the last one.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Ensure that strings are unique in our string table so that each unique string maps to a unique id.
        [Unique]
        public string Value { get; set; }
    }

    /// <summary>
    /// Inside the DB we have a table for data corresponding to the <see cref="Solution"/>.  The 
    /// data is just a blob that is keyed by <see cref="Id"/>.  Data with this ID can be retrieved
    /// or overwritten.
    /// </summary>
    internal class SolutionData
    {
        [PrimaryKey]
        public string Id { get; set; }

        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Inside the DB we have a table for data that we want associated with a <see cref="Project"/>.
    /// The data is keyed off of an integral value produced by combining the ID of the Project and
    /// the ID of the name of the data (see <see cref="SQLitePersistentStorage.ReadStreamAsync(Project, string, CancellationToken)"/>.
    /// 
    /// This gives a very efficient integral key, and means that the we only have to store a 
    /// single mapping from stream name to ID in the string table.
    /// </summary>
    internal class ProjectData
    {
        [PrimaryKey]
        public long Id { get; set; }

        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Inside the DB we have a table for data that we want associated with a <see cref="Project"/>.
    /// The data is keyed off of an integral value produced by combining the ID of the Project and
    /// the ID of the name of the data (see <see cref="SQLitePersistentStorage.ReadStreamAsync(Project, string, CancellationToken)"/>.
    /// 
    /// This gives a very efficient integral key, and means that the we only have to store a 
    /// single mapping from stream name to ID in the string table.
    /// </summary>
    internal class DocumentData
    {
        [PrimaryKey]
        public long Id { get; set; }

        public byte[] Data { get; set; }
    }
}