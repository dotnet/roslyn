// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.PooledObjects;
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
                // cache service will be disposed inside VisualStudioCloudCachePersistentStorage.Dispose
                var cacheService = await serviceBroker.GetProxyAsync<ICacheService>(VisualStudioServices.VS2019_9.CacheService, cancellationToken: cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

                Contract.ThrowIfNull(cacheService);
                return new VisualStudioCloudCachePersistentStorage(
                    cacheService, workingFolderPath, solutionKey.FilePath, databaseFilePath);
            }
        }

        private class ProjectCacheContainerKey
        {
            public readonly string SolutionFilePath;
            public readonly string? ProjectFilePath;
            public readonly string ProjectName;
            public readonly Checksum ParseOptionsChecksum;

            /// <summary>
            /// Container key explicitly for the project itself.
            /// </summary>
            public readonly CacheContainerKey? ContainerKey;

            private readonly ConditionalWeakTable<TextDocumentState, StrongBox<CacheContainerKey?>> _documentToContainerKey = new();
            private readonly ConditionalWeakTable<TextDocumentState, StrongBox<CacheContainerKey?>>.CreateValueCallback _documentToContainerKeyCallback;

            public ProjectCacheContainerKey(string solutionFilePath, string? projectFilePath, string projectName, Checksum parseOptionsChecksum)
            {
                SolutionFilePath = solutionFilePath;
                ProjectFilePath = projectFilePath;
                ProjectName = projectName;
                ParseOptionsChecksum = parseOptionsChecksum;

                ContainerKey = CreateProjectContainerKey(solutionFilePath, projectFilePath, projectName, parseOptionsChecksum);

                _documentToContainerKeyCallback = ds => new(CreateDocumentContainerKey(
                    solutionFilePath, projectFilePath, projectName, parseOptionsChecksum, ds.FilePath, ds.Name));
            }

            public CacheContainerKey? GetValue(TextDocumentState state)
                => _documentToContainerKey.GetValue(state, _documentToContainerKeyCallback).Value;

            public static CacheContainerKey? CreateProjectContainerKey(
                string solutionFilePath, string? projectFilePath, string projectName, Checksum? parseOptionsChecksum)
            {
                // Creates a container key for this project.  THe container key is a mix of the project's name, relative
                // file path (to the solution), and optional parse options.

                // If we don't have a valid solution path, we can't store anything.
                if (string.IsNullOrEmpty(solutionFilePath))
                    return null;

                // We have to have a file path for this project
                if (string.IsNullOrEmpty(projectFilePath))
                    return null;

                // The file path has to be relative to the solution path.
                var relativePath = PathUtilities.GetRelativePath(Path.GetDirectoryName(solutionFilePath), projectFilePath!);
                if (relativePath == projectFilePath)
                    return null;

                var dimensions = ImmutableSortedDictionary<string, string?>.Empty
                    .Add(nameof(projectName), projectName)
                    .Add(nameof(projectFilePath), relativePath);

                if (parseOptionsChecksum != null)
                    dimensions = dimensions.Add(nameof(parseOptionsChecksum), parseOptionsChecksum.ToString());

                return new CacheContainerKey("Roslyn.Project", dimensions);
            }

            public static CacheContainerKey? CreateDocumentContainerKey(
                string solutionFilePath,
                string? projectFilePath,
                string projectName,
                Checksum? parseOptionsChecksum,
                string? documentFilePath,
                string documentName)
            {
                // See if we can get a project key for this info.  If not, we def can't get a doc key.
                var projectKey = CreateProjectContainerKey(solutionFilePath, projectFilePath, projectName, parseOptionsChecksum);
                if (projectKey == null)
                    return null;

                // We have to have a file path for this document
                if (string.IsNullOrEmpty(documentFilePath))
                    return null;

                // The file path has to be relative to the solution path.
                var relativePath = PathUtilities.GetRelativePath(Path.GetDirectoryName(solutionFilePath), documentFilePath!);
                if (relativePath == documentFilePath)
                    return null;

                var dimensions = projectKey.Value.Dimensions
                    .Add(nameof(documentFilePath), relativePath)
                    .Add(nameof(documentName), documentName);

                return new CacheContainerKey("Roslyn.Document", dimensions);
            }
        }

        private class VisualStudioCloudCachePersistentStorage : AbstractPersistentStorage
        {
            private static readonly ObjectPool<byte[]> s_byteArrayPool = new(() => new byte[Checksum.HashSize]);
            private static readonly CacheContainerKey s_solutionKey = new("Roslyn.Solution");
            //private static readonly CacheContainerKey s_projectKey = new("Roslyn.Project");
            //private static readonly CacheContainerKey s_documentKey = new("Roslyn.Document");

            private static readonly ConditionalWeakTable<ProjectState, ProjectCacheContainerKey> s_projectToContainerKey = new();

            private readonly ICacheService _cacheService;

            private readonly ConditionalWeakTable<ProjectState, ProjectCacheContainerKey>.CreateValueCallback _projectToContainerKeyCallback;

            public VisualStudioCloudCachePersistentStorage(
                ICacheService cacheService,
                string workingFolderPath,
                string solutionFilePath,
                string databaseFilePath)
                : base(workingFolderPath, solutionFilePath, databaseFilePath)
            {
                _cacheService = cacheService;
                _projectToContainerKeyCallback = ps => new(solutionFilePath, ps.FilePath, ps.Name, ps.GetParseOptionsChecksum(CancellationToken.None));
            }

            public override void Dispose()
            {
                (_cacheService as IDisposable)?.Dispose();
            }

            private CacheContainerKey? GetContainerKey(ProjectKey projectKey, Project? bulkLoadSnapshot)
            {
                return bulkLoadSnapshot != null
                    ? s_projectToContainerKey.GetValue(bulkLoadSnapshot.State, _projectToContainerKeyCallback).ContainerKey
                    : ProjectCacheContainerKey.CreateProjectContainerKey(this.SolutionFilePath, projectKey.FilePath, projectKey.Name, parseOptionsChecksum: null);
            }

            private CacheContainerKey? GetContainerKey(DocumentKey documentKey, Document? bulkLoadSnapshot)
            {
                return bulkLoadSnapshot != null
                    ? s_projectToContainerKey.GetValue(bulkLoadSnapshot.Project.State, _projectToContainerKeyCallback).GetValue(bulkLoadSnapshot.State)
                    : ProjectCacheContainerKey.CreateDocumentContainerKey(this.SolutionFilePath, documentKey.Project.FilePath, documentKey.Project.Name, parseOptionsChecksum: null, documentKey.FilePath, documentKey.Name);
            }

            public override Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken)
                => ChecksumMatchesAsync(name, checksum, s_solutionKey, cancellationToken);

            protected override Task<bool> ChecksumMatchesAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, Checksum checksum, CancellationToken cancellationToken)
                => ChecksumMatchesAsync(name, checksum, GetContainerKey(projectKey, bulkLoadSnapshot), cancellationToken);

            protected override Task<bool> ChecksumMatchesAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, Checksum checksum, CancellationToken cancellationToken)
                => ChecksumMatchesAsync(name, checksum, GetContainerKey(documentKey, bulkLoadSnapshot), cancellationToken);

            private async Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CacheContainerKey? containerKey, CancellationToken cancellationToken)
            {
                if (containerKey == null)
                    return false;

                using var bytes = s_byteArrayPool.GetPooledObject();
                checksum.WriteTo(bytes.Object);

                return await _cacheService.CheckExistsAsync(new CacheItemKey(containerKey.Value, name) { Version = bytes.Object.AsMemory() }, cancellationToken).ConfigureAwait(false);
            }

            public override Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken)
                => ReadStreamAsync(name, checksum, s_solutionKey, cancellationToken);

            protected override Task<Stream?> ReadStreamAsync(ProjectKey projectKey, Project? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken)
                => ReadStreamAsync(name, checksum, GetContainerKey(projectKey, bulkLoadSnapshot), cancellationToken);

            protected override Task<Stream?> ReadStreamAsync(DocumentKey documentKey, Document? bulkLoadSnapshot, string name, Checksum? checksum, CancellationToken cancellationToken)
                => ReadStreamAsync(name, checksum, GetContainerKey(documentKey, bulkLoadSnapshot), cancellationToken);

            private async Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CacheContainerKey? containerKey, CancellationToken cancellationToken)
            {
                if (containerKey == null)
                    return null;

                if (checksum == null)
                {
                    return await ReadStreamAsync(new CacheItemKey(containerKey.Value, name), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using var bytes = s_byteArrayPool.GetPooledObject();
                    checksum.WriteTo(bytes.Object);

                    return await ReadStreamAsync(new CacheItemKey(containerKey.Value, name) { Version = bytes.Object.AsMemory() }, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<Stream?> ReadStreamAsync(CacheItemKey key, CancellationToken cancellationToken)
            {
                var pipe = new Pipe();
                var result = await _cacheService.TryGetItemAsync(key, pipe.Writer, cancellationToken).ConfigureAwait(false);
                if (!result)
                    return null;

                return pipe.Writer.AsStream();
            }

            public override Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
                => WriteStreamAsync(name, stream, checksum, s_solutionKey, cancellationToken);

            public override Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            private async Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CacheContainerKey? containerKey, CancellationToken cancellationToken)
            {
                if (containerKey == null)
                    return false;

                if (checksum == null)
                {
                    return await WriteStreamAsync(new CacheItemKey(containerKey.Value, name), stream, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    using var bytes = s_byteArrayPool.GetPooledObject();
                    checksum.WriteTo(bytes.Object);

                    return await WriteStreamAsync(new CacheItemKey(containerKey.Value, name) { Version = bytes.Object.AsMemory() }, stream, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<bool> WriteStreamAsync(CacheItemKey key, Stream stream, CancellationToken cancellationToken)
            {
                // From: https://dev.azure.com/devdiv/DevDiv/_wiki/wikis/DevDiv.wiki/12664/VS-Cache-Service
                var pipe = new Pipe();
                var addItemTask = _cacheService.SetItemAsync(key, pipe.Reader, shareable: false, cancellationToken);
                try
                {
                    await stream.CopyToAsync(pipe.Writer, cancellationToken).ConfigureAwait(false);
                    await pipe.Writer.CompleteAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await pipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
                    throw;
                }

                await addItemTask.ConfigureAwait(false);
                return true;
            }
        }
    }
}
