// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler.State
{
    internal abstract class AbstractAnalyzerState<TKey, TValue, TData>
    {
        protected readonly ConcurrentDictionary<TKey, TData> DataCache = new ConcurrentDictionary<TKey, TData>();

        protected abstract TKey GetCacheKey(TValue value);
        protected abstract Solution GetSolution(TValue value);
        protected abstract bool ShouldCache(TValue value);

        protected abstract Task<Stream> ReadStreamAsync(IPersistentStorage storage, TValue value, CancellationToken cancellationToken);
        protected abstract TData TryGetExistingData(Stream stream, TValue value, CancellationToken cancellationToken);

        protected abstract void WriteTo(Stream stream, TData data, CancellationToken cancellationToken);
        protected abstract Task<bool> WriteStreamAsync(IPersistentStorage storage, TValue value, Stream stream, CancellationToken cancellationToken);

        public async Task<TData> TryGetExistingDataAsync(TValue value, CancellationToken cancellationToken)
        {
            // we have data for the document
            TData data;
            if (this.DataCache.TryGetValue(GetCacheKey(value), out data))
            {
                return data;
            }

            var solution = GetSolution(value);
            var persistService = solution.Workspace.Services.GetService<IPersistentStorageService>();

            using (var storage = persistService.GetStorage(solution))
            using (var stream = await ReadStreamAsync(storage, value, cancellationToken).ConfigureAwait(false))
            {
                if (stream == null)
                {
                    return default(TData);
                }

                return TryGetExistingData(stream, value, cancellationToken);
            }
        }

        public async Task PersistAsync(TValue value, TData data, CancellationToken cancellationToken)
        {
            var succeeded = await WriteToStreamAsync(value, data, cancellationToken).ConfigureAwait(false);

            var id = GetCacheKey(value);

            // if data is for opened document or if persistence failed, 
            // we keep small cache so that we don't pay cost of deserialize/serializing data that keep changing
            if (!succeeded || ShouldCache(value))
            {
                this.DataCache[id] = data;
                return;
            }

            Remove(id);
        }

        public bool Remove(TKey id)
        {
            // remove doesn't actually remove data from the persistent storage
            // that will be automatically managed by the service itself.
            TData data;
            return this.DataCache.TryRemove(id, out data);
        }

        private async Task<bool> WriteToStreamAsync(TValue value, TData data, CancellationToken cancellationToken)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            {
                WriteTo(stream, data, cancellationToken);

                var solution = GetSolution(value);
                var persistService = solution.Workspace.Services.GetService<IPersistentStorageService>();

                using (var storage = persistService.GetStorage(solution))
                {
                    stream.Position = 0;
                    return await WriteStreamAsync(storage, value, stream, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
