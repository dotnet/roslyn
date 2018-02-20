// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionSize;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// A service that enables storing and retrieving of information associated with solutions,
    /// projects or documents across runtime sessions.
    /// </summary>
    internal abstract partial class AbstractPersistentStorageService : IPersistentStorageService2
    {
        protected readonly IOptionService OptionService;
        private readonly SolutionSizeTracker _solutionSizeTracker;

        private readonly bool _testing;
        private readonly object _primaryStorageAccessLock;
        private readonly PrimaryStorageInfo _primaryStorage;

        private string _lastSolutionPath;

        protected AbstractPersistentStorageService(
            IOptionService optionService,
            SolutionSizeTracker solutionSizeTracker)
        {
            OptionService = optionService;
            _solutionSizeTracker = solutionSizeTracker;

            _primaryStorageAccessLock = new object();

            _lastSolutionPath = null;
            _primaryStorage = new PrimaryStorageInfo();
        }

        protected AbstractPersistentStorageService(IOptionService optionService, bool testing)
            : this(optionService, solutionSizeTracker: null)
        {
            _testing = true;
        }

        protected abstract string GetDatabaseFilePath(string workingFolderPath);
        protected abstract bool TryOpenDatabase(Solution solution, string workingFolderPath, string databaseFilePath, out AbstractPersistentStorage storage);
        protected abstract bool ShouldDeleteDatabase(Exception exception);

        public IPersistentStorage GetStorage(Solution solution)
            => GetStorage(solution, checkBranchId: true);

        public IPersistentStorage GetStorage(Solution solution, bool checkBranchId)
        {
            if (!DatabaseSupported(solution, checkBranchId))
            {
                return NoOpPersistentStorage.Instance;
            }

            // check whether the solution actually exist on disk
            if (!CheckSolutionFileExist(solution.FilePath))
            {
                return NoOpPersistentStorage.Instance;
            }

            // get working folder path
            var workingFolderPath = GetWorkingFolderPath(solution);
            if (workingFolderPath == null)
            {
                // we don't have place to save db file. don't use db
                return NoOpPersistentStorage.Instance;
            }

            return GetStorage(solution, workingFolderPath);
        }

        private bool CheckSolutionFileExist(string solutionFilePath)
        {
            if (!string.Equals(solutionFilePath, _lastSolutionPath, StringComparison.OrdinalIgnoreCase))
            {
                // check whether the solution actually exist on disk
                var exist = File.Exists(solutionFilePath);

                // cache current result.
                _lastSolutionPath = exist ? solutionFilePath : null;

                return exist;                
            }

            return true;
        }

        private IPersistentStorage GetStorage(Solution solution, string workingFolderPath)
        {
            lock (_primaryStorageAccessLock)
            {
                // first check primary storage, this should cover most of cases
                if (_primaryStorage.TryGetStorage(solution.FilePath, workingFolderPath, out var storage))
                {
                    return storage;
                }

                // okay, go through steps to see whether persistent service should be
                // supported
                var dbFile = GetDatabaseFilePath(workingFolderPath);
                if (!File.Exists(dbFile) && !SolutionSizeAboveThreshold(solution))
                {
                    return NoOpPersistentStorage.Instance;
                }

                // try create new one
                storage = TryCreatePersistentStorage(solution, workingFolderPath);
                if (storage != null)
                {
                    _primaryStorage.EnsureStorage(solution, storage);

                    storage.AddRefUnsafe();
                    return storage;
                }

                return NoOpPersistentStorage.Instance;
            }
        }

        private bool DatabaseSupported(Solution solution, bool checkBranchId)
        {
            if (_testing)
            {
                return true;
            }

            if (solution.FilePath == null)
            {
                return false;
            }

            if (checkBranchId && solution.BranchId != solution.Workspace.PrimaryBranchId)
            {
                // we only use database for primary solution. (Ex, forked solution will not use database)
                return false;
            }

            return true;
        }

        private bool SolutionSizeAboveThreshold(Solution solution)
        {
            if (_testing)
            {
                return true;
            }

            var workspace = solution.Workspace;
            if (workspace.Kind == WorkspaceKind.RemoteWorkspace ||
                workspace.Kind == WorkspaceKind.RemoteTemporaryWorkspace)
            {
                // Storage is always available in the remote server.
                return true;
            }

            if (_solutionSizeTracker == null)
            {
                return false;
            }

            var size = _solutionSizeTracker.GetSolutionSize(solution.Workspace, solution.Id);
            var threshold = this.OptionService.GetOption(StorageOptions.SolutionSizeThreshold);
            return size >= threshold;
        }

        private string GetWorkingFolderPath(Solution solution)
        {
            if (_testing)
            {
                return Path.Combine(Path.GetDirectoryName(solution.FilePath), ".vs", Path.GetFileNameWithoutExtension(solution.FilePath));
            }

            var locationService = solution.Workspace.Services.GetService<IPersistentStorageLocationService>();
            return locationService?.GetStorageLocation(solution);
        }

        private AbstractPersistentStorage TryCreatePersistentStorage(Solution solution, string workingFolderPath)
        {
            // Attempt to create the database up to two times.  The first time we may encounter
            // some sort of issue (like DB corruption).  We'll then try to delete the DB and can
            // try to create it again.  If we can't create it the second time, then there's nothing
            // we can do and we have to store things in memory.
            if (TryCreatePersistentStorage(solution, workingFolderPath, out var persistentStorage) ||
                TryCreatePersistentStorage(solution, workingFolderPath, out persistentStorage))
            {
                return persistentStorage;
            }

            // okay, can't recover, then use no op persistent service 
            // so that things works old way (cache everything in memory)
            return null;
        }

        private bool TryCreatePersistentStorage(
            Solution solution, string workingFolderPath,
            out AbstractPersistentStorage persistentStorage)
        {
            persistentStorage = null;
            AbstractPersistentStorage database = null;

            var databaseFilePath = GetDatabaseFilePath(workingFolderPath);
            try
            {
                if (!TryOpenDatabase(solution, workingFolderPath, databaseFilePath, out database))
                {
                    return false;
                }

                database.Initialize(solution);

                persistentStorage = database;
                return true;
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);

                if (database != null)
                {
                    database.Close();
                }

                if (ShouldDeleteDatabase(ex))
                {
                    // this was not a normal exception that we expected during DB open.
                    // Report this so we can try to address whatever is causing this.
                    FatalError.ReportWithoutCrash(ex);
                    IOUtilities.PerformIO(() => Directory.Delete(Path.GetDirectoryName(databaseFilePath), recursive: true));
                }

                return false;
            }
        }

        protected void Release(AbstractPersistentStorage storage)
        {
            lock (_primaryStorageAccessLock)
            {
                if (storage.ReleaseRefUnsafe())
                {
                    storage.Close();
                }
            }
        }

        public void RegisterPrimarySolution(SolutionId solutionId)
        {
            // don't create database storage file right away. it will be
            // created when first C#/VB project is added
            lock (_primaryStorageAccessLock)
            {
                _primaryStorage.RegisterPrimarySolution(solutionId);
            }
        }

        public void UnregisterPrimarySolution(SolutionId solutionId, bool synchronousShutdown)
        {
            AbstractPersistentStorage storage = null;
            lock (_primaryStorageAccessLock)
            {
                _primaryStorage.UnregisterPrimarySolution(solutionId, out storage);
            }

            ReleaseStorage(storage, synchronousShutdown);
        }

        private static void ReleaseStorage(AbstractPersistentStorage storage, bool synchronousShutdown)
        {
            if (storage == null)
            {
                return;
            }

            if (synchronousShutdown)
            {
                // dispose storage outside of the lock
                storage.Dispose();
            }
            else
            {
                // make it to shutdown asynchronously
                Task.Run(() => storage.Dispose());
            }
        }

        private class PrimaryStorageInfo
        {
            public SolutionId SolutionId { get; private set; }

            public AbstractPersistentStorage Storage { get; private set; }

            public bool TryGetStorage(
                string solutionFilePath,
                string workingFolderPath,
                out AbstractPersistentStorage storage)
            {
                storage = null;

                if (Storage == null)
                {
                    return false;
                }

                if (Storage.SolutionFilePath != solutionFilePath ||
                    Storage.WorkingFolderPath != workingFolderPath)
                {
                    return false;
                }

                storage = Storage;
                storage.AddRefUnsafe();

                return true;
            }

            public void EnsureStorage(Solution solution, AbstractPersistentStorage storage)
            {
                if (Storage != null || solution.Id != SolutionId)
                {
                    return;
                }

                // hold onto the primary solution when it is used the first time.
                Storage = storage;
                storage.AddRefUnsafe();
            }

            public void RegisterPrimarySolution(SolutionId solutionId)
            {
                Contract.ThrowIfNull(solutionId);

                if (SolutionId == solutionId)
                {
                    // this can happen if user opened solution without creating actual file
                    // (ex, "Projects and Solutions" -> "Save new projects when created" option off in VS)
                    // and then later save the solution. then certain host such as VS can call it again
                    // without unregister solution first through IVsSolutionWorkingFoldersEvents
                    return;
                }

                SolutionId = solutionId;
                ReleaseStorage(Storage, synchronousShutdown: false);
            }

            public void UnregisterPrimarySolution(SolutionId solutionId, out AbstractPersistentStorage storage)
            {
                storage = null;
                if (SolutionId == null)
                {
                    // primary solution is never registered or already unregistered
                    Contract.ThrowIfTrue(Storage != null);
                    return;
                }

                Contract.ThrowIfFalse(SolutionId == solutionId);

                storage = Storage;

                SolutionId = null;
                Storage = null;
            }
        }
    }
}
