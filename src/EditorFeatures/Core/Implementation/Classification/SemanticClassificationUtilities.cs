// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal static class SemanticClassificationUtilities
    {
        public static async Task ProduceTagsAsync(
            TaggerContext<IClassificationTag> context,
            DocumentSnapshotSpan spanToTag,
            IClassificationService classificationService,
            ClassificationTypeMap typeMap)
        {
            var document = spanToTag.Document;
            if (document == null)
                return;

            // Don't block getting classifications on building the full compilation.  This may take a significant amount
            // of time and can cause a very latency sensitive operation (copying) to block the user while we wait on this
            // work to happen.  
            //
            // It's also a better experience to get classifications to the user faster versus waiting a potentially
            // large amount of time waiting for all the compilation information to be built.  For example, we can
            // classify types that we've parsed in other files, or partially loaded from metadata, even if we're still
            // parsing/loading.  For cross language projects, this also produces semantic classifications more quickly
            // as we do not have to wait on skeletons to be built.

            document = document.WithFrozenPartialSemantics(context.CancellationToken);
            spanToTag = new DocumentSnapshotSpan(document, spanToTag.SnapshotSpan);

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

        private static async Task<bool> TryClassifyContainingMemberSpanAsync(
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

        private static async Task ClassifySpansAsync(
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
                    using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var classifiedSpans);

                    await AddSemanticClassificationsAsync(
                        document, snapshotSpan.Span.ToTextSpan(), classificationService, classifiedSpans, cancellationToken: cancellationToken).ConfigureAwait(false);

                    foreach (var span in classifiedSpans)
                        context.AddTag(ClassificationUtilities.Convert(typeMap, snapshotSpan.Snapshot, span));

                    var version = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                    // Let the context know that this was the span we actually tried to tag.
                    context.SetSpansTagged(SpecializedCollections.SingletonEnumerable(spanToTag));
                    context.State = version;
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task AddSemanticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            IClassificationService classificationService,
            ArrayBuilder<ClassifiedSpan> classifiedSpans,
            CancellationToken cancellationToken)
        {
            var workspaceStatusService = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();

            // Importantly, we do not await/wait on the fullyLoadedStateTask.  We do not want to ever be waiting on work
            // that may end up touching the UI thread (As we can deadlock if GetTagsSynchronous waits on us).  Instead,
            // we only check if the Task is completed.  Prior to that we will assume we are still loading.  Once this
            // task is completed, we know that the WaitUntilFullyLoadedAsync call will have actually finished and we're
            // fully loaded.
            var isFullyLoadedTask = workspaceStatusService.IsFullyLoadedAsync(cancellationToken);
            var isFullyLoaded = isFullyLoadedTask.IsCompleted && isFullyLoadedTask.GetAwaiter().GetResult();

            // If we're not fully loaded try to read from the cache instead so that classifications appear up to date.
            // New code will not be semantically classified, but will eventually when the project fully loads.
            if (await TryAddSemanticClassificationsFromCacheAsync(document, textSpan, classifiedSpans, isFullyLoaded, cancellationToken).ConfigureAwait(false))
                return;

            await classificationService.AddSemanticClassificationsAsync(
                document, textSpan, classifiedSpans, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> TryAddSemanticClassificationsFromCacheAsync(
            Document document,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> classifiedSpans,
            bool isFullyLoaded,
            CancellationToken cancellationToken)
        {
            // Don't use the cache if we're fully loaded.  We should just compute values normally.
            if (isFullyLoaded)
                return false;

            var semanticCacheService = document.Project.Solution.Workspace.Services.GetService<ISemanticClassificationCacheService>();
            if (semanticCacheService == null)
                return false;

            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var checksum = checksums.Text;

            var result = await semanticCacheService.GetCachedSemanticClassificationsAsync(
                SemanticClassificationCacheUtilities.GetDocumentKeyForCaching(document),
                textSpan, checksum, cancellationToken).ConfigureAwait(false);
            if (result.IsDefault)
                return false;

            classifiedSpans.AddRange(result);
            return true;
        }
    }
}
