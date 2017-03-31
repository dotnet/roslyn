// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;
using SQLite;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorageService : AbstractPersistentStorageService<SQLiteException>
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

        protected override AbstractPersistentStorage OpenDatabase(Solution solution, string workingFolderPath)
            => new SQLitePersistentStorage(OptionService,
                    workingFolderPath, solution.FilePath, GetDatabaseFilePath(workingFolderPath),
                    this.Release);

        protected override bool ShouldDeleteDatabase(SQLiteException ex)
        {
            // Error occurred when trying to open this DB.  Try to remove it so we can create a good
            // DB.  Report the issue to help track down what's wrong.
            FatalError.ReportWithoutCrash(ex);
            return true;
        }
    }
}