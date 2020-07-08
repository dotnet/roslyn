// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Caching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Storage.CloudKernel
{
    internal class CloudKernelPersistentStorageService : IChecksummedPersistentStorageService
    {
        private readonly SolutionCacheFactory _factory;
        private readonly Task<ISolutionCache> _solutionCacheTask;

        public CloudKernelPersistentStorageService()
        {
            _factory = new SolutionCacheFactory();
            _solutionCacheTask = _factory.CreateCacheAsync("https://github.com/dotnet/roslyn", new SolutionCacheOptions { EnableLocalCache = true });
        }

        IPersistentStorage IPersistentStorageService.GetStorage(Solution solution)
            => this.GetStorage(solution);

        IPersistentStorage IPersistentStorageService2.GetStorage(Solution solution, bool checkBranchId)
            => this.GetStorage(solution, checkBranchId);

        public IChecksummedPersistentStorage GetStorage(Solution solution)
            => GetStorage(solution, checkBranchId: true);

        public IChecksummedPersistentStorage GetStorage(Solution solution, bool checkBranchId)
        {
            return new CloudKernelPersistentStorage(solution, _solutionCacheTask);
        }
    }

    internal class CloudKernelPersistentStorage : IChecksummedPersistentStorage
    {
        private readonly Solution _solution;
        private readonly Task<ISolutionCache> _solutionCacheTask;

        public CloudKernelPersistentStorage(Solution solution, Task<ISolutionCache> solutionCacheTask)
        {
            _solution = solution;
            _solutionCacheTask = solutionCacheTask;
        }

        public void Dispose()
        {
        }

        private string GetSolutionKey(string prefix, string suffix)
            => $"solution:{prefix}:{suffix}";

        private static string GetProjectKey(Project project, string prefix, string suffix)
            => $"project:{prefix}:{project.FilePath}:{project.Name}:{suffix}";

        private static string GetDocumentKey(Document document, string prefix, string suffix)
            => $"document:{prefix}:{document.Project.FilePath}:{document.Project.Name}:{document.FilePath}:{document.Name}:{suffix}";

        public Task<Checksum> ReadChecksumAsync(string name, CancellationToken cancellationToken)
            => ReadChecksumWorkerAsync(GetSolutionKey("checksum", name), cancellationToken);

        public Task<Checksum> ReadChecksumAsync(Project project, string name, CancellationToken cancellationToken)
            => ReadChecksumWorkerAsync(GetProjectKey(project, "checksum", name), cancellationToken);

        public Task<Checksum> ReadChecksumAsync(Document document, string name, CancellationToken cancellationToken)
            => ReadChecksumWorkerAsync(GetDocumentKey(document, "checksum", name), cancellationToken);

        private async Task<Checksum> ReadChecksumWorkerAsync(string key, CancellationToken cancellationToken)
        {
            var stream = await ReadStreamWorkerAsync(key, cancellationToken).ConfigureAwait(false);
            if (stream != null)
            {
                using (stream)
                using (var reader = ObjectReader.TryGetReader(stream, leaveOpen: false, cancellationToken))
                {
                    if (reader != null)
                    {
                        return Checksum.ReadFrom(reader);
                    }
                }
            }

            return null;
        }

        public Task<Stream> ReadStreamAsync(string name, Checksum checksum, CancellationToken cancellationToken)
            => ReadStreamWorkerAsync(GetSolutionKey(checksum.ToString(), name), cancellationToken);

        public Task<Stream> ReadStreamAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken)
            => ReadStreamWorkerAsync(GetProjectKey(project, checksum.ToString(), name), cancellationToken);

        public Task<Stream> ReadStreamAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
            => ReadStreamWorkerAsync(GetDocumentKey(document, checksum.ToString(), name), cancellationToken);

        public Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken)
            => ReadStreamWorkerAsync(GetSolutionKey("", name), cancellationToken);

        public Task<Stream> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
            => ReadStreamWorkerAsync(GetProjectKey(project, "", name), cancellationToken);

        public Task<Stream> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
            => ReadStreamWorkerAsync(GetDocumentKey(document, "", name), cancellationToken);

        private async Task<Stream> ReadStreamWorkerAsync(string key, CancellationToken cancellationToken)
        {
            var cache = await _solutionCacheTask.ConfigureAwait(false);
            try
            {
                return await cache.TryGetItemAsync(CalculateHash(key), cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException)
            {
                return null;
            }
        }

        private static string CalculateHash(string key)
        {
            return "a" + HashHelper.CalculateHash(key).Replace("--", "-");
        }

        public async Task<bool> WriteStreamAsync(string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
        {
            return await WriteChecksumStreamWorkerAsync(GetSolutionKey("checksum", name), checksum, cancellationToken).ConfigureAwait(false) &&
                   await WriteStreamWorkerAsync(GetSolutionKey(checksum.ToString(), name), stream, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
        {
            return await WriteChecksumStreamWorkerAsync(GetProjectKey(project, "checksum", name), checksum, cancellationToken).ConfigureAwait(false) &&
                   await WriteStreamWorkerAsync(GetProjectKey(project, checksum.ToString(), name), stream, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum checksum, CancellationToken cancellationToken)
        {
            return await WriteChecksumStreamWorkerAsync(GetDocumentKey(document, "checksum", name), checksum, cancellationToken).ConfigureAwait(false) &&
                   await WriteStreamWorkerAsync(GetDocumentKey(document, checksum.ToString(), name), stream, cancellationToken).ConfigureAwait(false);
        }

        public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
            => WriteStreamWorkerAsync(GetSolutionKey("", name), stream, cancellationToken);

        public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken)
            => WriteStreamWorkerAsync(GetProjectKey(project, "", name), stream, cancellationToken);

        public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken)
            => WriteStreamWorkerAsync(GetDocumentKey(document, "", name), stream, cancellationToken);

        private async Task<bool> WriteStreamWorkerAsync(string key, Stream stream, CancellationToken cancellationToken)
        {
            var cache = await _solutionCacheTask.ConfigureAwait(false);
            return await cache.TryAddItemAsync(CalculateHash(key), stream, CacheStorageTypes.Local, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> WriteChecksumStreamWorkerAsync(string key, Checksum checksum, CancellationToken cancellationToken)
        {
            if (checksum != null)
            {
                using var stream = SerializableBytes.CreateWritableStream();

                using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
                {
                    checksum.WriteTo(writer);
                }

                stream.Position = 0;
                return await WriteStreamWorkerAsync(key, stream, cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
    }
}
