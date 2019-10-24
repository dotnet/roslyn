// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal abstract partial class AbstractPersistentStorageService : IChecksummedPersistentStorageService
    {
        private readonly IOptionService _optionService;
        private readonly IPersistentStorageLocationService _locationService;
        private readonly ISolutionSizeTracker _solutionSizeTracker;

        /// <summary>
        /// This lock guards all mutable fields in this type.
        /// </summary>
        private readonly object _lock = new object();
        private ReferenceCountedDisposable<IChecksummedPersistentStorage> _currentPersistentStorage;
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
        protected abstract bool TryOpenDatabase(Solution solution, string workingFolderPath, string databaseFilePath, out IChecksummedPersistentStorage storage);
        protected abstract bool ShouldDeleteDatabase(Exception exception);

        IPersistentStorage IPersistentStorageService.GetStorage(Solution solution)
            => this.GetStorage(solution);

        IPersistentStorage IPersistentStorageService2.GetStorage(Solution solution, bool checkBranchId)
            => this.GetStorage(solution, checkBranchId);

        public IChecksummedPersistentStorage GetStorage(Solution solution)
            => GetStorage(solution, checkBranchId: true);

        public IChecksummedPersistentStorage GetStorage(Solution solution, bool checkBranchId)
        {
            if (!DatabaseSupported(solution, checkBranchId))
            {
                return NoOpPersistentStorage.Instance;
            }

            return GetStorageWorker(solution);
        }

        internal IChecksummedPersistentStorage GetStorageWorker(Solution solution)
        {
            lock (_lock)
            {
                // Do we already have storage for this?
                if (solution.Id == _currentPersistentStorageSolutionId)
                {
                    // We do, great
                    return PersistentStorageReferenceCountedDisposableWrapper.AddReferenceCountToAndCreateWrapper(_currentPersistentStorage);
                }

                if (!SolutionSizeAboveThreshold(solution))
                {
                    return NoOpPersistentStorage.Instance;
                }

                var workingFolder = _locationService.TryGetStorageLocation(solution.Id);

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

                return PersistentStorageReferenceCountedDisposableWrapper.AddReferenceCountToAndCreateWrapper(_currentPersistentStorage);
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

        private ReferenceCountedDisposable<IChecksummedPersistentStorage> TryCreatePersistentStorage(Solution solution, string workingFolderPath)
        {
            // Attempt to create the database up to two times.  The first time we may encounter
            // some sort of issue (like DB corruption).  We'll then try to delete the DB and can
            // try to create it again.  If we can't create it the second time, then there's nothing
            // we can do and we have to store things in memory.
            if (TryCreatePersistentStorage(solution, workingFolderPath, out var persistentStorage) ||
                TryCreatePersistentStorage(solution, workingFolderPath, out persistentStorage))
            {
                return new ReferenceCountedDisposable<IChecksummedPersistentStorage>(persistentStorage);
            }

            // okay, can't recover, then use no op persistent service 
            // so that things works old way (cache everything in memory)
            return null;
        }

        private bool TryCreatePersistentStorage(
            Solution solution,
            string workingFolderPath,
            out IChecksummedPersistentStorage persistentStorage)
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
            ReferenceCountedDisposable<IChecksummedPersistentStorage> storage = null;

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
        private sealed class PersistentStorageReferenceCountedDisposableWrapper : IChecksummedPersistentStorage
        {
            private readonly ReferenceCountedDisposable<IChecksummedPersistentStorage> _storage;

            private PersistentStorageReferenceCountedDisposableWrapper(ReferenceCountedDisposable<IChecksummedPersistentStorage> storage)
            {
                _storage = storage;
            }

            public static IChecksummedPersistentStorage AddReferenceCountToAndCreateWrapper(ReferenceCountedDisposable<IChecksummedPersistentStorage> storage)
            {
                return new PersistentStorageReferenceCountedDisposableWrapper(storage.TryAddReference());
            }

            public void Dispose()
            {
                _storage.Dispose();
            }

            public Task<Checksum> ReadChecksumAsync(string name, CancellationToken cancellationToken)
                => _storage.Target.ReadChecksumAsync(name, cancellationToken);

            public Task<Checksum> ReadChecksumAsync(Project project, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadChecksumAsync(project, name, cancellationToken);

            public Task<Checksum> ReadChecksumAsync(Document document, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadChecksumAsync(document, name, cancellationToken);

            public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(name, cancellationToken);

            public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(project, name, cancellationToken);

            public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(document, name, cancellationToken);

            public Task<Stream> ReadStreamAsync(string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(name, checksum, cancellationToken);

            public Task<Stream> ReadStreamAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(project, name, checksum, cancellationToken);

            public Task<Stream> ReadStreamAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(document, name, checksum, cancellationToken);

            public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(name, stream, cancellationToken);

            public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(project, name, stream, cancellationToken);

            public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(document, name, stream, cancellationToken);

            public Task<bool> WriteStreamAsync(string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(name, stream, checksum, cancellationToken);

            public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(project, name, stream, checksum, cancellationToken);

            public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(document, name, stream, checksum, cancellationToken);
        }
    }
}
