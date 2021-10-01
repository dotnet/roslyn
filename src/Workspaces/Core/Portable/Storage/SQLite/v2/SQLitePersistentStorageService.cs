// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite.v2
{
    /// <summary>
    /// The storage service is a process wide singleton.  It will ensure that any workspace that
    /// wants to read/write data for a particular solution gets the same DB.  This is important,
    /// we do not partition access to the information about a solution to particular workspaces.
    /// </summary>
    [Export(typeof(SQLitePersistentStorageService)), Shared]
    internal sealed class SQLitePersistentStorageService : AbstractSQLitePersistentStorageService
    {
        [ExportWorkspaceService(typeof(ISQLiteStorageServiceProvider)), Shared]
        internal sealed class Provider : ISQLiteStorageServiceProvider
        {
            public IChecksummedPersistentStorageService Service { get; }

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Provider(SQLitePersistentStorageService service)
                => Service = service;
        }

        private const string StorageExtension = "sqlite3";
        private const string PersistentStorageFileName = "storage.ide";

        private readonly SQLiteConnectionPoolService _connectionPoolService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IPersistentStorageFaultInjector? _faultInjector;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SQLitePersistentStorageService(
            SQLiteConnectionPoolService connectionPoolService,
            IAsynchronousOperationListenerProvider asyncOperationListenerProvider)
        {
            _connectionPoolService = connectionPoolService;
            _asyncListener = asyncOperationListenerProvider.GetListener(FeatureAttribute.PersistentStorage);
        }

#pragma warning disable RS0034
        // exported for testing purposes.
        public SQLitePersistentStorageService(
            SQLiteConnectionPoolService connectionPoolService,
            IAsynchronousOperationListenerProvider asyncOperationListenerProvider,
            IPersistentStorageFaultInjector? faultInjector)
            : this(connectionPoolService, asyncOperationListenerProvider)
        {
            _faultInjector = faultInjector;
        }
#pragma warning restore

        protected override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension, nameof(v2), PersistentStorageFileName);
        }

        protected override ValueTask<IChecksummedPersistentStorage?> TryOpenDatabaseAsync(
            IPersistentStorageConfiguration configuration, SolutionKey solutionKey, string workingFolderPath, string databaseFilePath, CancellationToken cancellationToken)
        {
            if (!TryInitializeLibraries())
            {
                // SQLite is not supported on the current platform
                return new((IChecksummedPersistentStorage?)null);
            }

            if (solutionKey.FilePath == null)
                return new(NoOpPersistentStorage.GetOrThrow(configuration.ThrowOnFailure));

            return new(SQLitePersistentStorage.TryCreate(
                _connectionPoolService,
                workingFolderPath,
                solutionKey.FilePath,
                databaseFilePath,
                _asyncListener,
                _faultInjector));
        }

        // Error occurred when trying to open this DB.  Try to remove it so we can create a good DB.
        protected override bool ShouldDeleteDatabase(Exception exception) => true;
    }
}
