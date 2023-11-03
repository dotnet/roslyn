// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
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
        protected readonly IPersistentStorageConfiguration Configuration;

        /// <summary>
        /// This lock guards all mutable fields in this type.
        /// </summary>
        private readonly SemaphoreSlim _lock = new(initialCount: 1);
        private ReferenceCountedDisposable<IChecksummedPersistentStorage>? _currentPersistentStorage;
        private SolutionId? _currentPersistentStorageSolutionId;

        protected AbstractPersistentStorageService(IPersistentStorageConfiguration configuration)
            => Configuration = configuration;

        protected abstract string GetDatabaseFilePath(string workingFolderPath);

        /// <summary>
        /// Can throw.  If it does, the caller (<see cref="CreatePersistentStorageAsync"/>) will attempt
        /// to delete the database and retry opening one more time.  If that fails again, the <see
        /// cref="NoOpPersistentStorage"/> instance will be used.
        /// </summary>
        protected abstract ValueTask<IChecksummedPersistentStorage?> TryOpenDatabaseAsync(SolutionKey solutionKey, string workingFolderPath, string databaseFilePath, CancellationToken cancellationToken);
        protected abstract bool ShouldDeleteDatabase(Exception exception);

        public ValueTask<IChecksummedPersistentStorage> GetStorageAsync(SolutionKey solutionKey, CancellationToken cancellationToken)
        {
            return solutionKey.FilePath == null
                ? new(NoOpPersistentStorage.GetOrThrow(Configuration.ThrowOnFailure))
                : GetStorageWorkerAsync(solutionKey, cancellationToken);
        }

        internal async ValueTask<IChecksummedPersistentStorage> GetStorageWorkerAsync(SolutionKey solutionKey, CancellationToken cancellationToken)
        {
            using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Do we already have storage for this?
                if (solutionKey.Id == _currentPersistentStorageSolutionId)
                {
                    // We do, great. Increment our ref count for our caller.  They'll decrement it
                    // when done with it.
                    return PersistentStorageReferenceCountedDisposableWrapper.AddReferenceCountToAndCreateWrapper(_currentPersistentStorage!);
                }

                var workingFolder = Configuration.TryGetStorageLocation(solutionKey);
                if (workingFolder == null)
                    return NoOpPersistentStorage.GetOrThrow(Configuration.ThrowOnFailure);

                // If we already had some previous cached service, let's let it start cleaning up
                if (_currentPersistentStorage != null)
                {
                    var storageToDispose = _currentPersistentStorage;

                    // Kick off a task to actually go dispose the previous cached storage instance.
                    // This will remove the single ref count we ourselves added when we cached the
                    // instance.  Then once all other existing clients who are holding onto this
                    // instance let go, it will finally get truly disposed.
                    // This operation is not safe to cancel (as dispose must happen).
                    _ = Task.Run(storageToDispose.Dispose, CancellationToken.None);

                    _currentPersistentStorage = null;
                    _currentPersistentStorageSolutionId = null;
                }

                var storage = await CreatePersistentStorageAsync(solutionKey, workingFolder, cancellationToken).ConfigureAwait(false);
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

        private async ValueTask<IChecksummedPersistentStorage> CreatePersistentStorageAsync(
            SolutionKey solutionKey, string workingFolderPath, CancellationToken cancellationToken)
        {
            // Attempt to create the database up to two times.  The first time we may encounter
            // some sort of issue (like DB corruption).  We'll then try to delete the DB and can
            // try to create it again.  If we can't create it the second time, then there's nothing
            // we can do and we have to store things in memory.
            var result = await TryCreatePersistentStorageAsync(solutionKey, workingFolderPath, cancellationToken).ConfigureAwait(false) ??
                         await TryCreatePersistentStorageAsync(solutionKey, workingFolderPath, cancellationToken).ConfigureAwait(false);

            if (result != null)
                return result;

            return NoOpPersistentStorage.GetOrThrow(Configuration.ThrowOnFailure);
        }

        private async ValueTask<IChecksummedPersistentStorage?> TryCreatePersistentStorageAsync(
            SolutionKey solutionKey,
            string workingFolderPath,
            CancellationToken cancellationToken)
        {
            var databaseFilePath = GetDatabaseFilePath(workingFolderPath);
            try
            {
                return await TryOpenDatabaseAsync(solutionKey, workingFolderPath, databaseFilePath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (Recover(e))
            {
                return null;
            }

            bool Recover(Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);

                if (Configuration.ThrowOnFailure)
                {
                    return false;
                }

                if (ShouldDeleteDatabase(ex))
                {
                    // this was not a normal exception that we expected during DB open.
                    // Report this so we can try to address whatever is causing this.
                    FatalError.ReportAndCatch(ex);
                    IOUtilities.PerformIO(() => Directory.Delete(Path.GetDirectoryName(databaseFilePath)!, recursive: true));
                }

                return true;
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

            // Dispose storage outside of the lock. Note this only removes our reference count; clients who are still
            // using this will still be holding a reference count.
            storage?.Dispose();
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor(AbstractPersistentStorageService service)
        {
            public void Shutdown()
                => service.Shutdown();
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
                return new PersistentStorageReferenceCountedDisposableWrapper(storage.TryAddReference() ?? throw ExceptionUtilities.Unreachable());
            }

            public void Dispose()
                => _storage.Dispose();

            public ValueTask DisposeAsync()
                => _storage.DisposeAsync();

            public Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ChecksumMatchesAsync(name, checksum, cancellationToken);

            public Task<bool> ChecksumMatchesAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ChecksumMatchesAsync(project, name, checksum, cancellationToken);

            public Task<bool> ChecksumMatchesAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ChecksumMatchesAsync(document, name, checksum, cancellationToken);

            public Task<bool> ChecksumMatchesAsync(ProjectKey project, string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ChecksumMatchesAsync(project, name, checksum, cancellationToken);

            public Task<bool> ChecksumMatchesAsync(DocumentKey document, string name, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.ChecksumMatchesAsync(document, name, checksum, cancellationToken);

            public Task<Stream?> ReadStreamAsync(string name, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(name, cancellationToken);

            public Task<Stream?> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
                => _storage.Target.ReadStreamAsync(project, name, cancellationToken);

            public Task<Stream?> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
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

            public Task<bool> WriteStreamAsync(ProjectKey projectKey, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(projectKey, name, stream, checksum, cancellationToken);

            public Task<bool> WriteStreamAsync(DocumentKey documentKey, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
                => _storage.Target.WriteStreamAsync(documentKey, name, stream, checksum, cancellationToken);
        }
    }
}
