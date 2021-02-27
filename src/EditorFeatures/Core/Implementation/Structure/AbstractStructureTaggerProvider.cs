// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Structure
{
    /// <summary>
    /// Shared implementation of the outliner tagger provider.
    ///
    /// Note: the outliner tagger is a normal buffer tagger provider and not a view tagger provider.
    /// This is important for two reasons.  The first is that if it were view-based then we would lose
    /// the state of the collapsed/open regions when they scrolled in and out of view.  Also, if the
    /// editor doesn't know about all the regions in the file, then it wouldn't be able to
    /// persist them to the SUO file to persist this data across sessions.
    /// </summary>
    internal abstract partial class AbstractStructureTaggerProvider :
        AsynchronousTaggerProvider<IStructureTag>
    {
        protected readonly IEditorOptionsFactoryService EditorOptionsFactoryService;
        protected readonly IProjectionBufferFactoryService ProjectionBufferFactoryService;

        protected AbstractStructureTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
                : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.Outlining), notificationService)
        {
            EditorOptionsFactoryService = editorOptionsFactoryService;
            ProjectionBufferFactoryService = projectionBufferFactoryService;
        }

        protected sealed override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // We listen to the following events:
            // 1) Text changes.  These can obviously affect outlining, so we need to recompute when
            //     we hear about them.
            // 2) Parse option changes.  These can affect outlining when, for example, we change from
            //    DEBUG to RELEASE (affecting the inactive/active regions).
            // 3) When we hear about a workspace being registered.  Outlining may run before a
            //    we even know about a workspace.  This can happen, for example, in the TypeScript
            //    case.  With TypeScript a file is opened, but the workspace is not generated until
            //    some time later when they have examined the file system.  As such, initially,
            //    the file will not have outline spans.  When the workspace is created, we want to
            //    then produce the right outlining spans.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnOptionChanged(subjectBuffer, BlockStructureOptions.ShowBlockStructureGuidesForCodeLevelConstructs, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, BlockStructureOptions.ShowBlockStructureGuidesForDeclarationLevelConstructs, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, BlockStructureOptions.ShowBlockStructureGuidesForCommentsAndPreprocessorRegions, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, BlockStructureOptions.ShowOutliningForCodeLevelConstructs, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, BlockStructureOptions.ShowOutliningForDeclarationLevelConstructs, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions, TaggerDelay.NearImmediate),
                TaggerEventSources.OnOptionChanged(subjectBuffer, BlockStructureOptions.CollapseRegionsWhenCollapsingToDefinitions, TaggerDelay.NearImmediate));
        }

        /// <summary>
        /// Keep this in sync with <see cref="ProduceTagsSynchronously"/>
        /// </summary>
        protected sealed override async Task ProduceTagsAsync(
            TaggerContext<IStructureTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var document = documentSnapshotSpan.Document;
                if (document == null)
                    return;

                // Let LSP handle producing tags in the cloud scenario
                if (documentSnapshotSpan.SnapshotSpan.Snapshot.TextBuffer.IsInLspEditorContext())
                    return;

                var outliningService = BlockStructureService.GetService(document);
                if (outliningService == null)
                    return;

                var blockStructure = await outliningService.GetBlockStructureAsync(
                        documentSnapshotSpan.Document, context.CancellationToken).ConfigureAwait(false);

                ProcessSpans(
                    context, documentSnapshotSpan.SnapshotSpan, outliningService,
                    blockStructure.Spans);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Keep this in sync with <see cref="ProduceTagsAsync"/>
        /// </summary>
        protected sealed override void ProduceTagsSynchronously(
            TaggerContext<IStructureTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var document = documentSnapshotSpan.Document;
                if (document == null)
                    return;

                // Let LSP handle producing tags in the cloud scenario
                if (documentSnapshotSpan.SnapshotSpan.Snapshot.TextBuffer.IsInLspEditorContext())
                    return;

                var outliningService = BlockStructureService.GetService(document);
                if (outliningService == null)
                    return;

                var blockStructure = outliningService.GetBlockStructure(document, context.CancellationToken);
                ProcessSpans(
                    context, documentSnapshotSpan.SnapshotSpan, outliningService,
                    blockStructure.Spans);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void ProcessSpans(
            TaggerContext<IStructureTag> context,
            SnapshotSpan snapshotSpan,
            BlockStructureService outliningService,
            ImmutableArray<BlockSpan> spans)
        {
            var snapshot = snapshotSpan.Snapshot;
            spans = GetMultiLineRegions(outliningService, spans, snapshot);

            foreach (var span in spans)
            {
                var tag = new StructureTag(this, span, snapshot);
                context.AddTag(new TagSpan<IStructureTag>(span.TextSpan.ToSnapshotSpan(snapshot), tag));
            }
        }

        internal abstract object? GetCollapsedHintForm(StructureTag structureTag);

        private static bool s_exceptionReported = false;

        private static ImmutableArray<BlockSpan> GetMultiLineRegions(
            BlockStructureService service,
            ImmutableArray<BlockSpan> regions, ITextSnapshot snapshot)
        {
            // Remove any spans that aren't multiline.
            var multiLineRegions = ArrayBuilder<BlockSpan>.GetInstance();
            foreach (var region in regions)
            {
                if (region.TextSpan.Length > 0)
                {
                    // Check if any clients produced an invalid OutliningSpan.  If so, filter them
                    // out and report a non-fatal watson so we can attempt to determine the source
                    // of the issue.
                    var snapshotSpan = snapshot.GetFullSpan().Span;
                    var regionSpan = region.TextSpan.ToSpan();
                    if (!snapshotSpan.Contains(regionSpan))
                    {
                        if (!s_exceptionReported)
                        {
                            s_exceptionReported = true;
                            try
                            {
                                throw new InvalidOutliningRegionException(service, snapshot, snapshotSpan, regionSpan);
                            }
                            catch (InvalidOutliningRegionException e) when (FatalError.ReportAndCatch(e))
                            {
                            }
                        }
                        continue;
                    }

                    var startLine = snapshot.GetLineNumberFromPosition(region.TextSpan.Start);
                    var endLine = snapshot.GetLineNumberFromPosition(region.TextSpan.End);
                    if (startLine != endLine)
                    {
                        multiLineRegions.Add(region);
                    }
                }
            }

            return multiLineRegions.ToImmutableAndFree();
        }

        #region Creating Preview Buffers

        private const int MaxPreviewText = 1000;

        /// <summary>
        /// Given a <see cref="StructureTag"/>, creates an ITextBuffer with the content to display 
        /// in the tooltip.
        /// </summary>
        protected ITextBuffer CreateElisionBufferForTagTooltip(StructureTag tag)
        {
            // Remove any starting whitespace.
            var span = TrimLeadingWhitespace(new SnapshotSpan(tag.Snapshot, tag.CollapsedHintFormSpan));

            // Trim the length if it's too long.
            var shortSpan = span;
            if (span.Length > MaxPreviewText)
            {
                shortSpan = ComputeShortSpan(span);
            }

            // Create an elision buffer for that span, also trimming the
            // leading whitespace.
            var elisionBuffer = CreateElisionBufferWithoutIndentation(shortSpan);
            var finalBuffer = elisionBuffer;

            // If we trimmed the length, then make a projection buffer that 
            // has the above elision buffer and follows it with "..."
            if (span.Length != shortSpan.Length)
            {
                finalBuffer = CreateTrimmedProjectionBuffer(elisionBuffer);
            }

            return finalBuffer;
        }

        private ITextBuffer CreateTrimmedProjectionBuffer(ITextBuffer elisionBuffer)
        {
            // The elision buffer is too long.  We've already trimmed it, but now we want to add
            // a "..." to it.  We do that by creating a projection of both the elision buffer and
            // a new text buffer wrapping the ellipsis.
            var elisionSpan = elisionBuffer.CurrentSnapshot.GetFullSpan();

            var sourceSpans = new List<object>()
                {
                    elisionSpan.Snapshot.CreateTrackingSpan(elisionSpan, SpanTrackingMode.EdgeExclusive),
                    "..."
                };

            var projectionBuffer = ProjectionBufferFactoryService.CreateProjectionBuffer(
                projectionEditResolver: null,
                sourceSpans: sourceSpans,
                options: ProjectionBufferOptions.None);

            return projectionBuffer;
        }

        private static SnapshotSpan ComputeShortSpan(SnapshotSpan span)
        {
            var endIndex = span.Start + MaxPreviewText;
            var line = span.Snapshot.GetLineFromPosition(endIndex);

            return new SnapshotSpan(span.Snapshot, Span.FromBounds(span.Start, line.EndIncludingLineBreak));
        }

        internal static SnapshotSpan TrimLeadingWhitespace(SnapshotSpan span)
        {
            int start = span.Start;

            while (start < span.End && char.IsWhiteSpace(span.Snapshot[start]))
                start++;

            return new SnapshotSpan(span.Snapshot, Span.FromBounds(start, span.End));
        }

        private ITextBuffer CreateElisionBufferWithoutIndentation(
            SnapshotSpan shortHintSpan)
        {
            return ProjectionBufferFactoryService.CreateProjectionBufferWithoutIndentation(
                EditorOptionsFactoryService.GlobalOptions,
                contentType: null,
                exposedSpans: shortHintSpan);
        }

        #endregion
    }
}
