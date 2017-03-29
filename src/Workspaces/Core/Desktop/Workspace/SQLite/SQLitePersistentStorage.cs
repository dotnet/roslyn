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
            _shutdownTokenSource.Cancel();
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
            // Bulk population is much faster than trying to do everything individually.
            // Note: we don't need to fetch the string table as we did it right above this.
            BulkPopulateIds(connection, solution, fetchStringTable: false);
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