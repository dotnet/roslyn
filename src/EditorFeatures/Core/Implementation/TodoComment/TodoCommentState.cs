// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    internal partial class TodoCommentIncrementalAnalyzer : IIncrementalAnalyzer
    {
        private class TodoCommentState
        {
            private readonly ConcurrentDictionary<DocumentId, CacheEntry> _dataCache = new ConcurrentDictionary<DocumentId, CacheEntry>(concurrencyLevel: 2, capacity: 10);

            public ImmutableArray<DocumentId> GetDocumentIds()
            {
                return _dataCache.Keys.ToImmutableArrayOrEmpty();
            }

            public async Task<Data> TryGetExistingDataAsync(Document document, CancellationToken cancellationToken)
            {
                if (!_dataCache.TryGetValue(document.Id, out var entry))
                {
                    // we don't have data
                    return default;
                }

                // we have in memory cache for the document
                if (entry.HasCachedData)
                {
                    return entry.Data;
                }

                try
                {
                    using (var stream = await entry.Storage.ReadStreamAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return TryGetExistingData(stream, document, cancellationToken);
                    }
                }
                catch (Exception e) when (IOUtilities.IsNormalIOException(e))
                {
                }

                return default;
            }

            public async Task PersistAsync(Document document, Data data, CancellationToken cancellationToken)
            {
                var id = document.Id;

                // get existing one if there is one
                CacheEntry existing;
                _dataCache.TryGetValue(id, out existing);

                // save data
                var storage = await WriteToStreamAsync(document, data, cancellationToken).ConfigureAwait(false);

                // if data is for opened document or if persistence failed, 
                // we keep small cache so that we don't pay cost of deserialize/serializing data that keep changing
                _dataCache[id] = (storage == null || ShouldCache(document)) ? new CacheEntry(data, storage, GetCount(data)) : new CacheEntry(storage, GetCount(data));

                // let old one go
                existing.Storage?.Dispose();
            }

            private bool ShouldCache(Document value)
            {
                return value.IsOpen();
            }

            private int GetCount(Data data)
            {
                return data.Items.Length;
            }

            private Data TryGetExistingData(Stream stream, Document document, CancellationToken cancellationToken)
            {
                using (var reader = ObjectReader.TryGetReader(stream))
                {
                    if (reader == null)
                    {
                        return null;
                    }

                    var textVersion = VersionStamp.ReadFrom(reader);
                    var dataVersion = VersionStamp.ReadFrom(reader);

                    var list = ArrayBuilder<TodoItem>.GetInstance();
                    AppendItems(reader, document, list, cancellationToken);

                    return new Data(textVersion, dataVersion, list.ToImmutableAndFree());
                }
            }

            private void WriteTo(Stream stream, Data data, CancellationToken cancellationToken)
            {
                using (var writer = new ObjectWriter(stream, cancellationToken: cancellationToken))
                {
                    data.TextVersion.WriteTo(writer);
                    data.SyntaxVersion.WriteTo(writer);

                    writer.WriteInt32(data.Items.Length);

                    foreach (var item in data.Items.OfType<TodoItem>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        writer.WriteInt32(item.Priority);
                        writer.WriteString(item.Message);

                        writer.WriteString(item.OriginalFilePath);
                        writer.WriteInt32(item.OriginalLine);
                        writer.WriteInt32(item.OriginalColumn);

                        writer.WriteString(item.MappedFilePath);
                        writer.WriteInt32(item.MappedLine);
                        writer.WriteInt32(item.MappedColumn);
                    }
                }
            }

            private void AppendItems(ObjectReader reader, Document document, ArrayBuilder<TodoItem> list, CancellationToken cancellationToken)
            {
                var count = reader.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var priority = reader.ReadInt32();
                    var message = reader.ReadString();

                    var originalFile = reader.ReadString();
                    var originalLine = reader.ReadInt32();
                    var originalColumn = reader.ReadInt32();

                    var mappedFile = reader.ReadString();
                    var mappedLine = reader.ReadInt32();
                    var mappedColumn = reader.ReadInt32();

                    list.Add(new TodoItem(
                        priority, message,
                        document.Project.Solution.Workspace, document.Id,
                        mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile));
                }
            }

            public bool Remove(DocumentId id)
            {
                if (this._dataCache.TryRemove(id, out var entry))
                {
                    // let temp storage go away when item is removed.
                    entry.Storage?.Dispose();
                    return true;
                }

                return false;
            }

            private async Task<ITemporaryStreamStorage> WriteToStreamAsync(Document document, Data data, CancellationToken cancellationToken)
            {
                using (var stream = SerializableBytes.CreateWritableStream())
                {
                    WriteTo(stream, data, cancellationToken);
                    stream.Position = 0;

                    var storageService = document.Project.Solution.Workspace.Services.GetService<ITemporaryStorageService>();

                    var storage = storageService.CreateTemporaryStreamStorage(cancellationToken);
                    await storage.WriteStreamAsync(stream, cancellationToken).ConfigureAwait(false);

                    return storage;
                }
            }

            private struct CacheEntry
            {
                public readonly Data Data;
                public readonly int Count;
                public readonly ITemporaryStreamStorage Storage;

                public CacheEntry(Data data, ITemporaryStreamStorage storage, int count)
                {
                    Data = data;
                    Storage = storage;
                    Count = count;
                }

                public CacheEntry(ITemporaryStreamStorage storage, int count)
                {
                    Data = default;
                    Storage = storage;
                    Count = count;
                }

                public bool HasCachedData => !object.Equals(Data, default);
            }
        }
    }
}
