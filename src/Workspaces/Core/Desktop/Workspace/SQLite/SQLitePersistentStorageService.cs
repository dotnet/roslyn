// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorageService : AbstractPersistentStorageService
    {
        private const string LockFile = "db.lock";
        private const string StorageExtension = "sqlite3";
        private const string PersistentStorageFileName = "storage.ide";

        private readonly IPersistentStorageFaultInjector _faultInjectorOpt;

        public SQLitePersistentStorageService(
            IOptionService optionService,
            IPersistentStorageLocationService locationService,
            SolutionSizeTracker solutionSizeTracker)
            : base(optionService, locationService, solutionSizeTracker)
        {
        }

        public SQLitePersistentStorageService(
            IOptionService optionService,
            IPersistentStorageLocationService locationService,
            SolutionSizeTracker solutionSizeTracker,
            IPersistentStorageFaultInjector faultInjector)
            : this(optionService, locationService, solutionSizeTracker)
        {
            _faultInjectorOpt = faultInjector;
        }

        protected override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension, PersistentStorageFileName);
        }

        protected override bool TryOpenDatabase(
            Solution solution, string workingFolderPath, string databaseFilePath, out IPersistentStorage storage)
        {
            // try to get db ownership lock. if someone else already has the lock. it will throw
            var dbOwnershipLock = TryGetDatabaseOwnership(databaseFilePath);
            if (dbOwnershipLock == null)
            {
                storage = null;
                return false;
            }

            var sqlStorage = new SQLitePersistentStorage(
                 workingFolderPath, solution.FilePath, databaseFilePath, dbOwnershipLock, _faultInjectorOpt);

            sqlStorage.Initialize(solution);

            storage = sqlStorage;
            return true;
        }

        private static IDisposable TryGetDatabaseOwnership(string databaseFilePath)
        {
            return IOUtilities.PerformIO<IDisposable>(() =>
            {
                // make sure directory exist first.
                EnsureDirectory(databaseFilePath);

                return File.Open(
                    Path.Combine(Path.GetDirectoryName(databaseFilePath), LockFile),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            });
        }

        private static void EnsureDirectory(string databaseFilePath)
        {
            var directory = Path.GetDirectoryName(databaseFilePath);
            if (Directory.Exists(directory))
            {
                return;
            }

            Directory.CreateDirectory(directory);
        }

        protected override bool ShouldDeleteDatabase(Exception exception)
        {
            // Error occurred when trying to open this DB.  Try to remove it so we can create a good dB.
            return true;
        }
    }
}
