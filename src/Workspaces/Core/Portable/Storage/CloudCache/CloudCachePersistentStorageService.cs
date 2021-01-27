// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Storage
{
    internal class CloudCachePersistentStorageService : AbstractPersistentStorageService
    {
        private const string StorageExtension = "CloudCache";
        private readonly ICloudCacheServiceProvider _provider;
        private readonly bool _mustSucceed;

        public CloudCachePersistentStorageService(
            ICloudCacheServiceProvider provider, IPersistentStorageLocationService locationService, bool mustSucceed)
            : base(locationService)
        {
            _provider = provider;
            _mustSucceed = mustSucceed;
        }

        protected override string GetDatabaseFilePath(string workingFolderPath)
        {
            Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(workingFolderPath));
            return Path.Combine(workingFolderPath, StorageExtension);
        }

        protected override bool ShouldDeleteDatabase(Exception exception)
        {
            // CloudCache owns the db, so we don't have to delete anything ourselves.
            return false;
        }

        protected override async ValueTask<IChecksummedPersistentStorage?> TryOpenDatabaseAsync(
            SolutionKey solutionKey, Solution? bulkLoadSnapshot, string workingFolderPath, string databaseFilePath, CancellationToken cancellationToken)
        {
            var cacheService = await _provider.CreateCacheAsync(cancellationToken).ConfigureAwait(false);
            var relativePathBase = await cacheService.GetRelativePathBaseAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(relativePathBase))
            {
                if (_mustSucceed)
                    throw new InvalidOperationException("RelativePathBase was empty");

                return null;
            }

            return new CloudCachePersistentStorage(cacheService, workingFolderPath, relativePathBase, databaseFilePath);
        }
    }
}
