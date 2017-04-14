﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorageService : AbstractPersistentStorageService
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

        protected override AbstractPersistentStorage OpenDatabase(Solution solution, string workingFolderPath, string databaseFilePath)
            => new SQLitePersistentStorage(
                OptionService, workingFolderPath, solution.FilePath, databaseFilePath, this.Release);

        protected override bool ShouldDeleteDatabase(Exception exception)
        {
            // Error occurred when trying to open this DB.  Try to remove it so we can create a good dB.
            return true;
        }
    }
}