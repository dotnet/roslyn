// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteSemanticClassificationService : BrokeredServiceBase, IRemoteSemanticClassificationService
    {
        /// <summary>
        /// Key we use to look this up in the persistence store for a particular document.
        /// </summary>
        private const string s_semanticPersistenceName = "<SemanticClassifiedSpans>";
        private const string s_embeddedLanguagePersistenceName = "<EmbeddedLanguageClassifiedSpans>";

        /// <summary>
        /// Our current persistence version.  If we ever change the on-disk format, this should be changed so that we
        /// skip over persisted data that we cannot read.
        /// </summary>
        private const int ClassificationFormat = 4;

        private const int MaxCachedDocumentCount = 8;

        /// <summary>
        /// Cache of the previously requested classified spans for a particular document.  We use this so that during
        /// loading, if we're asking about the same documents multiple times by the classification service, we can just
        /// return what we have already loaded and not go back to the persistence store to read/decode.
        /// <para/>
        /// This can be read and updated from different threads.  To keep things safe, we use this object itself
        /// as the lock that is taken to serialize access.
        /// </summary>
        private readonly LinkedList<(DocumentId id, ClassificationType type, Checksum checksum, ImmutableArray<ClassifiedSpan> classifiedSpans)> _cachedData = new();

        /// <summary>
        /// Queue where we place documents we want to compute and cache full semantic classifications for.  Note: the
        /// same document may appear multiple times inside of this queue (for different versions of the document).
        /// However, we'll only process the last version of any document added.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<(Document, ClassificationType type, ClassificationOptions)> _workQueue;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public RemoteSemanticClassificationService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
            _workQueue = new AsyncBatchingWorkQueue<(Document, ClassificationType, ClassificationOptions)>(
                DelayTimeSpan.NonFocus,
                CacheClassificationsAsync,
                EqualityComparer<(Document, ClassificationType, ClassificationOptions)>.Default,
                AsynchronousOperationListenerProvider.NullListener,
                _cancellationTokenSource.Token);
        }

        public override void Dispose()
        {
            _cancellationTokenSource.Cancel();
            base.Dispose();
        }

        private static string GetPersistenceName(ClassificationType type)
            => type switch
            {
                ClassificationType.Semantic => s_semanticPersistenceName,
                ClassificationType.EmbeddedLanguage => s_embeddedLanguagePersistenceName,
                _ => throw ExceptionUtilities.UnexpectedValue(type),
            };

        public async ValueTask<SerializableClassifiedSpans?> GetCachedClassificationsAsync(
            DocumentKey documentKey, ImmutableArray<TextSpan> textSpans, ClassificationType type, Checksum checksum, CancellationToken cancellationToken)
        {
            var classifiedSpans = await TryGetOrReadCachedSemanticClassificationsAsync(
                documentKey, type, checksum, cancellationToken).ConfigureAwait(false);
            var textSpanIntervalTree = new TextSpanIntervalTree(textSpans);
            return classifiedSpans.IsDefault
                ? null
                : SerializableClassifiedSpans.Dehydrate(classifiedSpans.WhereAsArray(c => textSpanIntervalTree.HasIntervalThatIntersectsWith(c.TextSpan)));
        }

        private static async ValueTask CacheClassificationsAsync(
            ImmutableSegmentedList<(Document document, ClassificationType type, ClassificationOptions options)> documents,
            CancellationToken cancellationToken)
        {
            // First group by type.  That way we process the last semantic and last embedded-lang classifications per document.
            foreach (var typeGroup in documents.GroupBy(t => t.type))
            {
                // Then, group all those requests by document (as we may have gotten many requests for the same
                // document). Then, only process the last document from each group (we don't need to bother stale
                // versions of a particular document).
                foreach (var group in typeGroup.GroupBy(d => d.document.Id))
                {
                    var (document, type, options) = group.Last();
                    await CacheClassificationsAsync(
                        document, type, options, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static async Task CacheClassificationsAsync(
            Document document, ClassificationType type, ClassificationOptions options, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var persistenceService = solution.Services.GetPersistentStorageService();

            var storage = await persistenceService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), cancellationToken).ConfigureAwait(false);
            await using var _1 = storage.ConfigureAwait(false);
            if (storage == null)
                return;

            var classificationService = document.GetLanguageService<IClassificationService>();
            if (classificationService == null)
                return;

            // Very intentionally do our lookup with a special document key.  This doc key stores info independent of
            // project config.  So we can still lookup data regardless of things like if the project is in DEBUG or
            // RELEASE mode.
            var (documentKey, checksum) = await SemanticClassificationCacheUtilities.GetDocumentKeyAndChecksumAsync(
                document, cancellationToken).ConfigureAwait(false);

            var persistenceName = GetPersistenceName(type);
            var matches = await storage.ChecksumMatchesAsync(documentKey, persistenceName, checksum, cancellationToken).ConfigureAwait(false);
            if (matches)
                return;

            using var _2 = Classifier.GetPooledList(out var classifiedSpans);

            // Compute classifications for the full span.
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var fullSpan = new TextSpan(0, text.Length);
            if (type == ClassificationType.Semantic)
            {
                await classificationService.AddSemanticClassificationsAsync(document, fullSpan, options, classifiedSpans, cancellationToken).ConfigureAwait(false);
            }
            else if (type == ClassificationType.EmbeddedLanguage)
            {
                await classificationService.AddEmbeddedLanguageClassificationsAsync(document, fullSpan, options, classifiedSpans, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(type);
            }

            using var stream = SerializableBytes.CreateWritableStream();
            using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
            {
                WriteTo(classifiedSpans, writer);
            }

            stream.Position = 0;
            await storage.WriteStreamAsync(documentKey, persistenceName, stream, checksum, cancellationToken).ConfigureAwait(false);
        }

        private static void WriteTo(SegmentedList<ClassifiedSpan> classifiedSpans, ObjectWriter writer)
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

        private async Task<ImmutableArray<ClassifiedSpan>> TryGetOrReadCachedSemanticClassificationsAsync(
            DocumentKey documentKey,
            ClassificationType type,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            // See if we've loaded this into memory first.
            if (TryGetFromInMemoryCache(documentKey, checksum, out var classifiedSpans))
                return classifiedSpans;

            // Otherwise, attempt to read in classifications from persistence store.
            classifiedSpans = await TryReadCachedSemanticClassificationsAsync(
                documentKey, type, checksum, cancellationToken).ConfigureAwait(false);
            if (classifiedSpans.IsDefault)
                return default;

            UpdateInMemoryCache(documentKey, type, checksum, classifiedSpans);
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
            ClassificationType type,
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
                _cachedData.AddLast((documentKey.Id, type, checksum, classifiedSpans));

                // And ensure we don't cache too many docs.
                if (_cachedData.Count > MaxCachedDocumentCount)
                    _cachedData.RemoveFirst();
            }
        }

        private async Task<ImmutableArray<ClassifiedSpan>> TryReadCachedSemanticClassificationsAsync(
            DocumentKey documentKey,
            ClassificationType type,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            var persistenceService = GetWorkspaceServices().GetPersistentStorageService();
            var storage = await persistenceService.GetStorageAsync(documentKey.Project.Solution, cancellationToken).ConfigureAwait(false);
            await using var _ = storage.ConfigureAwait(false);
            if (storage == null)
                return default;

            var persistenceName = GetPersistenceName(type);
            using var stream = await storage.ReadStreamAsync(documentKey, persistenceName, checksum, cancellationToken).ConfigureAwait(false);
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
                    classificationTypes.Add(reader.ReadRequiredString());

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

                return classifiedSpans.ToImmutableAndClear();
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
