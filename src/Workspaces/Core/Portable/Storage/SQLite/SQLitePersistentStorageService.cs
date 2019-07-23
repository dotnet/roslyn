// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
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

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        private static bool TryInitializeLibraries() => s_initialized.Value;

        private static readonly Lazy<bool> s_initialized = new Lazy<bool>(() => TryInitializeLibrariesLazy());

        private static bool TryInitializeLibrariesLazy()
        {
            // Attempt to load the correct version of e_sqlite.dll.  That way when we call
            // into SQLitePCL.Batteries_V2.Init it will be able to find it.
            //
            // Only do this on Windows when we can safely do the LoadLibrary call to this
            // direct dll.  On other platforms, it is the responsibility of the host to ensure
            // that the necessary sqlite library has already been loaded such that SQLitePCL.Batteries_V2
            // will be able to call into it.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var myFolder = Path.GetDirectoryName(
                    typeof(SQLitePersistentStorage).Assembly.Location);

                var is64 = IntPtr.Size == 8;
                var subfolder = is64 ? "x64" : "x86";

                LoadLibrary(Path.Combine(myFolder, subfolder, "e_sqlite3.dll"));
            }

            try
            {
                // Necessary to initialize SQLitePCL.
                SQLitePCL.Batteries_V2.Init();
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                StorageDatabaseLogger.LogException(e);
                return false;
            }

            return true;
        }

        public SQLitePersistentStorageService(
            IOptionService optionService,
            IPersistentStorageLocationService locationService,
            ISolutionSizeTracker solutionSizeTracker)
            : base(optionService, locationService, solutionSizeTracker)
        {
        }

        public SQLitePersistentStorageService(
            IOptionService optionService,
            IPersistentStorageLocationService locationService,
            ISolutionSizeTracker solutionSizeTracker,
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
            Solution solution, string workingFolderPath, string databaseFilePath, out IChecksummedPersistentStorage storage)
        {
            if (!TryInitializeLibraries())
            {
                // SQLite is not supported on the current platform
                storage = null;
                return false;
            }

            // try to get db ownership lock. if someone else already has the lock. it will throw
            var dbOwnershipLock = TryGetDatabaseOwnership(databaseFilePath);
            if (dbOwnershipLock == null)
            {
                storage = null;
                return false;
            }

            SQLitePersistentStorage sqlStorage = null;
            try
            {
                sqlStorage = new SQLitePersistentStorage(
                     workingFolderPath, solution.FilePath, databaseFilePath, dbOwnershipLock, _faultInjectorOpt);

                sqlStorage.Initialize(solution);

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

        // Error occurred when trying to open this DB.  Try to remove it so we can create a good DB.
        protected override bool ShouldDeleteDatabase(Exception exception) => true;
    }
}
