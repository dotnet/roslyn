// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteSemanticClassificationCacheService : ServiceBase, IRemoteSemanticClassificationCacheService
    {
        /// <summary>
        /// Key we use to look this up in the persistence store for a particular document.
        /// </summary>
        public const string PersistenceName = "<ClassifiedSpans>";

        /// <summary>
        /// Our current persistence version.  If we ever change the on-disk format, this should be changed so that we
        /// skip over persisted data that we cannot read.
        /// </summary>
        public const int ClassificationFormat = 1;

        public RemoteSemanticClassificationCacheService(
            Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            StartService();
        }

        public Task StartCachingSemanticClassificationsAsync(CancellationToken cancellation)
        {
            return RunServiceAsync(() =>
            {
                var registrationService = GetWorkspace().Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteSemanticClassificationCacheAnalyzerProvider(this.EndPoint);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteSemanticClassificationCacheAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return Task.CompletedTask;
            }, cancellation);
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
