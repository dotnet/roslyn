// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SemanticClassifier
    {
        /// <summary>
        /// Key we use to look this up in the persistence store for a particular document.
        /// </summary>
        private const string s_persistenceName = "<ClassifiedSpans>";

        /// <summary>
        /// Our current persistence version.  If we ever change the on-disk format, this should be changed so that we
        /// skip over persisted data that we cannot read.
        /// </summary>
        private const int s_classificationFormat = 1;

        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Queue that we put the list of documents we want to compute full classifications for to cache to disk.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<Document> _persistClassificationsWorkQueue;

        /// <summary>
        /// Mapping from workspaces to a task representing when they are fully loaded.  While this task is not complete,
        /// the workspace is still loading.  Once complete the workspace is loaded.  We store the task around, instead
        /// of just awaiting <see cref="IWorkspaceStatusService.IsFullyLoadedAsync"/> as actually awaiting that call
        /// takes non-neglible time (upwards of several hundred ms, to a second), whereas we can actually just use the
        /// status of the <see cref="IWorkspaceStatusService.WaitUntilFullyLoadedAsync"/> task to know when we have
        /// actually transitioned to a loaded state.
        /// </summary>
        private static readonly ConditionalWeakTable<Workspace, Task> s_workspaceToFullyLoadedStateTask =
            new ConditionalWeakTable<Workspace, Task>();

        public SemanticClassifier(
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener)
        {
            _threadingContext = threadingContext;

            // Every second, process the documents we were asked to classify and store their latest classifications to
            // disk. That way we can classify those quickly the next time VS loads (but hasn't fully loaded projects).
            //
            // We dedupe the queue based on docId so that we only persist the last version of a document we were asked
            // to classify and we only hold onto latest solution snapshots until we process them.
            //
            // This approach allows us to not hold onto snapshots too long, while also ensuring we only do work after a
            // reasonable amount of time has passed since the last time the user performs an edit.
            _persistClassificationsWorkQueue = new AsyncBatchingWorkQueue<Document>(
                TimeSpan.FromSeconds(1),
                PersistClassifiedSpansAsync,
                equalityComparer: DocumentByIdEqualityComparer.Instance,
                asyncListener,
                _threadingContext.DisposalToken);
        }

        public async Task ProduceTagsAsync(
            TaggerContext<IClassificationTag> context,
            DocumentSnapshotSpan spanToTag,
            IClassificationService classificationService,
            ClassificationTypeMap typeMap)
        {
            var document = spanToTag.Document;
            if (document == null)
            {
                return;
            }

            var classified = await TryClassifyContainingMemberSpanAsync(
                    context, spanToTag, classificationService, typeMap).ConfigureAwait(false);
            if (classified)
            {
                return;
            }

            // We weren't able to use our specialized codepaths for semantic classifying. 
            // Fall back to classifying the full span that was asked for.
            await ClassifySpansAsync(
                context, spanToTag, classificationService, typeMap).ConfigureAwait(false);
        }

        private async Task<bool> TryClassifyContainingMemberSpanAsync(
            TaggerContext<IClassificationTag> context,
            DocumentSnapshotSpan spanToTag,
            IClassificationService classificationService,
            ClassificationTypeMap typeMap)
        {
            var range = context.TextChangeRange;
            if (range == null)
            {
                // There was no text change range, we can't just reclassify a member body.
                return false;
            }

            // there was top level edit, check whether that edit updated top level element
            var document = spanToTag.Document;
            if (!document.SupportsSyntaxTree)
            {
                return false;
            }

            var cancellationToken = context.CancellationToken;

            var lastSemanticVersion = (VersionStamp?)context.State;
            if (lastSemanticVersion != null)
            {
                var currentSemanticVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                if (lastSemanticVersion.Value != currentSemanticVersion)
                {
                    // A top level change was made.  We can't perform this optimization.
                    return false;
                }
            }

            var service = document.GetLanguageService<ISyntaxFactsService>();

            // perf optimization. Check whether all edits since the last update has happened within
            // a member. If it did, it will find the member that contains the changes and only refresh
            // that member.  If possible, try to get a speculative binder to make things even cheaper.

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var changedSpan = new TextSpan(range.Value.Span.Start, range.Value.NewLength);
            var member = service.GetContainingMemberDeclaration(root, changedSpan.Start);
            if (member == null || !member.FullSpan.Contains(changedSpan))
            {
                // The edit was not fully contained in a member.  Reclassify everything.
                return false;
            }

            var subTextSpan = service.GetMemberBodySpanForSpeculativeBinding(member);
            if (subTextSpan.IsEmpty)
            {
                // Wasn't a member we could reclassify independently.
                return false;
            }

            var subSpan = subTextSpan.Contains(changedSpan) ? subTextSpan.ToSpan() : member.FullSpan.ToSpan();

            var subSpanToTag = new DocumentSnapshotSpan(spanToTag.Document,
                new SnapshotSpan(spanToTag.SnapshotSpan.Snapshot, subSpan));

            // re-classify only the member we're inside.
            await ClassifySpansAsync(
                context, subSpanToTag, classificationService, typeMap).ConfigureAwait(false);
            return true;
        }

        private async Task ClassifySpansAsync(
            TaggerContext<IClassificationTag> context,
            DocumentSnapshotSpan spanToTag,
            IClassificationService classificationService,
            ClassificationTypeMap typeMap)
        {
            try
            {
                var document = spanToTag.Document;
                var snapshotSpan = spanToTag.SnapshotSpan;
                var snapshot = snapshotSpan.Snapshot;

                var cancellationToken = context.CancellationToken;
                using (Logger.LogBlock(FunctionId.Tagger_SemanticClassification_TagProducer_ProduceTags, cancellationToken))
                {
                    var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();

                    await AddSemanticClassificationsAsync(
                        classificationService, document, snapshotSpan.Span.ToTextSpan(), classifiedSpans, cancellationToken: cancellationToken).ConfigureAwait(false);

                    ClassificationUtilities.Convert(typeMap, snapshotSpan.Snapshot, classifiedSpans, context.AddTag);
                    ClassificationUtilities.ReturnClassifiedSpanList(classifiedSpans);

                    var version = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                    // Let the context know that this was the span we actually tried to tag.
                    context.SetSpansTagged(SpecializedCollections.SingletonEnumerable(spanToTag));
                    context.State = version;
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task AddSemanticClassificationsAsync(
            IClassificationService classificationService,
            Document document,
            TextSpan textSpan,
            List<ClassifiedSpan> classifiedSpans,
            CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var fullyLoadedStateTask = s_workspaceToFullyLoadedStateTask.GetValue(
                workspace, w =>
                {
                    var workspaceLoadedService = workspace.Services.GetRequiredService<IWorkspaceStatusService>();
                    return workspaceLoadedService.WaitUntilFullyLoadedAsync(CancellationToken.None);
                });

            // If we're not fully loaded try to read from the cache instead so that classifications appear up to date.
            // New code will not be semantically classified, but will eventually when the project fully loads.
            var isFullyLoaded = fullyLoadedStateTask.IsCompleted;
            if (await TryAddSemanticClassificationsFromCacheAsync(document, textSpan, classifiedSpans, isFullyLoaded, cancellationToken).ConfigureAwait(false))
                return;

            await classificationService.AddSemanticClassificationsAsync(
                document, textSpan, classifiedSpans, cancellationToken).ConfigureAwait(false);

            // Once fully loaded, add this document to the work queue to persist its classifications to storage in the
            // future.  This way we can store the full accurate set of classifications for it to help speed things up in
            // VS on next launch.
            if (isFullyLoaded)
                _persistClassificationsWorkQueue.AddWork(document);
        }

        private static async Task<bool> TryAddSemanticClassificationsFromCacheAsync(
            Document document,
            TextSpan textSpan,
            List<ClassifiedSpan> result,
            bool isFullyLoaded,
            CancellationToken cancellationToken)
        {
            // Don't use the cache if we're fully loaded.  We should just compute values normally.
            if (isFullyLoaded)
                return false;

            var solution = document.Project.Solution;
            var storageService = (IChecksummedPersistentStorageService)solution.Workspace.Services.GetRequiredService<IPersistentStorageService>();
            using var storage = storageService.GetStorage(solution);
            if (storage == null)
                return false;

            // Try to read the existing cached data from the persistence service.  However, only bother reading it if it
            // was stored against the same checksum that we're now checking against.
            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);
            using var stream = await storage.ReadStreamAsync(document, s_persistenceName, checksum, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            if (reader == null)
                return false;

            return TryRead(textSpan, result, reader);
        }

        private static async Task<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
        {
            // We only checksum off of the contents of the file.  During load, we can't really compute any other
            // information since we don't necessarily know about other files, metadata, or dependencies.  So during
            // load, we allow for the previous semantic classifications to be used as long as the file contents match.
            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = checksums.Text;
            return textChecksum;
        }

        private Task PersistClassifiedSpansAsync(
            ImmutableArray<Document> documentsToClassify, CancellationToken cancellationToken)
        {
#if DEBUG
            var seenIds = new HashSet<DocumentId>();
            foreach (var document in documentsToClassify)
                Contract.ThrowIfFalse(seenIds.Add(document.Id));
#endif
            return Task.WhenAll(documentsToClassify.Select(d => PersistClassifiedSpansAsync(d, cancellationToken)));
        }

        private async Task PersistClassifiedSpansAsync(Document document, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var storageService = (IChecksummedPersistentStorageService)solution.Workspace.Services.GetRequiredService<IPersistentStorageService>();
            using var storage = storageService.GetStorage(solution);
            if (storage == null)
                return;

            // Don't need to do anything if the information we've persisted matches the checksum of this doc.
            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);
            var persistedChecksum = await storage.ReadChecksumAsync(document, s_persistenceName, cancellationToken).ConfigureAwait(false);
            if (checksum == persistedChecksum)
                return;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var classifiedSpans = ClassificationUtilities.GetOrCreateClassifiedSpanList();

            try
            {
                // Compute the classifications for the full document span.
                await AddSemanticClassificationsAsync(
                    document.GetLanguageService<IClassificationService>(), document,
                    new TextSpan(0, text.Length), classifiedSpans, cancellationToken).ConfigureAwait(false);

                using var stream = SerializableBytes.CreateWritableStream();
                using (var writer = new ObjectWriter(stream, leaveOpen: true, cancellationToken))
                {
                    WriteTo(classifiedSpans, writer);
                }

                stream.Position = 0;
                await storage.WriteStreamAsync(document, s_persistenceName, stream, checksum, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (IOUtilities.IsNormalIOException(e))
            {
                // Storage APIs can throw arbitrary exceptions.
            }
            finally
            {
                ClassificationUtilities.ReturnClassifiedSpanList(classifiedSpans);
            }
        }

        private static void WriteTo(List<ClassifiedSpan> classifiedSpans, ObjectWriter writer)
        {
            writer.WriteInt32(s_classificationFormat);

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

        private static bool TryRead(TextSpan textSpan, List<ClassifiedSpan> result, ObjectReader reader)
        {
            try
            {
                // if the format doesn't match, we def can't read this.
                if (reader.ReadInt32() != s_classificationFormat)
                    return false;

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
                return true;
            }
            catch
            {
                // We're reading and interpreting arbitrary data from disk.  This may be invalid for any reason.
                Logger.Log(FunctionId.SemanticClassifier_ExceptionInCacheRead);
                return false;
            }
        }
    }
}
