// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    internal class SQLitePersistentStorageService : AbstractSQLitePersistentStorageService
    {
        private const string StorageExtension = "sqlite3";
        private const string PersistentStorageFileName = "storage.ide";

        private readonly SQLiteConnectionPoolService _connectionPoolService;
        private readonly IPersistentStorageFaultInjector? _faultInjector;

        public SQLitePersistentStorageService(SQLiteConnectionPoolService connectionPoolService, IPersistentStorageLocationService locationService)
            : base(locationService)
        {
            _connectionPoolService = connectionPoolService;
        }

        public SQLitePersistentStorageService(
            SQLiteConnectionPoolService connectionPoolService,
            IPersistentStorageLocationService locationService,
            IPersistentStorageFaultInjector? faultInjector)
            : this(connectionPoolService, locationService)
        {
            _faultInjector = faultInjector;
        }

        protected override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension, nameof(v2), PersistentStorageFileName);
        }

        protected override IChecksummedPersistentStorage? TryOpenDatabase(
            SolutionKey solutionKey, Solution? bulkLoadSnapshot, string workingFolderPath, string databaseFilePath)
        {
            if (!TryInitializeLibraries())
            {
                // SQLite is not supported on the current platform
                return null;
            }

            return SQLitePersistentStorage.TryCreate(
                _connectionPoolService,
                bulkLoadSnapshot,
                workingFolderPath,
                solutionKey.FilePath,
                databaseFilePath,
                _faultInjector);
        }

        // Error occurred when trying to open this DB.  Try to remove it so we can create a good DB.
        protected override bool ShouldDeleteDatabase(Exception exception) => true;
    }
}
