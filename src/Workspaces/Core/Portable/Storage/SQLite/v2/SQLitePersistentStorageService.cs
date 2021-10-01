// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
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
    internal sealed class SQLitePersistentStorageService : AbstractSQLitePersistentStorageService
    {
        [ExportWorkspaceService(typeof(ISQLiteStorageServiceFactory)), Shared]
        internal sealed class Factory : ISQLiteStorageServiceFactory
        {
            private readonly IChecksummedPersistentStorageService _instance;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory(
                SQLiteConnectionPoolService connectionPoolService,
                IAsynchronousOperationListenerProvider asyncOperationListenerProvider)
            {
                _instance = new SQLitePersistentStorageService(
                    connectionPoolService,
                    asyncOperationListenerProvider.GetListener(FeatureAttribute.PersistentStorage));
            }

            public IChecksummedPersistentStorageService Create()
                => _instance;
        }

        private const string StorageExtension = "sqlite3";
        private const string PersistentStorageFileName = "storage.ide";

        private readonly SQLiteConnectionPoolService _connectionPoolService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IPersistentStorageFaultInjector? _faultInjector;

        public SQLitePersistentStorageService(
            SQLiteConnectionPoolService connectionPoolService,
            IAsynchronousOperationListener asyncListener)
        {
            _connectionPoolService = connectionPoolService;
            _asyncListener = asyncListener;
        }

        public SQLitePersistentStorageService(
            SQLiteConnectionPoolService connectionPoolService,
            IAsynchronousOperationListener asyncListener,
            IPersistentStorageFaultInjector? faultInjector)
            : this(connectionPoolService, asyncListener)
        {
            _faultInjector = faultInjector;
        }

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
