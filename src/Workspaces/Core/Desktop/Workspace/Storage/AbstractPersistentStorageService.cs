﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private readonly IOptionService _optionService;
        private readonly IPersistentStorageLocationService _locationService;
        private readonly ISolutionSizeTracker _solutionSizeTracker;

        /// <summary>
        /// This lock guards all mutable fields in this type.
        /// </summary>
        private readonly object _lock = new object();
        private ReferenceCountedDisposable<IPersistentStorage> _currentPersistentStorage;
        private SolutionId _currentPersistentStorageSolutionId;
        private bool _subscribedToLocationServiceChangeEvents;

        protected AbstractPersistentStorageService(
            IOptionService optionService,
            IPersistentStorageLocationService locationService,
            ISolutionSizeTracker solutionSizeTracker)
        {
            _optionService = optionService;
            _locationService = locationService;
            _solutionSizeTracker = solutionSizeTracker;
        }

        protected abstract string GetDatabaseFilePath(string workingFolderPath);
        protected abstract bool TryOpenDatabase(Solution solution, string workingFolderPath, string databaseFilePath, out IPersistentStorage storage);
        protected abstract bool ShouldDeleteDatabase(Exception exception);

        public IPersistentStorage GetStorage(Solution solution)
            => GetStorage(solution, checkBranchId: true);

        public IPersistentStorage GetStorage(Solution solution, bool checkBranchId)
        {
            if (!DatabaseSupported(solution, checkBranchId))
            {
                return NoOpPersistentStorage.Instance;
            }

            lock (_lock)
            {
                // Do we already have storage for this?
                if (solution.Id == _currentPersistentStorageSolutionId)
                {
                    // We do, great
                    return PersistentStorageReferenceCountedDisposableWrapper.AddRefrenceCountToAndCreateWrapper(_currentPersistentStorage);
                }

                if (!SolutionSizeAboveThreshold(solution))
                {
                    return NoOpPersistentStorage.Instance;
                }

                string workingFolder = _locationService.TryGetStorageLocation(solution.Id);

                if (workingFolder == null)
                {
                    return NoOpPersistentStorage.Instance;
                }

                if (!_subscribedToLocationServiceChangeEvents)
                {
                    _locationService.StorageLocationChanging += LocationServiceStorageLocationChanging;
                    _subscribedToLocationServiceChangeEvents = true;
                }

                // If we already had some previous cached service, let's let it start cleaning up
                if (_currentPersistentStorage != null)
                {
                    var storageToDispose = _currentPersistentStorage;

                    Task.Run(() => storageToDispose.Dispose());

                    _currentPersistentStorage = null;
                    _currentPersistentStorageSolutionId = null;
                }

                _currentPersistentStorage = TryCreatePersistentStorage(solution, workingFolder);

                if (_currentPersistentStorage == null)
                {
                    return NoOpPersistentStorage.Instance;
                }

                _currentPersistentStorageSolutionId = solution.Id;

                return PersistentStorageReferenceCountedDisposableWrapper.AddRefrenceCountToAndCreateWrapper(_currentPersistentStorage);
            }
        }

        private bool DatabaseSupported(Solution solution, bool checkBranchId)
        {
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
            var threshold = this._optionService.GetOption(StorageOptions.SolutionSizeThreshold);
            return size >= threshold;
        }

        private ReferenceCountedDisposable<IPersistentStorage> TryCreatePersistentStorage(Solution solution, string workingFolderPath)
        {
            // Attempt to create the database up to two times.  The first time we may encounter
            // some sort of issue (like DB corruption).  We'll then try to delete the DB and can
            // try to create it again.  If we can't create it the second time, then there's nothing
            // we can do and we have to store things in memory.
            if (TryCreatePersistentStorage(solution, workingFolderPath, out var persistentStorage) ||
                TryCreatePersistentStorage(solution, workingFolderPath, out persistentStorage))
            {
                return new ReferenceCountedDisposable<IPersistentStorage>(persistentStorage);
            }

            // okay, can't recover, then use no op persistent service 
            // so that things works old way (cache everything in memory)
            return null;
        }

        private bool TryCreatePersistentStorage(
            Solution solution,
            string workingFolderPath,
            out IPersistentStorage persistentStorage)
        {
            persistentStorage = null;

            var databaseFilePath = GetDatabaseFilePath(workingFolderPath);
            try
            {
                if (!TryOpenDatabase(solution, workingFolderPath, databaseFilePath, out persistentStorage))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);

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

        private void LocationServiceStorageLocationChanging(object sender, PersistentStorageLocationChangingEventArgs e)
        {
            ReferenceCountedDisposable<IPersistentStorage> storage = null;

            lock (_lock)
            {
                if (e.SolutionId != _currentPersistentStorageSolutionId)
                {
                    return;
                }

                // We will transfer ownership in a thread-safe way out so we can dispose outside the lock
                storage = _currentPersistentStorage;
                _currentPersistentStorage = null;
                _currentPersistentStorageSolutionId = null;
            }

            if (storage != null)
            {
                if (e.MustUseNewStorageLocationImmediately)
                {
                    // Dispose storage outside of the lock. Note this only removes our reference count; clients who are still
                    // using this will still be holding a reference count.
                    storage.Dispose();
                }
                else
                {
                    // make it to shutdown asynchronously
                    Task.Run(() => storage.Dispose());
                }
            }
        }

        /// <summary>
        /// A trivial wrapper that we can hand out for instances from the <see cref="AbstractPersistentStorageService"/>
        /// that wraps the underlying <see cref="IPersistentStorage"/> singleton.
        /// </summary>
        private sealed class PersistentStorageReferenceCountedDisposableWrapper : IPersistentStorage
        {
            private readonly ReferenceCountedDisposable<IPersistentStorage> _storage;

            private PersistentStorageReferenceCountedDisposableWrapper(ReferenceCountedDisposable<IPersistentStorage> storage)
            {
                _storage = storage;
            }
            
            public static IPersistentStorage AddRefrenceCountToAndCreateWrapper(ReferenceCountedDisposable<IPersistentStorage> storage)
            {
                return new PersistentStorageReferenceCountedDisposableWrapper(storage.TryAddReference());
            }

            public void Dispose()
            {
                _storage.Dispose();
            }

            public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken = default)
                => _storage.Target.ReadStreamAsync(name, cancellationToken);

            public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken = default)
                => _storage.Target.ReadStreamAsync(project, name, cancellationToken);

            public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken = default)
                => _storage.Target.ReadStreamAsync(document, name, cancellationToken);

            public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken = default)
                => _storage.Target.WriteStreamAsync(name, stream, cancellationToken);

            public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken = default)
                => _storage.Target.WriteStreamAsync(project, name, stream, cancellationToken);

            public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken = default)
                => _storage.Target.WriteStreamAsync(document, name, stream, cancellationToken);
        }
    }
}
