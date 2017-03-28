// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;
using SQLite;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorageService : AbstractPersistentStorageService, IPersistentStorageService
    {
        private const string StorageExtension = "sqlite3";
        private const string PersistentStorageFileName = "storage.ide";

        public SQLitePersistentStorageService(
            IOptionService optionService,
            SolutionSizeTracker solutionSizeTracker)
            : base(optionService, solutionSizeTracker)
        {
        }

        public SQLitePersistentStorageService(IOptionService optionService, bool testing) 
            : base(optionService, testing)
        {
        }

        protected override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension, PersistentStorageFileName);
        }

        protected override bool TryCreatePersistentStorage(
            string workingFolderPath, string solutionPath,
            out AbstractPersistentStorage persistentStorage)
        {
            persistentStorage = null;
            SQLitePersistentStorage database = null;

            try
            {
                database = new SQLitePersistentStorage(OptionService, 
                    workingFolderPath, solutionPath, GetDatabaseFilePath(workingFolderPath),
                    this.Release);
                database.Initialize();

                persistentStorage = database;
                return true;
            }
            catch (SQLiteException ex)
            {
                // db is already in use by someone.
                if (database != null)
                {
                    database.Close();
                }

                StorageDatabaseLogger.LogException(ex);
                return false;
            }
            catch (Exception ex)
            {
                if (database != null)
                {
                    database.Close();
                }

                StorageDatabaseLogger.LogException(ex);
            }

            if (database != null)
            {
                IOUtilities.PerformIO(() => Directory.Delete(database.DatabaseDirectory, recursive: true));
            }

            return false;
        }
    }
}