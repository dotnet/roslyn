// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;

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

        protected readonly ICocoaTextEditorFactoryService TextEditorFactoryService;
        protected readonly IEditorOptionsFactoryService EditorOptionsFactoryService;
        protected readonly IProjectionBufferFactoryService ProjectionBufferFactoryService;

        protected AbstractStructureTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ICocoaTextEditorFactoryService textEditorFactoryService,
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
                // Let LSP handle producing tags in the cloud scenario
                if (documentSnapshotSpan.SnapshotSpan.Snapshot.TextBuffer.IsInLspEditorContext())
                {
                    return;
                }

                var outliningService = AbstractStructureTaggerProvider<TRegionTag>.TryGetService(context, documentSnapshotSpan);
                if (outliningService != null)
                {
                    var blockStructure = await outliningService.GetBlockStructureAsync(
                        documentSnapshotSpan.Document, context.CancellationToken).ConfigureAwait(false);

                    ProcessSpans(
                        context, documentSnapshotSpan.SnapshotSpan, outliningService,
                        blockStructure.Spans);
                }
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
            TaggerContext<TRegionTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                // Let LSP handle producing tags in the cloud scenario
                if (documentSnapshotSpan.SnapshotSpan.Snapshot.TextBuffer.IsInLspEditorContext())
                {
                    return;
                }

                var outliningService = AbstractStructureTaggerProvider<TRegionTag>.TryGetService(context, documentSnapshotSpan);
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
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static BlockStructureService TryGetService(
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
                // We're targetting a version of the BlockTagging infrastructure in
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
                spans = AbstractStructureTaggerProvider<TRegionTag>.GetMultiLineRegions(outliningService, spans, snapshot);

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

        private static ImmutableArray<BlockSpan> GetMultiLineRegions(
#pragma warning disable IDE0060 // Remove unused parameter
            BlockStructureService service,
#pragma warning restore IDE0060 // Remove unused parameter
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

    internal static partial class ITextSnapshotExtensions
    {
        public static SnapshotPoint GetPoint(this ITextSnapshot snapshot, int position)
            => new SnapshotPoint(snapshot, position);

        public static SnapshotPoint? TryGetPoint(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            var position = snapshot.TryGetPosition(lineNumber, columnIndex);
            if (position.HasValue)
            {
                return new SnapshotPoint(snapshot, position.Value);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Convert a <see cref="LinePositionSpan"/> to <see cref="TextSpan"/>.
        /// </summary>
        public static TextSpan GetTextSpan(this ITextSnapshot snapshot, LinePositionSpan span)
        {
            return TextSpan.FromBounds(
                GetPosition(snapshot, span.Start.Line, span.Start.Character),
                GetPosition(snapshot, span.End.Line, span.End.Character));
        }

        public static int GetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
            => TryGetPosition(snapshot, lineNumber, columnIndex).Value;

        public static int? TryGetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex)
        {
            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
            {
                return null;
            }

            var end = snapshot.GetLineFromLineNumber(lineNumber).Start.Position + columnIndex;
            if (end < 0 || end > snapshot.Length)
            {
                return null;
            }

            return end;
        }

        public static bool TryGetPosition(this ITextSnapshot snapshot, int lineNumber, int columnIndex, out SnapshotPoint position)
        {
            position = new SnapshotPoint();

            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
            {
                return false;
            }

            var line = snapshot.GetLineFromLineNumber(lineNumber);
            if (columnIndex < 0 || columnIndex >= line.Length)
            {
                return false;
            }

            var result = line.Start.Position + columnIndex;
            position = new SnapshotPoint(snapshot, result);
            return true;
        }

        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, int start, int length)
            => new SnapshotSpan(snapshot, new Span(start, length));

        public static SnapshotSpan GetSpanFromBounds(this ITextSnapshot snapshot, int start, int end)
            => new SnapshotSpan(snapshot, Span.FromBounds(start, end));

        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, Span span)
            => new SnapshotSpan(snapshot, span);

        public static ITagSpan<TTag> GetTagSpan<TTag>(this ITextSnapshot snapshot, Span span, TTag tag)
            where TTag : ITag
        {
            return new TagSpan<TTag>(new SnapshotSpan(snapshot, span), tag);
        }

        public static SnapshotSpan GetSpan(this ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex)
        {
            return TryGetSpan(snapshot, startLine, startIndex, endLine, endIndex).Value;
        }

        public static SnapshotSpan? TryGetSpan(this ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex)
        {
            var startPosition = snapshot.TryGetPosition(startLine, startIndex);
            var endPosition = snapshot.TryGetPosition(endLine, endIndex);
            if (startPosition == null || endPosition == null)
            {
                return null;
            }

            return new SnapshotSpan(snapshot, Span.FromBounds(startPosition.Value, endPosition.Value));
        }

        public static SnapshotSpan GetFullSpan(this ITextSnapshot snapshot)
        {
            Contract.ThrowIfNull(snapshot);

            return new SnapshotSpan(snapshot, new Span(0, snapshot.Length));
        }

        public static NormalizedSnapshotSpanCollection GetSnapshotSpanCollection(this ITextSnapshot snapshot)
        {
            Contract.ThrowIfNull(snapshot);

            return new NormalizedSnapshotSpanCollection(snapshot.GetFullSpan());
        }

        public static void GetLineAndColumn(this ITextSnapshot snapshot, int position, out int lineNumber, out int columnIndex)
        {
            var line = snapshot.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            columnIndex = position - line.Start.Position;
        }

        public static bool AreOnSameLine(this ITextSnapshot snapshot, int x1, int x2)
            => snapshot.GetLineNumberFromPosition(x1) == snapshot.GetLineNumberFromPosition(x2);
    }

    internal static class TextSpanExtensions
    {
        /// <summary>
        /// Convert a <see cref="TextSpan"/> instance to a <see cref="TextSpan"/>.
        /// </summary>
        public static Span ToSpan(this TextSpan textSpan)
        {
            return new Span(textSpan.Start, textSpan.Length);
        }

        /// <summary>
        /// Convert a <see cref="TextSpan"/> to a <see cref="SnapshotSpan"/> on the given <see cref="ITextSnapshot"/> instance
        /// </summary>
        public static SnapshotSpan ToSnapshotSpan(this TextSpan textSpan, ITextSnapshot snapshot)
        {
            Debug.Assert(snapshot != null);
            var span = textSpan.ToSpan();
            return new SnapshotSpan(snapshot, span);
        }
    }
}
