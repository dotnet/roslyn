// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ITextViewExtensions
    {
        /// <summary>
        /// Collects the content types in the view's buffer graph.
        /// </summary>
        public static ISet<IContentType> GetContentTypes(this ITextView textView)
        {
            return new HashSet<IContentType>(
                textView.BufferGraph.GetTextBuffers(_ => true).Select(b => b.ContentType));
        }

        public static bool IsReadOnlyOnSurfaceBuffer(this ITextView textView, SnapshotSpan span)
        {
            var spansInView = textView.BufferGraph.MapUpToBuffer(span, SpanTrackingMode.EdgeInclusive, textView.TextBuffer);
            return spansInView.Any(spanInView => textView.TextBuffer.IsReadOnly(spanInView.Span));
        }

        public static SnapshotPoint? GetCaretPoint(this ITextView textView, ITextBuffer subjectBuffer)
        {
            var caret = textView.Caret.Position;
            return textView.BufferGraph.MapUpOrDownToBuffer(caret.BufferPosition, subjectBuffer);
        }

        public static SnapshotPoint? GetCaretPoint(this ITextView textView, Predicate<ITextSnapshot> match)
        {
            var caret = textView.Caret.Position;
            var span = textView.BufferGraph.MapUpOrDownToFirstMatch(new SnapshotSpan(caret.BufferPosition, 0), match);
            if (span.HasValue)
            {
                return span.Value.Start;
            }
            else
            {
                return null;
            }
        }

        public static VirtualSnapshotPoint? GetVirtualCaretPoint(this ITextView textView, ITextBuffer subjectBuffer)
        {
            if (subjectBuffer == textView.TextBuffer)
            {
                return textView.Caret.Position.VirtualBufferPosition;
            }

            var mappedPoint = textView.BufferGraph.MapDownToBuffer(
                textView.Caret.Position.VirtualBufferPosition.Position,
                PointTrackingMode.Negative,
                subjectBuffer,
                PositionAffinity.Predecessor);

            return mappedPoint.HasValue
                ? new VirtualSnapshotPoint(mappedPoint.Value)
                : default;
        }

        public static ITextBuffer GetBufferContainingCaret(this ITextView textView, string contentType = ContentTypeNames.RoslynContentType)
        {
            var point = GetCaretPoint(textView, s => s.ContentType.IsOfType(contentType));
            return point.HasValue ? point.Value.Snapshot.TextBuffer : null;
        }

        public static SnapshotPoint? GetPositionInView(this ITextView textView, SnapshotPoint point)
        {
            return textView.BufferGraph.MapUpToSnapshot(point, PointTrackingMode.Positive, PositionAffinity.Successor, textView.TextSnapshot);
        }

        public static NormalizedSnapshotSpanCollection GetSpanInView(this ITextView textView, SnapshotSpan span)
        {
            return textView.BufferGraph.MapUpToSnapshot(span, SpanTrackingMode.EdgeInclusive, textView.TextSnapshot);
        }

        public static void SetSelection(
            this ITextView textView, VirtualSnapshotPoint anchorPoint, VirtualSnapshotPoint activePoint)
        {
            var isReversed = activePoint < anchorPoint;
            var start = isReversed ? activePoint : anchorPoint;
            var end = isReversed ? anchorPoint : activePoint;
            SetSelection(textView, new SnapshotSpan(start.Position, end.Position), isReversed);
        }

        public static void SetSelection(
            this ITextView textView, SnapshotSpan span, bool isReversed = false)
        {
            var spanInView = textView.GetSpanInView(span).Single();
            textView.Selection.Select(spanInView, isReversed);
            textView.Caret.MoveTo(isReversed ? spanInView.Start : spanInView.End);
        }

        public static bool TryMoveCaretToAndEnsureVisible(this ITextView textView, SnapshotPoint point, IOutliningManagerService outliningManagerService = null, EnsureSpanVisibleOptions ensureSpanVisibleOptions = EnsureSpanVisibleOptions.None)
        {
            return textView.TryMoveCaretToAndEnsureVisible(new VirtualSnapshotPoint(point), outliningManagerService, ensureSpanVisibleOptions);
        }

        public static bool TryMoveCaretToAndEnsureVisible(this ITextView textView, VirtualSnapshotPoint point, IOutliningManagerService outliningManagerService = null, EnsureSpanVisibleOptions ensureSpanVisibleOptions = EnsureSpanVisibleOptions.None)
        {
            if (textView.IsClosed)
            {
                return false;
            }

            var pointInView = textView.GetPositionInView(point.Position);

            if (!pointInView.HasValue)
            {
                return false;
            }

            // If we were given an outlining service, we need to expand any outlines first, or else
            // the Caret.MoveTo won't land in the correct location if our target is inside a
            // collapsed outline.
            if (outliningManagerService != null)
            {
                var outliningManager = outliningManagerService.GetOutliningManager(textView);

                if (outliningManager != null)
                {
                    outliningManager.ExpandAll(new SnapshotSpan(pointInView.Value, length: 0), match: _ => true);
                }
            }

            var newPosition = textView.Caret.MoveTo(new VirtualSnapshotPoint(pointInView.Value, point.VirtualSpaces));

            // We use the caret's position in the view's current snapshot here in case something 
            // changed text in response to a caret move (e.g. line commit)
            var spanInView = new SnapshotSpan(newPosition.BufferPosition, 0);
            textView.ViewScroller.EnsureSpanVisible(spanInView, ensureSpanVisibleOptions);

            return true;
        }

        /// <summary>
        /// Gets or creates a view property that would go away when view gets closed
        /// </summary>
        public static TProperty GetOrCreateAutoClosingProperty<TProperty, TTextView>(
            this TTextView textView,
            Func<TTextView, TProperty> valueCreator) where TTextView : ITextView
        {
            return textView.GetOrCreateAutoClosingProperty(typeof(TProperty), valueCreator);
        }

        /// <summary>
        /// Gets or creates a view property that would go away when view gets closed
        /// </summary>
        public static TProperty GetOrCreateAutoClosingProperty<TProperty, TTextView>(
            this TTextView textView,
            object key,
            Func<TTextView, TProperty> valueCreator) where TTextView : ITextView
        {
            GetOrCreateAutoClosingProperty(textView, key, valueCreator, out var value);
            return value;
        }

        /// <summary>
        /// Gets or creates a view property that would go away when view gets closed
        /// </summary>
        public static bool GetOrCreateAutoClosingProperty<TProperty, TTextView>(
            this TTextView textView,
            object key,
            Func<TTextView, TProperty> valueCreator,
            out TProperty value) where TTextView : ITextView
        {
            return AutoClosingViewProperty<TProperty, TTextView>.GetOrCreateValue(textView, key, valueCreator, out value);
        }

        /// <summary>
        /// Gets or creates a per subject buffer property.
        /// </summary>
        public static TProperty GetOrCreatePerSubjectBufferProperty<TProperty, TTextView>(
            this TTextView textView,
            ITextBuffer subjectBuffer,
            object key,
            Func<TTextView, ITextBuffer, TProperty> valueCreator) where TTextView : class, ITextView
        {
            GetOrCreatePerSubjectBufferProperty(textView, subjectBuffer, key, valueCreator, out var value);

            return value;
        }

        /// <summary>
        /// Gets or creates a per subject buffer property, returning true if it needed to create it.
        /// </summary>
        public static bool GetOrCreatePerSubjectBufferProperty<TProperty, TTextView>(
            this TTextView textView,
            ITextBuffer subjectBuffer,
            object key,
            Func<TTextView, ITextBuffer, TProperty> valueCreator,
            out TProperty value) where TTextView : class, ITextView
        {
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(subjectBuffer);
            Contract.ThrowIfNull(valueCreator);

            return PerSubjectBufferProperty<TProperty, TTextView>.GetOrCreateValue(textView, subjectBuffer, key, valueCreator, out value);
        }

        public static bool TryGetPerSubjectBufferProperty<TProperty, TTextView>(
            this TTextView textView,
            ITextBuffer subjectBuffer,
            object key,
            out TProperty value) where TTextView : class, ITextView
        {
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(subjectBuffer);

            return PerSubjectBufferProperty<TProperty, TTextView>.TryGetValue(textView, subjectBuffer, key, out value);
        }

        public static void AddPerSubjectBufferProperty<TProperty, TTextView>(
            this TTextView textView,
            ITextBuffer subjectBuffer,
            object key,
            TProperty value) where TTextView : class, ITextView
        {
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(subjectBuffer);

            PerSubjectBufferProperty<TProperty, TTextView>.AddValue(textView, subjectBuffer, key, value);
        }

        public static void RemovePerSubjectBufferProperty<TProperty, TTextView>(
            this TTextView textView,
            ITextBuffer subjectBuffer,
            object key) where TTextView : class, ITextView
        {
            Contract.ThrowIfNull(textView);
            Contract.ThrowIfNull(subjectBuffer);

            PerSubjectBufferProperty<TProperty, TTextView>.RemoveValue(textView, subjectBuffer, key);
        }

        public static bool TypeCharWasHandledStrangely(
            this ITextView textView,
            ITextBuffer subjectBuffer,
            char ch)
        {
            var finalCaretPositionOpt = textView.GetCaretPoint(subjectBuffer);
            if (finalCaretPositionOpt == null)
            {
                // Caret moved outside of our buffer.  Don't want to handle this typed character.
                return true;
            }

            var previousPosition = finalCaretPositionOpt.Value.Position - 1;
            var inRange = previousPosition >= 0 && previousPosition < subjectBuffer.CurrentSnapshot.Length;
            if (!inRange)
            {
                // The character before the caret isn't even in the buffer we care about.  Don't
                // handle this.
                return true;
            }

            if (subjectBuffer.CurrentSnapshot[previousPosition] != ch)
            {
                // The character that was typed is not in the buffer at the typed location.  Don't
                // handle this character.
                return true;
            }

            return false;
        }

        public static int? GetDesiredIndentation(this ITextView textView, ISmartIndentationService smartIndentService, ITextSnapshotLine line)
        {
            var pointInView = textView.BufferGraph.MapUpToSnapshot(
                line.Start, PointTrackingMode.Positive, PositionAffinity.Successor, textView.TextSnapshot);

            if (!pointInView.HasValue)
            {
                return null;
            }

            var lineInView = textView.TextSnapshot.GetLineFromPosition(pointInView.Value.Position);
            return smartIndentService.GetDesiredIndentation(textView, lineInView);
        }

        public static bool TryGetSurfaceBufferSpan(
            this ITextView textView,
            VirtualSnapshotSpan virtualSnapshotSpan,
            out VirtualSnapshotSpan surfaceBufferSpan)
        {
            // If we are already on the surface buffer, then there's no reason to attempt mappings
            // as we'll lose virtualness
            if (virtualSnapshotSpan.Snapshot.TextBuffer == textView.TextBuffer)
            {
                surfaceBufferSpan = virtualSnapshotSpan;
                return true;
            }

            // We have to map. We'll lose virtualness in this process because
            // mapping virtual points through projections is poorly defined.
            var targetSpan = textView.BufferGraph.MapUpToSnapshot(
                virtualSnapshotSpan.SnapshotSpan,
                SpanTrackingMode.EdgeExclusive,
                textView.TextSnapshot).FirstOrNull();

            if (targetSpan.HasValue)
            {
                surfaceBufferSpan = new VirtualSnapshotSpan(targetSpan.Value);
                return true;
            }

            surfaceBufferSpan = default;
            return false;
        }

        /// <summary>
        /// Returns the span of the lines in subjectBuffer that is currently visible in the provided
        /// view.  "extraLines" can be provided to get a span that encompasses some number of lines
        /// before and after the actual visible lines.
        /// </summary>
        public static SnapshotSpan? GetVisibleLinesSpan(this ITextView textView, ITextBuffer subjectBuffer, int extraLines = 0)
        {
            // No point in continuing if the text view has been closed.
            if (textView.IsClosed)
            {
                return null;
            }

            // If we're being called while the textview is actually in the middle of a layout, then 
            // we can't proceed.  Much of the text view state is unsafe to access (and will throw).
            if (textView.InLayout)
            {
                return null;
            }

            // During text view initialization the TextViewLines may be null.  In that case we can't
            // get an appropriate visisble span.
            if (textView.TextViewLines == null)
            {
                return null;
            }

            // Determine the range of text that is visible in the view.  Then map this down to the
            // bufffer passed in.  From that, determine the start/end line for the buffer that is in
            // view.
            var visibleSpan = textView.TextViewLines.FormattedSpan;
            var visibleSpansInBuffer = textView.BufferGraph.MapDownToBuffer(visibleSpan, SpanTrackingMode.EdgeInclusive, subjectBuffer);
            if (visibleSpansInBuffer.Count == 0)
            {
                return null;
            }

            var visibleStart = visibleSpansInBuffer.First().Start;
            var visibleEnd = visibleSpansInBuffer.Last().End;

            var snapshot = subjectBuffer.CurrentSnapshot;
            var startLine = visibleStart.GetContainingLine().LineNumber;
            var endLine = visibleEnd.GetContainingLine().LineNumber;

            startLine = Math.Max(startLine - extraLines, 0);
            endLine = Math.Min(endLine + extraLines, snapshot.LineCount - 1);

            var start = snapshot.GetLineFromLineNumber(startLine).Start;
            var end = snapshot.GetLineFromLineNumber(endLine).EndIncludingLineBreak;

            var span = new SnapshotSpan(snapshot, Span.FromBounds(start, end));

            return span;
        }
    }
}
