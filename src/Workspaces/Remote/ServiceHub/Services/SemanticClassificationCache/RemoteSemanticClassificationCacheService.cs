﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteSemanticClassificationCacheService : BrokeredServiceBase, IRemoteSemanticClassificationCacheService
    {
        internal sealed class Factory : FactoryBase<IRemoteSemanticClassificationCacheService>
        {
            protected override IRemoteSemanticClassificationCacheService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteSemanticClassificationCacheService(arguments);
        }

        public RemoteSemanticClassificationCacheService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        /// <summary>
        /// Key we use to look this up in the persistence store for a particular document.
        /// </summary>
        private const string PersistenceName = "<ClassifiedSpans>";

        /// <summary>
        /// Our current persistence version.  If we ever change the on-disk format, this should be changed so that we
        /// skip over persisted data that we cannot read.
        /// </summary>
        private const int ClassificationFormat = 3;

        private const int MaxCachedDocumentCount = 8;

        /// <summary>
        /// Cache of the previously requested classified spans for a particular document.  We use this so that during
        /// loading, if we're asking about the same documents multiple times by the classification service, we can just
        /// return what we have already loaded and not go back to the persistence store to read/decode.
        /// <para/>
        /// This can be read and updated from different threads.  To keep things safe, we use thsi object itself
        /// as the lock that is taken to serialize access.
        /// </summary>
        private readonly LinkedList<(DocumentId id, Checksum checksum, ImmutableArray<ClassifiedSpan> classifiedSpans)> _cachedData
            = new LinkedList<(DocumentId id, Checksum checksum, ImmutableArray<ClassifiedSpan> classifiedSpans)>();

        private static async Task<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
        {
            // We only checksum off of the contents of the file.  During load, we can't really compute any other
            // information since we don't necessarily know about other files, metadata, or dependencies.  So during
            // load, we allow for the previous semantic classifications to be used as long as the file contents match.
            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = checksums.Text;
            return textChecksum;
        }

        public ValueTask CacheSemanticClassificationsAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                // We only get called to cache classifications once we're fully loaded.  At that point there's no need
                // for us to keep around any of the data we cached in-memory during the time the solution was loading.
                lock (_cachedData)
                    _cachedData.Clear();

                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetRequiredDocument(documentId);

                await CacheSemanticClassificationsAsync(document, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        private static async Task CacheSemanticClassificationsAsync(Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var workspace = solution.Workspace;
            var persistenceService = workspace.Services.GetService<IPersistentStorageService>() as IChecksummedPersistentStorageService;
            if (persistenceService == null)
                return;

            using var storage = persistenceService.GetStorage(solution);
            if (storage == null)
                return;

            var classificationService = document.GetLanguageService<IClassificationService>();
            if (classificationService == null)
                return;

            // Don't need to do anything if the information we've persisted matches the checksum of this doc.
            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);
            var persistedChecksum = await storage.ReadChecksumAsync(document, PersistenceName, cancellationToken).ConfigureAwait(false);
            if (checksum == persistedChecksum)
                return;

            var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();
            try
            {
                // Compute classifications for the full span.
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                await classificationService.AddSemanticClassificationsAsync(document, new TextSpan(0, text.Length), classifiedSpans, cancellationToken).ConfigureAwait(false);

                using var stream = SerializableBytes.CreateWritableStream();
                using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
                {
                    WriteTo(classifiedSpans, writer);
                }

                stream.Position = 0;
                await storage.WriteStreamAsync(document, PersistenceName, stream, checksum, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ClassificationUtilities.ReturnClassifiedSpanList(classifiedSpans);
            }
        }

        private static void WriteTo(List<ClassifiedSpan> classifiedSpans, ObjectWriter writer)
        {
            writer.WriteInt32(ClassificationFormat);

            // First, look through all the spans and determine which classification types are used.  For efficiency,
            // we'll emit the unique types up front and then only refer to them by index for all the actual classified
            // spans we emit.

            using var _1 = ArrayBuilder<string>.GetInstance(out var classificationTypes);
            using var _2 = PooledDictionary<string, int>.GetInstance(out var seenClassificationTypes);

            foreach (var classifiedSpan in classifiedSpans)
            {
                var classificationType = classifiedSpan.ClassificationType;
                if (!seenClassificationTypes.ContainsKey(classificationType))
                {
                    seenClassificationTypes.Add(classificationType, classificationTypes.Count);
                    classificationTypes.Add(classificationType);
                }
            }

            writer.WriteInt32(classificationTypes.Count);
            foreach (var type in classificationTypes)
                writer.WriteString(type);

            // Now emit each classified span as a triple of it's start, length, type.
            //
            // In general, the latter two will all be a single byte as tokens tend to be short and we don't have many
            // classification types.
            //
            // We do need to store the start (as opposed to a delta) as we may have multiple items starting at the same
            // position and we cannot encode a negative delta.
            writer.WriteInt32(classifiedSpans.Count);
            foreach (var classifiedSpan in classifiedSpans)
            {
                checked
                {
                    writer.WriteInt32(classifiedSpan.TextSpan.Start);
                    writer.WriteCompressedUInt((uint)classifiedSpan.TextSpan.Length);
                    writer.WriteCompressedUInt((uint)seenClassificationTypes[classifiedSpan.ClassificationType]);
                }
            }
        }

        public ValueTask<SerializableClassifiedSpans?> GetCachedSemanticClassificationsAsync(
            SerializableDocumentKey documentKey, TextSpan textSpan, Checksum checksum, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var classifiedSpans = await TryGetOrReadCachedSemanticClassificationsAsync(
                    documentKey.Rehydrate(), checksum, cancellationToken).ConfigureAwait(false);
                if (classifiedSpans.IsDefault)
                    return null;

                return SerializableClassifiedSpans.Dehydrate(classifiedSpans.WhereAsArray(c => c.TextSpan.IntersectsWith(textSpan)));
            }, cancellationToken);
        }

        private async Task<ImmutableArray<ClassifiedSpan>> TryGetOrReadCachedSemanticClassificationsAsync(
            DocumentKey documentKey,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            // See if we've loaded this into memory first.
            if (TryGetFromInMemoryCache(documentKey, checksum, out var classifiedSpans))
                return classifiedSpans;

            // Otherwise, attempt to read in classifications from persistence store.
            classifiedSpans = await TryReadCachedSemanticClassificationsAsync(
                documentKey, checksum, cancellationToken).ConfigureAwait(false);
            if (classifiedSpans.IsDefault)
                return default;

            UpdateInMemoryCache(documentKey, checksum, classifiedSpans);
            return classifiedSpans;
        }

        private bool TryGetFromInMemoryCache(DocumentKey documentKey, Checksum checksum, out ImmutableArray<ClassifiedSpan> classifiedSpans)
        {
            lock (_cachedData)
            {
                var data = _cachedData.FirstOrNull(d => d.id == documentKey.Id && d.checksum == checksum);
                if (data != null)
                {
                    classifiedSpans = data.Value.classifiedSpans;
                    return true;
                }
            }

            classifiedSpans = default;
            return false;
        }

        private void UpdateInMemoryCache(
            DocumentKey documentKey,
            Checksum checksum,
            ImmutableArray<ClassifiedSpan> classifiedSpans)
        {
            lock (_cachedData)
            {
                // First, remove any existing info for this doc.
                for (var currentNode = _cachedData.First; currentNode != null; currentNode = currentNode.Next)
                {
                    if (currentNode.Value.id == documentKey.Id)
                    {
                        _cachedData.Remove(currentNode);
                        break;
                    }
                }

                // Then place the cached information for this doc at the end.
                _cachedData.AddLast((documentKey.Id, checksum, classifiedSpans));

                // And ensure we don't cache too many docs.
                if (_cachedData.Count > MaxCachedDocumentCount)
                    _cachedData.RemoveFirst();
            }
        }

        private async Task<ImmutableArray<ClassifiedSpan>> TryReadCachedSemanticClassificationsAsync(
            DocumentKey documentKey,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            var workspace = GetWorkspace();
            var persistenceService = workspace.Services.GetService<IPersistentStorageService>() as IChecksummedPersistentStorageService;
            if (persistenceService == null)
                return default;

            using var storage = persistenceService.GetStorage(workspace, documentKey.Project.Solution, checkBranchId: false);
            if (storage == null)
                return default;

            using var stream = await storage.ReadStreamAsync(documentKey, PersistenceName, checksum, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            if (reader == null)
                return default;

            return Read(reader);
        }

        private static ImmutableArray<ClassifiedSpan> Read(ObjectReader reader)
        {
            try
            {
                // if the format doesn't match, we def can't read this.
                if (reader.ReadInt32() != ClassificationFormat)
                    return default;

                // For space efficiency, the unique classification types are emitted in one array up front, and then the
                // specific classification type is referred to by index when emitting the individual spans.
                var classificationTypesCount = reader.ReadInt32();
                using var _1 = ArrayBuilder<string>.GetInstance(classificationTypesCount, out var classificationTypes);

                for (var i = 0; i < classificationTypesCount; i++)
                    classificationTypes.Add(reader.ReadString());

                var classifiedSpanCount = reader.ReadInt32();
                using var _2 = ArrayBuilder<ClassifiedSpan>.GetInstance(classifiedSpanCount, out var classifiedSpans);

                for (var i = 0; i < classifiedSpanCount; i++)
                {
                    checked
                    {
                        var start = reader.ReadInt32();
                        var length = (int)reader.ReadCompressedUInt();
                        var typeIndex = (int)reader.ReadCompressedUInt();

                        classifiedSpans.Add(new ClassifiedSpan(classificationTypes[typeIndex], new TextSpan(start, length)));
                    }
                }

                return classifiedSpans.ToImmutable();
            }
            catch
            {
                // We're reading and interpreting arbitrary data from disk.  This may be invalid for any reason.
                Internal.Log.Logger.Log(FunctionId.RemoteSemanticClassificationCacheService_ExceptionInCacheRead);
                return default;
            }
        }
    }
}
