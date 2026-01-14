// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.SQLite.v2;

internal sealed class SQLitePersistentStorageService(
    IPersistentStorageConfiguration configuration,
    IAsynchronousOperationListener asyncListener) : AbstractPersistentStorageService(configuration), IWorkspaceService
{
    [ExportWorkspaceServiceFactory(typeof(SQLitePersistentStorageService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class ServiceFactory(
        IAsynchronousOperationListenerProvider asyncOperationListenerProvider) : IWorkspaceServiceFactory
    {
        private readonly IAsynchronousOperationListener _asyncListener = asyncOperationListenerProvider.GetListener(FeatureAttribute.PersistentStorage);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SQLitePersistentStorageService(workspaceServices.GetRequiredService<IPersistentStorageConfiguration>(), _asyncListener);
    }

    private const string StorageExtension = "sqlite3";
    private const string PersistentStorageFileName = "storage.ide";

    private static bool TryInitializeLibraries() => s_initialized.Value;

    private static readonly Lazy<bool> s_initialized = new(TryInitializeLibrariesLazy);

    private static bool TryInitializeLibrariesLazy()
    {
        try
        {
            // Necessary to initialize SQLitePCL.
            SQLitePCL.Batteries_V2.Init();
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            StorageDatabaseLogger.LogException(e);

            // In debug also insta fail here.  That way if there is an issue with sqlite (for example with authoring,
            // or with some particular configuration) that get CI coverage that reveals this.
            Debug.Fail("Sqlite failed to load: " + e);
            return false;
        }

        return true;
    }

    protected override string GetDatabaseFilePath(string workingFolderPath)
    {
        Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
        return Path.Combine(workingFolderPath, StorageExtension, nameof(v2), PersistentStorageFileName);
    }

    protected override async ValueTask<IChecksummedPersistentStorage?> TryOpenDatabaseAsync(
        SolutionKey solutionKey, string workingFolderPath, string databaseFilePath, IPersistentStorageFaultInjector? faultInjector, CancellationToken cancellationToken)
    {
        if (!TryInitializeLibraries())
        {
            // SQLite is not supported on the current platform
            return null;
        }

        if (solutionKey.FilePath == null)
            return NoOpPersistentStorage.GetOrThrow(solutionKey, Configuration.ThrowOnFailure);

        return SQLitePersistentStorage.TryCreate(
            solutionKey,
            workingFolderPath,
            databaseFilePath,
            asyncListener,
            faultInjector);
    }
}
