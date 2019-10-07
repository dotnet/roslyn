// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
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
    internal abstract partial class AbstractStructureTaggerProvider<TRegionTag> :
        AsynchronousTaggerProvider<TRegionTag>
        where TRegionTag : class, ITag
    {
        private static readonly IComparer<BlockSpan> s_blockSpanComparer =
            Comparer<BlockSpan>.Create((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);

        protected readonly ITextEditorFactoryService TextEditorFactoryService;
        protected readonly IEditorOptionsFactoryService EditorOptionsFactoryService;
        protected readonly IProjectionBufferFactoryService ProjectionBufferFactoryService;

        protected AbstractStructureTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IAsynchronousOperationListenerProvider listenerProvider)
                : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.Outlining), notificationService)
        {
            TextEditorFactoryService = textEditorFactoryService;
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
            TaggerContext<TRegionTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var outliningService = TryGetService(context, documentSnapshotSpan);
                if (outliningService != null)
                {
                    var blockStructure = await outliningService.GetBlockStructureAsync(
                        documentSnapshotSpan.Document, context.CancellationToken).ConfigureAwait(false);

                    ProcessSpans(
                        context, documentSnapshotSpan.SnapshotSpan, outliningService,
                        blockStructure.Spans);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Keep this in sync with <see cref="ProduceTagsAsync"/>
        /// </summary>
        protected sealed override void ProduceTagsSynchronously(
            TaggerContext<TRegionTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var outliningService = TryGetService(context, documentSnapshotSpan);
                if (outliningService != null)
                {
                    var document = documentSnapshotSpan.Document;
                    var cancellationToken = context.CancellationToken;

                    // Try to call through the synchronous service if possible. Otherwise, fallback
                    // and make a blocking call against the async service.

                    var blockStructure = outliningService.GetBlockStructure(document, cancellationToken);

                    ProcessSpans(
                        context, documentSnapshotSpan.SnapshotSpan, outliningService,
                        blockStructure.Spans);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private BlockStructureService TryGetService(
            TaggerContext<TRegionTag> context,
            DocumentSnapshotSpan documentSnapshotSpan)
        {
            var cancellationToken = context.CancellationToken;
            using (Logger.LogBlock(FunctionId.Tagger_Outlining_TagProducer_ProduceTags, cancellationToken))
            {
                var document = documentSnapshotSpan.Document;
                if (document != null)
                {
                    return BlockStructureService.GetService(document);
                }
            }

            return null;
        }

        private void ProcessSpans(
            TaggerContext<TRegionTag> context,
            SnapshotSpan snapshotSpan,
            BlockStructureService outliningService,
            ImmutableArray<BlockSpan> spans)
        {
            try
            {
                ProcessSpansWorker(context, snapshotSpan, outliningService, spans);
            }
            catch (TypeLoadException)
            {
                // We're targeting a version of the BlockTagging infrastructure in 
                // VS that may not match the version that the user is currently
                // developing against.  Be resilient to this until everything moves
                // forward to the right VS version.
            }
        }

        private void ProcessSpansWorker(
            TaggerContext<TRegionTag> context,
            SnapshotSpan snapshotSpan,
            BlockStructureService outliningService,
            ImmutableArray<BlockSpan> spans)
        {
            if (spans != null)
            {
                var snapshot = snapshotSpan.Snapshot;
                spans = GetMultiLineRegions(outliningService, spans, snapshot);

                // Create the outlining tags.
                var tagSpanStack = new Stack<TagSpan<TRegionTag>>();

                foreach (var region in spans)
                {
                    var spanToCollapse = new SnapshotSpan(snapshot, region.TextSpan.ToSpan());

                    while (tagSpanStack.Count > 0 &&
                           tagSpanStack.Peek().Span.End <= spanToCollapse.Span.Start)
                    {
                        tagSpanStack.Pop();
                    }

                    var parentTag = tagSpanStack.Count > 0 ? tagSpanStack.Peek() : null;
                    var tag = CreateTag(parentTag?.Tag, snapshot, region);

                    if (tag != null)
                    {
                        var tagSpan = new TagSpan<TRegionTag>(spanToCollapse, tag);

                        context.AddTag(tagSpan);
                        tagSpanStack.Push(tagSpan);
                    }
                }
            }
        }

        protected abstract TRegionTag CreateTag(TRegionTag parentTag, ITextSnapshot snapshot, BlockSpan region);

        private static bool s_exceptionReported = false;

        private ImmutableArray<BlockSpan> GetMultiLineRegions(
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
                            catch (InvalidOutliningRegionException e) when (FatalError.ReportWithoutCrash(e))
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

            // Make sure the regions are lexicographically sorted.  This is needed
            // so we can appropriately parent them for BlockTags.
            //
            // Note we pass a IComparer instead of a Comparison to work around this
            // issue in ImmutableArray.Builder: https://github.com/dotnet/corefx/issues/11173
            multiLineRegions.Sort(s_blockSpanComparer);
            return multiLineRegions.ToImmutableAndFree();
        }
    }
}
