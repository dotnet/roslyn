// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
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

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassificationUtilities
    {
        public static TagSpan<IClassificationTag> Convert(IClassificationTypeMap typeMap, ITextSnapshot snapshot, ClassifiedSpan classifiedSpan)
        {
            return new TagSpan<IClassificationTag>(
                classifiedSpan.TextSpan.ToSnapshotSpan(snapshot),
                new ClassificationTag(typeMap.GetClassificationType(classifiedSpan.ClassificationType)));
        }

        public static List<ITagSpan<IClassificationTag>> Convert(IClassificationTypeMap typeMap, ITextSnapshot snapshot, ArrayBuilder<ClassifiedSpan> classifiedSpans)
        {
            var result = new List<ITagSpan<IClassificationTag>>(capacity: classifiedSpans.Count);
            foreach (var span in classifiedSpans)
                result.Add(Convert(typeMap, snapshot, span));

            return result;
        }

        public static async Task ProduceTagsAsync(
            TaggerContext<IClassificationTag> context,
            DocumentSnapshotSpan spanToTag,
            IClassificationService classificationService,
            ClassificationTypeMap typeMap,
            ClassificationOptions options,
            ClassificationType type,
            CancellationToken cancellationToken)
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

            document = document.WithFrozenPartialSemantics(cancellationToken);
            options = options with { ForceFrozenPartialSemanticsForCrossProcessOperations = true };

            var classified = await TryClassifyContainingMemberSpanAsync(
                    context, document, spanToTag.SnapshotSpan, classificationService, typeMap, options, type, cancellationToken).ConfigureAwait(false);
            if (classified)
            {
                return;
            }

            // We weren't able to use our specialized codepaths for semantic classifying. 
            // Fall back to classifying the full span that was asked for.
            await ClassifySpansAsync(
                context, document, spanToTag.SnapshotSpan, classificationService, typeMap, options, type, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<bool> TryClassifyContainingMemberSpanAsync(
            TaggerContext<IClassificationTag> context,
            Document document,
            SnapshotSpan snapshotSpan,
            IClassificationService classificationService,
            ClassificationTypeMap typeMap,
            ClassificationOptions options,
            ClassificationType type,
            CancellationToken cancellationToken)
        {
            var range = context.TextChangeRange;
            if (range == null)
            {
                // There was no text change range, we can't just reclassify a member body.
                return false;
            }

            // there was top level edit, check whether that edit updated top level element
            if (!document.SupportsSyntaxTree)
                return false;

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

            var service = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // perf optimization. Check whether all edits since the last update has happened within
            // a member. If it did, it will find the member that contains the changes and only refresh
            // that member.  If possible, try to get a speculative binder to make things even cheaper.

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

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

            var subSpanToTag = new SnapshotSpan(
                snapshotSpan.Snapshot,
                subTextSpan.Contains(changedSpan) ? subTextSpan.ToSpan() : member.FullSpan.ToSpan());

            // re-classify only the member we're inside.
            await ClassifySpansAsync(
                context, document, subSpanToTag, classificationService, typeMap, options, type, cancellationToken).ConfigureAwait(false);
            return true;
        }

        private static async Task ClassifySpansAsync(
            TaggerContext<IClassificationTag> context,
            Document document,
            SnapshotSpan snapshotSpan,
            IClassificationService classificationService,
            ClassificationTypeMap typeMap,
            ClassificationOptions options,
            ClassificationType type,
            CancellationToken cancellationToken)
        {
            try
            {
                using (Logger.LogBlock(FunctionId.Tagger_SemanticClassification_TagProducer_ProduceTags, cancellationToken))
                {
                    using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var classifiedSpans);

                    await AddClassificationsAsync(
                        classificationService, options, document, snapshotSpan, classifiedSpans, type, cancellationToken).ConfigureAwait(false);

                    foreach (var span in classifiedSpans)
                        context.AddTag(Convert(typeMap, snapshotSpan.Snapshot, span));

                    var version = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                    // Let the context know that this was the span we actually tried to tag.
                    context.SetSpansTagged(ImmutableArray.Create(snapshotSpan));
                    context.State = version;
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task AddClassificationsAsync(
            IClassificationService classificationService,
            ClassificationOptions options,
            Document document,
            SnapshotSpan snapshotSpan,
            ArrayBuilder<ClassifiedSpan> classifiedSpans,
            ClassificationType type,
            CancellationToken cancellationToken)
        {
            if (type == ClassificationType.Semantic)
            {
                await classificationService.AddSemanticClassificationsAsync(
                   document, snapshotSpan.Span.ToTextSpan(), options, classifiedSpans, cancellationToken).ConfigureAwait(false);
            }
            else if (type == ClassificationType.EmbeddedLanguage)
            {
                await classificationService.AddEmbeddedLanguageClassificationsAsync(
                   document, snapshotSpan.Span.ToTextSpan(), options, classifiedSpans, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(type);
            }
        }
    }
}
