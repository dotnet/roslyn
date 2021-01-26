// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.VisualStudio.RpcContracts.Caching;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    internal partial class VisualStudioCloudCachePersistentStorageServiceFactory
    {
        private class VisualStudioCloudCachePersistentStorageService : AbstractPersistentStorageService, ICloudCacheStorageService
        {
            private const string StorageExtension = "CloudCache";
            private readonly IAsyncServiceProvider _serviceProvider;

            public VisualStudioCloudCachePersistentStorageService(
                IAsyncServiceProvider serviceProvider, IPersistentStorageLocationService locationService)
                : base(locationService)
            {
                _serviceProvider = serviceProvider;
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
                var serviceContainer = await _serviceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>().ConfigureAwait(false);
                var serviceBroker = serviceContainer.GetFullAccessServiceBroker();

#pragma warning disable ISB001 // Dispose of proxies
                var cacheService = await serviceBroker.GetProxyAsync<ICacheService>(VisualStudioServices.VS2019_9.CacheService, cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

                Contract.ThrowIfNull(cacheService);
                return new VisualStudioCloudCachePersistentStorage(cacheService);
            }
        }

        private class VisualStudioCloudCachePersistentStorage : AbstractPersistentStorage
        {
            private readonly ICacheService _cacheService;
            private readonly CacheContainerKey _solutionKey = new CacheContainerKey();

            public VisualStudioCloudCachePersistentStorage(ICacheService cacheService) : base()
            {
                _cacheService = cacheService;
            }

            public override void Dispose()
            {
                (_cacheService as IDisposable)?.Dispose();
            }

            public override Task<Checksum?> ReadChecksumAsync(string name, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            protected override async Task<Checksum?> ReadChecksumAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, CancellationToken cancellationToken)
            {
                if (bulkLoadSnapshot == null)
                    return null;

                throw new NotImplementedException();
            }

            protected override Task<Checksum?> ReadChecksumAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, CancellationToken cancellationToken)
            {
                if (bulkLoadSnapshot == null)
                    return null;

                throw new NotImplementedException();
            }

            public override Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            protected override Task<Stream?> ReadStreamAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            protected override Task<Stream?> ReadStreamAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
