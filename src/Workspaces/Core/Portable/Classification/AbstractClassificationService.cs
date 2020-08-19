// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal abstract partial class AbstractClassificationService : IClassificationService, IDisposable
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

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly AsyncBatchingWorkQueue<Document> _persistClassificationsWorkQueue;

        protected AbstractClassificationService()
        {
            // Every second process the documents we were asked to classify and store their latest classifications to
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
                DocumentIdEqualityComparer.Instance,
                asyncListener: null,
                _tokenSource.Token);
        }

        public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken);
        public abstract ClassifiedSpan AdjustStaleClassification(SourceText text, ClassifiedSpan classifiedSpan);

        public void Dispose()
        {
            // Stop all the BG work to persist out classifications.
            _tokenSource.Cancel();
        }

        public async Task AddSemanticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetLanguageService<ISyntaxClassificationService>();
            if (classificationService == null)
            {
                // When renaming a file's extension through VS when it's opened in editor, 
                // the content type might change and the content type changed event can be 
                // raised before the renaming propagate through VS workspace. As a result, 
                // the document we got (based on the buffer) could still be the one in the workspace
                // before rename happened. This would cause us problem if the document is supported 
                // by workspace but not a roslyn language (e.g. xaml, F#, etc.), since none of the roslyn 
                // language services would be available.
                //
                // If this is the case, we will simply bail out. It's OK to ignore the request
                // because when the buffer eventually get associated with the correct document in roslyn
                // workspace, we will be invoked again.
                //
                // For example, if you open a xaml from from a WPF project in designer view,
                // and then rename file extension from .xaml to .cs, then the document we received
                // here would still belong to the special "-xaml" project.
                return;
            }

            var workspace = document.Project.Solution.Workspace;
            var workspaceLoadedService = workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            var isFullyLoaded = await workspaceLoadedService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

            var remoteSuccess = await TryAddSemanticClassificationsInRemoteProcessAsync(
                document, textSpan, result, isFullyLoaded, cancellationToken).ConfigureAwait(false);
            if (remoteSuccess)
                return;

            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var temp);
            await AddSemanticClassificationsInCurrentProcessAsync(
                document, textSpan, temp, isFullyLoaded, cancellationToken).ConfigureAwait(false);
            AddRange(temp, result);
        }

        /// <returns><see langword="true"/> if the remote call was made successfully and we should
        /// use the results of it. Otherwise, fall back to processing locally</returns>
        private static async Task<bool> TryAddSemanticClassificationsInRemoteProcessAsync(
            Document document,
            TextSpan textSpan,
            List<ClassifiedSpan> result,
            bool isFullyLoaded,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return false;

            var classifiedSpans = await client.RunRemoteAsync<SerializableClassifiedSpans>(
                WellKnownServiceHubService.CodeAnalysis,
                nameof(IRemoteSemanticClassificationService.GetSemanticClassificationsAsync),
                project.Solution,
                new object[] { document.Id, textSpan, isFullyLoaded },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);

            classifiedSpans.Rehydrate(result);
            return true;
        }

        public async Task AddSemanticClassificationsInCurrentProcessAsync(
            Document document,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> result,
            bool isFullyLoaded,
            CancellationToken cancellationToken)
        {
            // if we're not fully loaded try to read from the cache instead so that classifications appear up to
            // date.  New code will not be semantically classified, but will eventually when the project fully
            // loads.
            if (await TryAddSemanticClassificationsFromCacheAsync(document, textSpan, result, isFullyLoaded, cancellationToken).ConfigureAwait(false))
                return;

            // Weren't able to use the cache. Compute the classifications normally.
            await ComputeSemanticClassificationsAsync(document, textSpan, result, cancellationToken).ConfigureAwait(false);

            // Add this document to the work queue to persist its classifications to storage in the future.
            if (isFullyLoaded)
                _persistClassificationsWorkQueue.AddWork(document);
        }

        private static async Task ComputeSemanticClassificationsAsync(Document document, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetRequiredLanguageService<ISyntaxClassificationService>();

            var extensionManager = document.Project.Solution.Workspace.Services.GetRequiredService<IExtensionManager>();
            var classifiers = classificationService.GetDefaultSyntaxClassifiers();

            var getNodeClassifiers = extensionManager.CreateNodeExtensionGetter(classifiers, c => c.SyntaxNodeTypes);
            var getTokenClassifiers = extensionManager.CreateTokenExtensionGetter(classifiers, c => c.SyntaxTokenKinds);

            await classificationService.AddSemanticClassificationsAsync(document, textSpan, getNodeClassifiers, getTokenClassifiers, result, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> TryAddSemanticClassificationsFromCacheAsync(
            Document document,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> result,
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

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);
            using var stream = await storage.ReadStreamAsync(document, s_persistenceName, checksum, cancellationToken).ConfigureAwait(false);
            using var reader = ObjectReader.TryGetReader(stream, cancellationToken: cancellationToken);
            if (reader == null)
                return false;

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

                // succeeded reading in the classified spans.  copy over from our temp array to the final result.
                result.AddRange(tempResult);
                return true;
            }
            catch
            {
                // We're reading and interpreting arbitrary data from disk.  This may be invalid for any reason.
                Logger.Log(FunctionId.AbstractClassificationService_ExceptionInCacheRead);
                return false;
            }
        }

        private static async Task<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
        {
            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = checksums.Text;
            return textChecksum;
        }

        private static Task PersistClassifiedSpansAsync(
            ImmutableArray<Document> documentsToClassify, CancellationToken cancellationToken)
        {
#if DEBUG
            var seenIds = new HashSet<DocumentId>();
            foreach (var document in documentsToClassify)
                Contract.ThrowIfFalse(seenIds.Add(document.Id));
#endif
            return Task.WhenAll(documentsToClassify.Select(d => PersistClassifiedSpansAsync(d, cancellationToken)));
        }

        private static async Task PersistClassifiedSpansAsync(Document document, CancellationToken cancellationToken)
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

            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var classifiedSpans);

            // Compute the classifications for the full document span.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            await ComputeSemanticClassificationsAsync(
                document, new TextSpan(0, text.Length), classifiedSpans, cancellationToken).ConfigureAwait(false);

            try
            {
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
        }

        private static void WriteTo(ArrayBuilder<ClassifiedSpan> classifiedSpans, ObjectWriter writer)
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

        public async Task AddSyntacticClassificationsAsync(Document document, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var classificationService = document.GetLanguageService<ISyntaxClassificationService>();
            if (classificationService == null)
            {
                // When renaming a file's extension through VS when it's opened in editor, 
                // the content type might change and the content type changed event can be 
                // raised before the renaming propagate through VS workspace. As a result, 
                // the document we got (based on the buffer) could still be the one in the workspace
                // before rename happened. This would cause us problem if the document is supported 
                // by workspace but not a roslyn language (e.g. xaml, F#, etc.), since none of the roslyn 
                // language services would be available.
                //
                // If this is the case, we will simply bail out. It's OK to ignore the request
                // because when the buffer eventually get associated with the correct document in roslyn
                // workspace, we will be invoked again.
                //
                // For example, if you open a xaml from from a WPF project in designer view,
                // and then rename file extension from .xaml to .cs, then the document we received
                // here would still belong to the special "-xaml" project.
                return;
            }

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(syntaxTree);

            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var temp);
            classificationService.AddSyntacticClassifications(syntaxTree, textSpan, temp, cancellationToken);
            AddRange(temp, result);
        }

        /// <summary>
        /// Helper to add all the values of <paramref name="temp"/> into <paramref name="result"/>
        /// without causing any allocations or boxing of enumerators.
        /// </summary>
        protected static void AddRange(ArrayBuilder<ClassifiedSpan> temp, List<ClassifiedSpan> result)
        {
            foreach (var span in temp)
            {
                result.Add(span);
            }
        }
    }
}
