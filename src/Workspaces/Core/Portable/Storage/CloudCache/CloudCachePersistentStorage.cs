// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Storage
{
    /// <summary>
    /// Implementation of Roslyn's <see cref="IPersistentStorage"/> sitting on top of the platform's cloud storage
    /// system.
    /// </summary>
    internal class CloudCachePersistentStorage : AbstractPersistentStorage
    {
        private static readonly ObjectPool<byte[]> s_byteArrayPool = new(() => new byte[Checksum.HashSize]);

        /// <remarks>
        /// We do not need to store anything specific about the solution in this key as the platform cloud cache is
        /// already keyed to the current solution.  So this just allows us to store values considering that as the root.
        /// </remarks>
        private static readonly RoslynCloudCacheContainerKey s_solutionKey = new("Roslyn.Solution");

        /// <summary>
        /// Cache from project green nodes to the container keys we've computed for it (and the documents inside of it).
        /// We can avoid computing these container keys when called repeatedly for the same projects/documents.
        /// </summary>
        private static readonly ConditionalWeakTable<ProjectState, ContainerKeyCache> s_projectToContainerKey = new();
        private readonly ConditionalWeakTable<ProjectState, ContainerKeyCache>.CreateValueCallback _projectToContainerKeyCallback;

        /// <summary>
        /// Underlying cache service (owned by platform team) responsible for actual storage and retrieval of data.
        /// </summary>
        private readonly IRoslynCloudCacheService _cacheService;

        public CloudCachePersistentStorage(
            SolutionKey solutionKey,
            IRoslynCloudCacheService cacheService,
            string workingFolderPath,
            string relativePathBase,
            string databaseFilePath)
            : base(workingFolderPath, relativePathBase, databaseFilePath)
        {
            _cacheService = cacheService;
            _projectToContainerKeyCallback = ps => new(relativePathBase, ProjectKey.ToProjectKey(solutionKey, ps));
        }

        public override void Dispose()
            => _cacheService.Dispose();

        public override ValueTask DisposeAsync()
            => _cacheService.DisposeAsync();

        /// <summary>
        /// Maps our own roslyn key to the appropriate key to use for the cloud cache system.  To avoid lots of
        /// allocations we cache these (weakly) so if the same keys are used we can use the same platform keys.
        /// </summary>
        private RoslynCloudCacheContainerKey? GetContainerKey(ProjectKey projectKey)
        {
            var state = projectKey.ProjectState;
            return state != null
                ? s_projectToContainerKey.GetValue(state, _projectToContainerKeyCallback).ContainerKey
                : ContainerKeyCache.CreateProjectContainerKey(this.SolutionFilePath, projectKey);
        }

        /// <summary>
        /// Maps our own roslyn key to the appropriate key to use for the cloud cache system.  To avoid lots of
        /// allocations we cache these (weakly) so if the same keys are used we can use the same platform keys.
        /// </summary>
        private RoslynCloudCacheContainerKey? GetContainerKey(DocumentKey documentKey)
        {
            var projectState = documentKey.Project.ProjectState;
            var documentState = documentKey.DocumentState;
            return projectState != null && documentState != null
                ? s_projectToContainerKey.GetValue(projectState, _projectToContainerKeyCallback).GetValue(documentState)
                : ContainerKeyCache.CreateDocumentContainerKey(this.SolutionFilePath, documentKey);
        }

        public override Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken)
            => ChecksumMatchesAsync(name, checksum, s_solutionKey, cancellationToken);

        public override Task<bool> ChecksumMatchesAsync(ProjectKey projectKey, string name, Checksum checksum, CancellationToken cancellationToken)
            => ChecksumMatchesAsync(name, checksum, GetContainerKey(projectKey), cancellationToken);

        public override Task<bool> ChecksumMatchesAsync(DocumentKey documentKey, string name, Checksum checksum, CancellationToken cancellationToken)
            => ChecksumMatchesAsync(name, checksum, GetContainerKey(documentKey), cancellationToken);

        private async Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, RoslynCloudCacheContainerKey? containerKey, CancellationToken cancellationToken)
        {
            // If we failed to get a container key (for example, because the client is referencing a file not under the
            // solution folder) then we can't proceed.
            if (containerKey == null)
                return false;

            using var bytes = s_byteArrayPool.GetPooledObject();
            checksum.WriteTo(bytes.Object);

            return await _cacheService.CheckExistsAsync(new RoslynCloudCacheItemKey(containerKey.Value, name, bytes.Object), cancellationToken).ConfigureAwait(false);
        }

        public override Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken)
            => ReadStreamAsync(name, checksum, s_solutionKey, cancellationToken);

        public override Task<Stream?> ReadStreamAsync(ProjectKey projectKey, string name, Checksum? checksum, CancellationToken cancellationToken)
            => ReadStreamAsync(name, checksum, GetContainerKey(projectKey), cancellationToken);

        public override Task<Stream?> ReadStreamAsync(DocumentKey documentKey, string name, Checksum? checksum, CancellationToken cancellationToken)
            => ReadStreamAsync(name, checksum, GetContainerKey(documentKey), cancellationToken);

        private async Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, RoslynCloudCacheContainerKey? containerKey, CancellationToken cancellationToken)
        {
            // If we failed to get a container key (for example, because teh client is referencing a file not under the
            // solution folder) then we can't proceed.
            if (containerKey == null)
                return null;

            if (checksum == null)
            {
                return await ReadStreamAsync(new RoslynCloudCacheItemKey(containerKey.Value, name), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using var bytes = s_byteArrayPool.GetPooledObject();
                checksum.WriteTo(bytes.Object);

                return await ReadStreamAsync(new RoslynCloudCacheItemKey(containerKey.Value, name, bytes.Object), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<Stream?> ReadStreamAsync(RoslynCloudCacheItemKey key, CancellationToken cancellationToken)
        {
            var pipe = new Pipe();
            var result = await _cacheService.TryGetItemAsync(key, pipe.Writer, cancellationToken).ConfigureAwait(false);
            if (!result)
                return null;

            // Clients will end up doing blocking reads on the synchronous stream we return from this.  This can
            // negatively impact our calls as that will cause sync blocking on the async work to fill the pipe.  To
            // alleviate that issue, we actually asynchronously read in the entire stream into memory inside the reader
            // and then pass that out.  This should not be a problem in practice as PipeReader internally intelligently
            // uses and pools reasonable sized buffers, preventing us from exacerbating the GC or causing LOH
            // allocations.
            while (true)
            {
                var readResult = await pipe.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                pipe.Reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

                if (readResult.IsCompleted)
                    break;
            }

            return pipe.Reader.AsStream();
        }

        public override Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => WriteStreamAsync(name, stream, checksum, s_solutionKey, cancellationToken);

        public override Task<bool> WriteStreamAsync(ProjectKey projectKey, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => WriteStreamAsync(name, stream, checksum, GetContainerKey(projectKey), cancellationToken);

        public override Task<bool> WriteStreamAsync(DocumentKey documentKey, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
            => WriteStreamAsync(name, stream, checksum, GetContainerKey(documentKey), cancellationToken);

        private async Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, RoslynCloudCacheContainerKey? containerKey, CancellationToken cancellationToken)
        {
            // If we failed to get a container key (for example, because teh client is referencing a file not under the
            // solution folder) then we can't proceed.
            if (containerKey == null)
                return false;

            if (checksum == null)
            {
                return await WriteStreamAsync(new RoslynCloudCacheItemKey(containerKey.Value, name), stream, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using var bytes = s_byteArrayPool.GetPooledObject();
                checksum.WriteTo(bytes.Object);

                return await WriteStreamAsync(new RoslynCloudCacheItemKey(containerKey.Value, name, bytes.Object), stream, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<bool> WriteStreamAsync(RoslynCloudCacheItemKey key, Stream stream, CancellationToken cancellationToken)
        {
            await _cacheService.SetItemAsync(key, PipeReader.Create(stream), shareable: false, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
