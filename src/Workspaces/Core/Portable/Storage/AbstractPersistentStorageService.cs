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

namespace Microsoft.CodeAnalysis.Storage;

/// <summary>
/// A service that enables storing and retrieving of information associated with solutions,
/// projects or documents across runtime sessions.
/// </summary>
internal abstract partial class AbstractPersistentStorageService(IPersistentStorageConfiguration configuration) : IChecksummedPersistentStorageService
{
    protected readonly IPersistentStorageConfiguration Configuration = configuration;

    private readonly SemaphoreSlim _lock = new(initialCount: 1);
    private IChecksummedPersistentStorage? _currentPersistentStorage;

    protected abstract string GetDatabaseFilePath(string workingFolderPath);

    /// <summary>
    /// Can throw.  If it does, the caller (<see cref="CreatePersistentStorageAsync"/>) will attempt
    /// to delete the database and retry opening one more time.  If that fails again, the <see
    /// cref="NoOpPersistentStorage"/> instance will be used.
    /// </summary>
    protected abstract ValueTask<IChecksummedPersistentStorage?> TryOpenDatabaseAsync(SolutionKey solutionKey, string workingFolderPath, string databaseFilePath, IPersistentStorageFaultInjector? faultInjector, CancellationToken cancellationToken);

    public ValueTask<IChecksummedPersistentStorage> GetStorageAsync(SolutionKey solutionKey, CancellationToken cancellationToken)
        => GetStorageAsync(solutionKey, this.Configuration, faultInjector: null, cancellationToken);

    public async ValueTask<IChecksummedPersistentStorage> GetStorageAsync(
        SolutionKey solutionKey,
        IPersistentStorageConfiguration configuration,
        IPersistentStorageFaultInjector? faultInjector,
        CancellationToken cancellationToken)
    {
        if (solutionKey.FilePath == null)
            return NoOpPersistentStorage.GetOrThrow(solutionKey, Configuration.ThrowOnFailure);

        // Without taking the lock, see if we can lookup a storage for this key.
        var existing = _currentPersistentStorage;
        if (existing?.SolutionKey == solutionKey)
            return existing;

        var workingFolder = configuration.TryGetStorageLocation(solutionKey);
        if (workingFolder == null)
            return NoOpPersistentStorage.GetOrThrow(solutionKey, Configuration.ThrowOnFailure);

        using (await _lock.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            // Recheck if we have a storage for this key after taking the lock.
            if (_currentPersistentStorage?.SolutionKey != solutionKey)
                _currentPersistentStorage = await CreatePersistentStorageAsync(solutionKey, workingFolder, faultInjector, cancellationToken).ConfigureAwait(false);

            return _currentPersistentStorage;
        }
    }

    private async ValueTask<IChecksummedPersistentStorage> CreatePersistentStorageAsync(
        SolutionKey solutionKey, string workingFolderPath, IPersistentStorageFaultInjector? faultInjector, CancellationToken cancellationToken)
    {
        // Attempt to create the database up to two times.  The first time we may encounter
        // some sort of issue (like DB corruption).  We'll then try to delete the DB and can
        // try to create it again.  If we can't create it the second time, then there's nothing
        // we can do and we have to store things in memory.
        var result = await TryCreatePersistentStorageAsync(solutionKey, workingFolderPath, faultInjector, cancellationToken).ConfigureAwait(false) ??
                     await TryCreatePersistentStorageAsync(solutionKey, workingFolderPath, faultInjector, cancellationToken).ConfigureAwait(false);

        if (result != null)
            return result;

        return NoOpPersistentStorage.GetOrThrow(solutionKey, Configuration.ThrowOnFailure);
    }

    private async ValueTask<IChecksummedPersistentStorage?> TryCreatePersistentStorageAsync(
        SolutionKey solutionKey,
        string workingFolderPath,
        IPersistentStorageFaultInjector? faultInjector,
        CancellationToken cancellationToken)
    {
        var databaseFilePath = GetDatabaseFilePath(workingFolderPath);
        try
        {
            return await TryOpenDatabaseAsync(solutionKey, workingFolderPath, databaseFilePath, faultInjector, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (Recover(e))
        {
            return null;
        }

        bool Recover(Exception ex)
        {
            StorageDatabaseLogger.LogException(ex);

            if (Configuration.ThrowOnFailure)
                return false;

            // this was not a normal exception that we expected during DB open.
            // Report this so we can try to address whatever is causing this.
            FatalError.ReportAndCatch(ex);
            IOUtilities.PerformIO(() => Directory.Delete(Path.GetDirectoryName(databaseFilePath)!, recursive: true));

            return true;
        }
    }
}
