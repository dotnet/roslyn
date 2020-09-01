// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// A service that enables storing and retrieving of information associated with solutions,
    /// projects or documents across runtime sessions.
    /// </summary>
    internal abstract partial class AbstractPersistentStorageService : IChecksummedPersistentStorageService
    {
        private readonly IPersistentStorageLocationService _locationService;

        /// <summary>
        /// This lock guards all mutable fields in this type.
        /// </summary>
        private readonly object _lock = new object();
        private ReferenceCountedDisposable<IChecksummedPersistentStorage>? _currentPersistentStorage;
        private SolutionId? _currentPersistentStorageSolutionId;

        protected AbstractPersistentStorageService(IPersistentStorageLocationService locationService)
            => _locationService = locationService;

        protected abstract string GetDatabaseFilePath(string workingFolderPath);

        /// <summary>
        /// Can throw.  If it does, the caller (<see cref="CreatePersistentStorage"/>) will attempt
        /// to delete the database and retry opening one more time.  If that fails again, the <see
        /// cref="NoOpPersistentStorage"/> instance will be used.
        /// </summary>
        protected abstract IChecksummedPersistentStorage? TryOpenDatabase(SolutionKey solutionKey, Solution? bulkLoadSnapshot, string workingFolderPath, string databaseFilePath);
        protected abstract bool ShouldDeleteDatabase(Exception exception);

        IPersistentStorage IPersistentStorageService.GetStorage(Solution solution)
            => this.GetStorage(solution.Workspace, (SolutionKey)solution, solution, checkBranchId: true);

        IPersistentStorage IPersistentStorageService2.GetStorage(Solution solution, bool checkBranchId)
            => this.GetStorage(solution.Workspace, (SolutionKey)solution, solution, checkBranchId);

        IPersistentStorage IPersistentStorageService2.GetStorage(Workspace workspace, SolutionKey solutionKey, bool checkBranchId)
            => this.GetStorage(workspace, solutionKey, bulkLoadSnapshot: null, checkBranchId);

        public IChecksummedPersistentStorage GetStorage(Solution solution)
            => this.GetStorage(solution.Workspace, (SolutionKey)solution, solution, checkBranchId: true);

        public IChecksummedPersistentStorage GetStorage(Solution solution, bool checkBranchId)
            => this.GetStorage(solution.Workspace, (SolutionKey)solution, solution, checkBranchId);

        public IChecksummedPersistentStorage GetStorage(Workspace workspace, SolutionKey solutionKey, bool checkBranchId)
            => this.GetStorage(workspace, solutionKey, bulkLoadSnapshot: null, checkBranchId);

        public IChecksummedPersistentStorage GetStorage(Workspace workspace, SolutionKey solutionKey, Solution? bulkLoadSnapshot, bool checkBranchId)
        {
            if (!DatabaseSupported(solutionKey, checkBranchId))
            {
                return NoOpPersistentStorage.Instance;
            }

            return GetStorageWorker(workspace, solutionKey, bulkLoadSnapshot);
        }

        internal IChecksummedPersistentStorage GetStorageWorker(Workspace workspace, SolutionKey solutionKey, Solution? bulkLoadSnapshot)
        {
            lock (_lock)
            {
                // Do we already have storage for this?
                if (solutionKey.Id == _currentPersistentStorageSolutionId)
                {
                    // We do, great. Increment our ref count for our caller.  They'll decrement it
                    // when done with it.
                    return PersistentStorageReferenceCountedDisposableWrapper.AddReferenceCountToAndCreateWrapper(_currentPersistentStorage!);
                }

                var workingFolder = TryGetWorkingFolder(workspace, solutionKey, bulkLoadSnapshot);
                if (workingFolder == null)
                    return NoOpPersistentStorage.Instance;

                // If we already had some previous cached service, let's let it start cleaning up
                if (_currentPersistentStorage != null)
                {
                    var storageToDispose = _currentPersistentStorage;

                    // Kick off a task to actually go dispose the previous cached storage instance.
                    // This will remove the single ref count we ourselves added when we cached the
                    // instance.  Then once all other existing clients who are holding onto this
                    // instance let go, it will finally get truly disposed.
                    Task.Run(() => storageToDispose.Dispose());

                    _currentPersistentStorage = null;
                    _currentPersistentStorageSolutionId = null;
                }

                var storage = CreatePersistentStorage(solutionKey, bulkLoadSnapshot, workingFolder);
                Contract.ThrowIfNull(storage);

                // Create and cache a new storage instance associated with this particular solution.
                // It will initially have a ref-count of 1 due to our reference to it.
                _currentPersistentStorage = new ReferenceCountedDisposable<IChecksummedPersistentStorage>(storage);
                _currentPersistentStorageSolutionId = solutionKey.Id;

                // Now increment the reference count and return to our caller.  The current ref
                // count for this instance will be 2.  Until all the callers *and* us decrement
                // the refcounts, this instance will not be actually disposed.
                return PersistentStorageReferenceCountedDisposableWrapper.AddReferenceCountToAndCreateWrapper(_currentPersistentStorage);
            }
        }

        private string? TryGetWorkingFolder(Workspace workspace, SolutionKey solutionKey, Solution? bulkLoadSnapshot)
        {
            // First, see if we have the new API that just operates on a Workspace/Key.  If so, use that.
            if (_locationService is IPersistentStorageLocationService2 locationService2)
                return locationService2.TryGetStorageLocation(workspace, solutionKey);

            // Otherwise, use the existing API.  However, that API only works if we have a full Solution to pass it.
            if (bulkLoadSnapshot == null)
                return null;

            return _locationService.TryGetStorageLocation(bulkLoadSnapshot);
        }

        private static bool DatabaseSupported(SolutionKey solution, bool checkBranchId)
        {
            if (solution.FilePath == null)
            {
                return false;
            }

            if (checkBranchId && !solution.IsPrimaryBranch)
            {
                // we only use database for primary solution. (Ex, forked solution will not use database)
                return false;
            }

            return true;
        }

        private IChecksummedPersistentStorage CreatePersistentStorage(SolutionKey solutionKey, Solution? bulkLoadSnapshot, string workingFolderPath)
        {
            // Attempt to create the database up to two times.  The first time we may encounter
            // some sort of issue (like DB corruption).  We'll then try to delete the DB and can
            // try to create it again.  If we can't create it the second time, then there's nothing
            // we can do and we have to store things in memory.
            return TryCreatePersistentStorage(solutionKey, bulkLoadSnapshot, workingFolderPath) ??
                   TryCreatePersistentStorage(solutionKey, bulkLoadSnapshot, workingFolderPath) ??
                   NoOpPersistentStorage.Instance;
        }

        private IChecksummedPersistentStorage? TryCreatePersistentStorage(
            SolutionKey solutionKey,
            Solution? bulkLoadSnapshot,
            string workingFolderPath)
        {
            var databaseFilePath = GetDatabaseFilePath(workingFolderPath);
            try
            {
                return TryOpenDatabase(solutionKey, bulkLoadSnapshot, workingFolderPath, databaseFilePath);
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

                return null;
            }
        }

        private void Shutdown()
        {
            ReferenceCountedDisposable<IChecksummedPersistentStorage>? storage = null;

            lock (_lock)
            {
                // We will transfer ownership in a thread-safe way out so we can dispose outside the lock
                storage = _currentPersistentStorage;
                _currentPersistentStorage = null;
                _currentPersistentStorageSolutionId = null;
            }

            if (storage != null)
            {
                // Dispose storage outside of the lock. Note this only removes our reference count; clients who are still
                // using this will still be holding a reference count.
                storage.Dispose();
            }
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly AbstractPersistentStorageService _service;

            public TestAccessor(AbstractPersistentStorageService service)
                => _service = service;

            public void Shutdown()
                => _service.Shutdown();
        }

        /// <summary>
        /// A trivial wrapper that we can hand out for instances from the <see cref="AbstractPersistentStorageService"/>
        /// that wraps the underlying <see cref="IPersistentStorage"/> singleton.
        /// </summary>
        private sealed class PersistentStorageReferenceCountedDisposableWrapper : IChecksummedPersistentStorage
        {
            private readonly ReferenceCountedDisposable<IChecksummedPersistentStorage> _storage;

            private PersistentStorageReferenceCountedDisposableWrapper(ReferenceCountedDisposable<IChecksummedPersistentStorage> storage)
                => _storage = storage;

            public static IChecksummedPersistentStorage AddReferenceCountToAndCreateWrapper(ReferenceCountedDisposable<IChecksummedPersistentStorage> storage)
            {
                // This should only be called from a caller that has a non-null storage that it
                // already has a reference on.  So .TryAddReference cannot fail.
                return new PersistentStorageReferenceCountedDisposableWrapper(storage.TryAddReference() ?? throw ExceptionUtilities.Unreachable);
            }

            public void Dispose()
                => _storage.Dispose();

            public Task<Checksum> ReadChecksumAsync(string name, CancellationToken cancellationToken)
                => _storage.Target.ReadChecksumAsync(name, cancellationToken);

            public Task<Checksum> ReadChecksumAsync(Project project, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadChecksumAsync(project, name, cancellationToken);

            public Task<Checksum> ReadChecksumAsync(Document document, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadChecksumAsync(document, name, cancellationToken);

            public Task<Checksum> ReadChecksumAsync(ProjectKey project, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadChecksumAsync(project, name, cancellationToken);

            public Task<Checksum> ReadChecksumAsync(DocumentKey document, string name, CancellationToken cancellationToken)
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

            public Task<Stream> ReadStreamAsync(ProjectKey project, string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(project, name, checksum, cancellationToken);

            public Task<Stream> ReadStreamAsync(DocumentKey document, string name, Checksum checksum, CancellationToken cancellationToken)
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
