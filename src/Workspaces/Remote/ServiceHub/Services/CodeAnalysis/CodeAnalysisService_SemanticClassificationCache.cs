// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteSemanticClassificationCacheService
    {
        /// <summary>
        /// Key we use to look this up in the persistence store for a particular document.
        /// </summary>
        public const string PersistenceName = "<ClassifiedSpans>";

        /// <summary>
        /// Our current persistence version.  If we ever change the on-disk format, this should be changed so that we
        /// skip over persisted data that we cannot read.
        /// </summary>
        public const int ClassificationFormat = 2;

        private static async Task<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
        {
            // We only checksum off of the contents of the file.  During load, we can't really compute any other
            // information since we don't necessarily know about other files, metadata, or dependencies.  So during
            // load, we allow for the previous semantic classifications to be used as long as the file contents match.
            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = checksums.Text;
            return textChecksum;
        }

        public Task CacheSemanticClassificationsAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
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

            // Now emit each classified span as a triple of it's type, start, length.
            writer.WriteInt32(classifiedSpans.Count);
            foreach (var classifiedSpan in classifiedSpans)
            {
                writer.WriteInt32(seenClassificationTypes[classifiedSpan.ClassificationType]);
                writer.WriteInt32(classifiedSpan.TextSpan.Start);
                writer.WriteInt32(classifiedSpan.TextSpan.Length);
            }
        }

        public Task<SerializableClassifiedSpans> GetCachedSemanticClassificationsAsync(
            SerializableDocumentKey documentKey, TextSpan textSpan, Checksum checksum, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var classifiedSpans);

                await AddCachedSemanticClassificationsAsync(
                    documentKey.Rehydrate(), textSpan, checksum, classifiedSpans, cancellationToken).ConfigureAwait(false);

                return SerializableClassifiedSpans.Dehydrate(classifiedSpans);
            }, cancellationToken);
        }

        private async Task AddCachedSemanticClassificationsAsync(
            DocumentKey documentKey,
            TextSpan textSpan,
            Checksum checksum,
            ArrayBuilder<ClassifiedSpan> classifiedSpans,
            CancellationToken cancellationToken)
        {
            var workspace = GetWorkspace();
            var persistenceService = workspace.Services.GetService<IPersistentStorageService>() as IChecksummedPersistentStorageService;
            if (persistenceService == null)
                return;

            using var storage = persistenceService.GetStorage(workspace, documentKey.Project.Solution, checkBranchId: false);
            if (storage == null)
                return;

            using var stream = await storage.ReadStreamAsync(documentKey, PersistenceName, checksum, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            if (reader == null)
                return;

            Read(textSpan, classifiedSpans, reader);
        }

        private static void Read(TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, ObjectReader reader)
        {
            try
            {
                // if the format doesn't match, we def can't read this.
                if (reader.ReadInt32() != ClassificationFormat)
                    return;

                // For space efficiency, the unique classification types are emitted in one array up front, and then the
                // specific classification type is referred to by index when emitting the individual spans.
                var classificationTypesCount = reader.ReadInt32();
                using var _1 = ArrayBuilder<string>.GetInstance(classificationTypesCount, out var classificationTypes);

                for (var i = 0; i < classificationTypesCount; i++)
                    classificationTypes.Add(reader.ReadString());

                var classifiedSpanCount = reader.ReadInt32();
                using var _2 = ArrayBuilder<ClassifiedSpan>.GetInstance(classifiedSpanCount, out var tempResult);

                for (var i = 0; i < classifiedSpanCount; i++)
                {
                    var typeIndex = reader.ReadInt32();
                    var start = reader.ReadInt32();
                    var length = reader.ReadInt32();

                    var classification = classificationTypes[typeIndex];
                    var classifiedSpan = new TextSpan(start, length);
                    if (textSpan.IntersectsWith(classifiedSpan))
                        tempResult.Add(new ClassifiedSpan(classification, classifiedSpan));
                }

                // succeeded reading in the classified spans.  copy over from our temp array to the final result. do
                // this last so we only mutate the result once we've successfully deserialized the entire blob.
                result.AddRange(tempResult);
            }
            catch
            {
                // We're reading and interpreting arbitrary data from disk.  This may be invalid for any reason.
                Internal.Log.Logger.Log(FunctionId.RemoteSemanticClassificationCacheService_ExceptionInCacheRead);
            }
        }
    }
}
