// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v1
{
    internal partial class SQLitePersistentStorageService : AbstractSQLitePersistentStorageService
    {
        private const string LockFile = "db.lock";
        private const string StorageExtension = "sqlite3";
        private const string PersistentStorageFileName = "storage.ide";

        private readonly IPersistentStorageFaultInjector? _faultInjectorOpt;

        public SQLitePersistentStorageService(IPersistentStorageLocationService locationService)
            : base(locationService)
        {
        }

        public SQLitePersistentStorageService(
            IPersistentStorageLocationService locationService,
            IPersistentStorageFaultInjector faultInjector)
            : this(locationService)
        {
            _faultInjectorOpt = faultInjector;
        }

        protected override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension, nameof(v1), PersistentStorageFileName);
        }

        protected override IChecksummedPersistentStorage? TryOpenDatabase(
            SolutionKey solutionKey, Solution? bulkLoadSnapshot, string workingFolderPath, string databaseFilePath)
        {
            if (!TryInitializeLibraries())
            {
                // SQLite is not supported on the current platform
                return null;
            }

            // try to get db ownership lock. if someone else already has the lock. it will throw
            var dbOwnershipLock = TryGetDatabaseOwnership(databaseFilePath);
            if (dbOwnershipLock == null)
            {
                return null;
            }

            SQLitePersistentStorage? sqlStorage = null;
            try
            {
                sqlStorage = new SQLitePersistentStorage(
                     workingFolderPath, solutionKey.FilePath, databaseFilePath, dbOwnershipLock, _faultInjectorOpt);

                sqlStorage.Initialize(bulkLoadSnapshot);

                return sqlStorage;
            }
            catch (Exception)
            {
                if (sqlStorage != null)
                {
                    // Dispose of the storage, releasing the ownership lock.
                    sqlStorage.Dispose();
                }
                else
                {
                    // The storage was not created so nothing owns the lock.
                    // Dispose the lock to allow reuse.
                    dbOwnershipLock.Dispose();
                }
                throw;
            }
        }

        /// <summary>
        /// Returns null in the case where an IO exception prevented us from being able to acquire
        /// the db lock file.
        /// </summary>
        private static IDisposable? TryGetDatabaseOwnership(string databaseFilePath)
        {
            return IOUtilities.PerformIO<IDisposable?>(() =>
            {
                // make sure directory exist first.
                EnsureDirectory(databaseFilePath);

                var directoryName = Path.GetDirectoryName(databaseFilePath);
                Contract.ThrowIfNull(directoryName);

                return File.Open(
                    Path.Combine(directoryName, LockFile),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }, defaultValue: null);
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

        // Error occurred when trying to open this DB.  Try to remove it so we can create a good DB.
        protected override bool ShouldDeleteDatabase(Exception exception) => true;
    }
}
