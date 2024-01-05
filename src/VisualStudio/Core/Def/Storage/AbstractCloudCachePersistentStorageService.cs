// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Storage.CloudCache;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    internal abstract class AbstractCloudCachePersistentStorageService : AbstractPersistentStorageService, ICloudCacheStorageService
    {
        private const string StorageExtension = "CloudCache";

        protected AbstractCloudCachePersistentStorageService(IPersistentStorageConfiguration configuration)
            : base(configuration)
        {
        }

        protected abstract ValueTask<ICacheService> CreateCacheServiceAsync(string solutionFolder, CancellationToken cancellationToken);

        protected sealed override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension);
        }

        protected sealed override bool ShouldDeleteDatabase(Exception exception)
        {
            // CloudCache owns the db, so we don't have to delete anything ourselves.
            return false;
        }

        protected sealed override async ValueTask<IChecksummedPersistentStorage?> TryOpenDatabaseAsync(
            SolutionKey solutionKey, string workingFolderPath, string databaseFilePath, CancellationToken cancellationToken)
        {
            var solutionFolder = IOUtilities.PerformIO(() => Path.GetDirectoryName(solutionKey.FilePath));
            if (RoslynString.IsNullOrEmpty(solutionFolder))
                return null;

            var cacheService = await this.CreateCacheServiceAsync(solutionFolder, cancellationToken).ConfigureAwait(false);
            var relativePathBase = await cacheService.GetRelativePathBaseAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(relativePathBase))
                return null;

            return new CloudCachePersistentStorage(cacheService, solutionKey, workingFolderPath, relativePathBase, databaseFilePath);
        }
    }
}
