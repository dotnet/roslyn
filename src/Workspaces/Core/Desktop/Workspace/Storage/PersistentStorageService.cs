// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Esent;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.Isam.Esent.Interop;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// A service that enables storing and retrieving of information associated with solutions,
    /// projects or documents across runtime sessions.
    /// </summary>
    internal partial class PersistentStorageService : IPersistentStorageService
    {
        private enum StorageProvider
        {
            Esent,
            SQLite,
        }

        /// <summary>
        /// threshold to start to use db (50MB)
        /// </summary>
        private const int SolutionSizeThreshold = 50 * 1024 * 1024;

        internal static readonly IPersistentStorage NoOpPersistentStorageInstance = new NoOpPersistentStorage();

        private readonly IOptionService _optionService;
        private readonly SolutionSizeTracker _solutionSizeTracker;

        private readonly object _lookupAccessLock;
        private readonly Dictionary<string, AbstractPersistentStorage> _lookup;
        private readonly bool _testing;

        private string _lastSolutionPath;

        private SolutionId _primarySolutionId;
        private AbstractPersistentStorage _primarySolutionStorage;

        private StorageProvider _storageProvider = StorageProvider.SQLite;

        public PersistentStorageService(
            IOptionService optionService,
            SolutionSizeTracker solutionSizeTracker)
        {
            _optionService = optionService;
            _solutionSizeTracker = solutionSizeTracker;

            _lookupAccessLock = new object();
            _lookup = new Dictionary<string, AbstractPersistentStorage>();

            _lastSolutionPath = null;

            _primarySolutionId = null;
            _primarySolutionStorage = null;
        }

        public PersistentStorageService(IOptionService optionService, bool testing) : this(optionService)
        {
            _testing = true;
        }

        public PersistentStorageService(IOptionService optionService) : this(optionService, null)
        {
        }

        public IPersistentStorage GetStorage(Solution solution)
        {
            if (!ShouldUseOnDiskStorage(solution))
            {
                return NoOpPersistentStorageInstance;
            }

            // can't use cached information
            if (!string.Equals(solution.FilePath, _lastSolutionPath, StringComparison.OrdinalIgnoreCase))
            {
                // check whether the solution actually exist on disk
                if (!File.Exists(solution.FilePath))
                {
                    return NoOpPersistentStorageInstance;
                }
            }

            // cache current result.
            _lastSolutionPath = solution.FilePath;

            // get working folder path
            var workingFolderPath = GetWorkingFolderPath(solution);
            if (workingFolderPath == null)
            {
                // we don't have place to save db file. don't use db.
                return NoOpPersistentStorageInstance;
            }

            return GetStorage(solution, workingFolderPath);
        }

        private IPersistentStorage GetStorage(Solution solution, string workingFolderPath)
        {
            lock (_lookupAccessLock)
            {
                // see whether we have something we can use
                if (_lookup.TryGetValue(solution.FilePath, out var storage))
                {
                    // previous attempt to create db storage failed.
                    if (storage == null && !SolutionSizeAboveThreshold(solution))
                    {
                        return NoOpPersistentStorageInstance;
                    }

                    // everything seems right, use what we have
                    if (storage?.WorkingFolderPath == workingFolderPath)
                    {
                        storage.AddRefUnsafe();
                        return storage;
                    }
                }

                // either this is the first time, or working folder path has changed.
                // remove existing one
                _lookup.Remove(solution.FilePath);

                var dbFile = _storageProvider == StorageProvider.Esent
                    ? EsentPersistentStorage.GetDatabaseFile(workingFolderPath)
                    : SQLitePersistentStorage.GetDatabaseFile(workingFolderPath);
                if (!File.Exists(dbFile) && !SolutionSizeAboveThreshold(solution))
                {
                    _lookup.Add(solution.FilePath, storage);
                    return NoOpPersistentStorageInstance;
                }

                // try create new one
                storage = TryCreateStorage(workingFolderPath, solution.FilePath);
                _lookup.Add(solution.FilePath, storage);

                if (storage != null)
                {
                    RegisterPrimarySolutionStorageIfNeeded(solution, storage);

                    storage.AddRefUnsafe();
                    return storage;
                }

                return NoOpPersistentStorageInstance;
            }
        }

        private bool ShouldUseOnDiskStorage(Solution solution)
        {
            if (_testing)
            {
                return true;
            }

            // we only use db for primary solution. (Ex, forked solution will not use db)
            if (solution.BranchId != solution.Workspace.PrimaryBranchId || solution.FilePath == null)
            {
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

            if (_solutionSizeTracker == null)
            {
                return false;
            }

            var size = _solutionSizeTracker.GetSolutionSize(solution.Workspace, solution.Id);
            return size > SolutionSizeThreshold;
        }

        private void RegisterPrimarySolutionStorageIfNeeded(Solution solution, AbstractPersistentStorage storage)
        {
            if (_primarySolutionStorage != null || solution.Id != _primarySolutionId)
            {
                return;
            }

            // hold onto the primary solution when it is used the first time.
            _primarySolutionStorage = storage;
            storage.AddRefUnsafe();
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

        private AbstractPersistentStorage TryCreateStorage(string workingFolderPath, string solutionPath)
        {
            if (TryCreateStorage(workingFolderPath, solutionPath, out var storage))
            {
                return storage;
            }

            // first attempt could fail if there was something wrong with existing db.
            // try one more time in case the first attempt fixed the problem.
            if (TryCreateStorage(workingFolderPath, solutionPath, out storage))
            {
                return storage;
            }

            // okay, can't recover, then use no op persistent service 
            // so that things works old way (cache everything in memory)
            return null;
        }

        private bool TryCreateStorage(
            string workingFolderPath, string solutionPath, out AbstractPersistentStorage storage)
        {
            return _storageProvider == StorageProvider.Esent
                ? TryCreateStorage(workingFolderPath, solutionPath, CreateEsentPersistentStorage, out storage)
                : TryCreateStorage(workingFolderPath, solutionPath, CreateSQLitePersistentStorage, out storage);
        }

        private AbstractPersistentStorage CreateEsentPersistentStorage(
            string workingFolderPath, string solutionPath)
        {
            return new EsentPersistentStorage(_optionService, workingFolderPath, solutionPath, this.Release);
        }

        private AbstractPersistentStorage CreateSQLitePersistentStorage(
            string workingFolderPath, string solutionPath)
        {
            return new SQLitePersistentStorage(_optionService, workingFolderPath, solutionPath, this.Release);
        }

        private bool TryCreateStorage(
            string workingFolderPath, string solutionPath,
            Func<string, string, AbstractPersistentStorage> createStorage,
            out AbstractPersistentStorage storage)
        {
            storage = null;
            AbstractPersistentStorage db = null;

            try
            {
                db = createStorage(workingFolderPath, solutionPath);
                db.Initialize();

                storage = db;
                return true;
            }
            catch (EsentAccessDeniedException ex)
            {
                // esent db is already in use by someone.
                if (db != null)
                {
                    db.Close();
                }

                StorageLogger.LogException(ex);

                return false;
            }
            catch (Exception ex)
            {
                if (db != null)
                {
                    db.Close();
                }

                StorageLogger.LogException(ex);
            }

            try
            {
                if (db != null)
                {
                    Directory.Delete(db.DatabaseFileDirectory, recursive: true);
                }
            }
            catch
            {
                // somehow, we couldn't delete the directory.
            }

            return false;
        }

        private void Release(AbstractPersistentStorage storage)
        {
            lock (_lookupAccessLock)
            {
                if (storage.ReleaseRefUnsafe())
                {
                    _lookup.Remove(storage.SolutionFilePath);
                    storage.Close();
                }
            }
        }

        public void RegisterPrimarySolution(SolutionId solutionId)
        {
            // don't create db storage file right away. it will be
            // created when first C#/VB project is added
            lock (_lookupAccessLock)
            {
                Contract.ThrowIfTrue(_primarySolutionStorage != null);

                // just reset solutionId as long as there is no storage has created.
                _primarySolutionId = solutionId;
            }
        }

        public void UnregisterPrimarySolution(SolutionId solutionId, bool synchronousShutdown)
        {
            AbstractPersistentStorage storage = null;
            lock (_lookupAccessLock)
            {
                if (_primarySolutionId == null)
                {
                    // primary solution is never registered or already unregistered
                    Contract.ThrowIfTrue(_primarySolutionStorage != null);
                    return;
                }

                Contract.ThrowIfFalse(_primarySolutionId == solutionId);

                _primarySolutionId = null;
                if (_primarySolutionStorage == null)
                {
                    // primary solution is registered but no C#/VB project was added
                    return;
                }

                storage = _primarySolutionStorage;
                _primarySolutionStorage = null;
            }

            if (storage != null)
            {
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
        }
    }
}
