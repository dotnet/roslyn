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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    using static RemoteSemanticClassificationCacheService;

    internal sealed class RemoteSemanticClassificationCacheAnalyzer : IncrementalAnalyzerBase
    {
        private static async Task<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
        {
            // We only checksum off of the contents of the file.  During load, we can't really compute any other
            // information since we don't necessarily know about other files, metadata, or dependencies.  So during
            // load, we allow for the previous semantic classifications to be used as long as the file contents match.
            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = checksums.Text;
            return textChecksum;
        }

        public override async Task AnalyzeDocumentAsync(
            Document document, SyntaxNode? body, InvocationReasons reasons, CancellationToken cancellationToken)
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
                await storage.WriteStreamAsync(document, RemoteSemanticClassificationCacheService.PersistenceName, stream, checksum, cancellationToken).ConfigureAwait(false);
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
    }
}
