// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler.State
{
    internal abstract class AbstractAnalyzerState<TKey, TValue, TData>
    {
        protected readonly ConcurrentDictionary<TKey, CacheEntry> DataCache = new ConcurrentDictionary<TKey, CacheEntry>(concurrencyLevel: 2, capacity: 10);

        protected abstract TKey GetCacheKey(TValue value);
        protected abstract Solution GetSolution(TValue value);
        protected abstract bool ShouldCache(TValue value);
        protected abstract int GetCount(TData data);

        protected abstract Task<Stream> ReadStreamAsync(IPersistentStorage storage, TValue value, CancellationToken cancellationToken);
        protected abstract TData TryGetExistingData(Stream stream, TValue value, CancellationToken cancellationToken);

        protected abstract void WriteTo(Stream stream, TData data, CancellationToken cancellationToken);
        protected abstract Task<bool> WriteStreamAsync(IPersistentStorage storage, TValue value, Stream stream, CancellationToken cancellationToken);

        public int Count => DataCache.Count;

        public int GetDataCount(TKey key)
        {
            if (!DataCache.TryGetValue(key, out var entry))
            {
                return 0;
            }

            return entry.Count;
        }

        public async Task<TData> TryGetExistingDataAsync(TValue value, CancellationToken cancellationToken)
        {
            if (!DataCache.TryGetValue(GetCacheKey(value), out var entry))
            {
                // we don't have data
                return default;
            }

            // we have in memory cache for the document
            if (entry.HasCachedData)
            {
                return entry.Data;
            }

            // we have persisted data
            var solution = GetSolution(value);
            var persistService = solution.Workspace.Services.GetService<IPersistentStorageService>();

            try
            {
                using var storage = persistService.GetStorage(solution);
                using var stream = await ReadStreamAsync(storage, value, cancellationToken).ConfigureAwait(false);

                if (stream != null)
                {
                    return TryGetExistingData(stream, value, cancellationToken);
                }
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
            }

            return default;
        }

        public async Task PersistAsync(TValue value, TData data, CancellationToken cancellationToken)
        {
            var succeeded = await WriteToStreamAsync(value, data, cancellationToken).ConfigureAwait(false);

            var id = GetCacheKey(value);

            // if data is for opened document or if persistence failed, 
            // we keep small cache so that we don't pay cost of deserialize/serializing data that keep changing
            DataCache[id] = (!succeeded || ShouldCache(value)) ? new CacheEntry(data, GetCount(data)) : new CacheEntry(default, GetCount(data));
        }

        public virtual bool Remove(TKey id)
        {
            // remove doesn't actually remove data from the persistent storage
            // that will be automatically managed by the service itself.
            return DataCache.TryRemove(id, out _);
        }

        private async Task<bool> WriteToStreamAsync(TValue value, TData data, CancellationToken cancellationToken)
        {
            using var stream = SerializableBytes.CreateWritableStream();
            WriteTo(stream, data, cancellationToken);

            var solution = GetSolution(value);
            var persistService = solution.Workspace.Services.GetService<IPersistentStorageService>();

            using var storage = persistService.GetStorage(solution);
            stream.Position = 0;
            return await WriteStreamAsync(storage, value, stream, cancellationToken).ConfigureAwait(false);
        }

        protected readonly struct CacheEntry
        {
            public readonly TData Data;
            public readonly int Count;

            public CacheEntry(TData data, int count)
            {
                Data = data;
                Count = count;
            }

            public bool HasCachedData => !object.Equals(Data, default);
        }
    }
}
