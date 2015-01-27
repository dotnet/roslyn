// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Services.Editor.Shared.Utilities
{
    [ExcludeFromCodeCoverage]
    internal class TextViewVisibleSpanTracker
    {
        private readonly ITextView textView;
        private readonly int initialThreadId;

        private Dictionary<ITextBuffer, NormalizedSnapshotSpanCollection> bufferToPreviousVisibleSpans = new Dictionary<ITextBuffer, NormalizedSnapshotSpanCollection>();

        public event EventHandler<VisibleSpansChangedEventArgs> VisibleSpansChanged;

        [Obsolete("This method is currently unused and excluded from code coverage. Should you decide to use it, please add a test.")]
        public TextViewVisibleSpanTracker(ITextView textView)
        {
            this.textView = textView;
            this.textView.LayoutChanged += OnTextViewLayoutChanged;
            this.textView.Closed += OnTextViewClosed;

            this.initialThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public void RegisterSubjectBuffer(ITextBuffer subjectBuffer)
        {
            CheckThread();

            if (!bufferToPreviousVisibleSpans.ContainsKey(subjectBuffer))
            {
                this.bufferToPreviousVisibleSpans.Add(subjectBuffer, ComputePossiblyVisibleSnapshotSpans(textView, subjectBuffer.CurrentSnapshot));
            }
        }

        private void OnTextViewClosed(object sender, EventArgs e)
        {
            CheckThread();

            this.textView.LayoutChanged -= OnTextViewLayoutChanged;
            this.textView.Closed -= this.OnTextViewClosed;
        }

        private void OnTextViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            CheckThread();

            var bufferToPreviousVisibleSpansLocal = this.bufferToPreviousVisibleSpans;
            var bufferToCurrentVisibleSpansLocal = new Dictionary<ITextBuffer, NormalizedSnapshotSpanCollection>(bufferToPreviousVisibleSpansLocal.Count);
            foreach (var subjectBuffer in bufferToPreviousVisibleSpansLocal.Keys)
            {
                bufferToCurrentVisibleSpansLocal.Add(subjectBuffer, ComputePossiblyVisibleSnapshotSpans(textView, subjectBuffer.CurrentSnapshot));
            }

            this.bufferToPreviousVisibleSpans = bufferToCurrentVisibleSpansLocal;

            var visibleSpansChanged = VisibleSpansChanged;
            if (visibleSpansChanged == null)
            {
                return;
            }

            foreach (var subjectBuffer in bufferToCurrentVisibleSpansLocal.Keys)
            {
                var previous = bufferToPreviousVisibleSpansLocal[subjectBuffer];
                var current = bufferToCurrentVisibleSpansLocal[subjectBuffer];

                if (previous != current)
                {
#pragma warning disable 618
                    visibleSpansChanged(this, new VisibleSpansChangedEventArgs(subjectBuffer, current));
#pragma warning restore 618
                }
            }
        }

        public NormalizedSnapshotSpanCollection GetPossiblyVisibleSnapshotSpans(ITextSnapshot snapshot)
        {
            CheckThread();

#pragma warning disable 618
            RegisterSubjectBuffer(snapshot.TextBuffer);
#pragma warning restore 618
            var spans = bufferToPreviousVisibleSpans[snapshot.TextBuffer];
            Debug.Assert(!spans.Any() || spans.First().Snapshot == snapshot, "We don't have visible spans for this solution");
            return spans;
        }

        [Conditional("DEBUG")]
        private void CheckThread()
        {
            Debug.Assert(this.initialThreadId == Thread.CurrentThread.ManagedThreadId);
        }

        /// <summary>
        /// Returns a set of all visible spans and potentially some invisible ones.
        /// In a common scenario of view snapshot matching text snapshot with limited amount of hidden text
        /// getting "potential" visible spans could be acceptable cheaper alternative to the more precise GetVisibleSnapshotSpans.
        /// </summary>
        private static NormalizedSnapshotSpanCollection ComputePossiblyVisibleSnapshotSpans(ITextView textView, ITextSnapshot snapshot)
        {
            Debug.Assert(!textView.IsClosed);

            // We may get asked to start tracking a view before any layout has happened.
            if (textView.TextViewLines == null)
            {
                return new NormalizedSnapshotSpanCollection();
            }

            // MapUpTo/DownTo are expensive functions involving multiple allocations, so we are
            // trying to catch the common case here - the view snapshot is the same as the given
            //  snapshot && if there is some hidden text it is not too large compared to the visual
            //  size so precise filtering of visible from not visible may not worth the effort.
            var formattedSpan = textView.TextViewLines.FormattedSpan;
            var formattedLength = formattedSpan.Length;
            var visualLength = textView.VisualSnapshot.Length;

            // TODO: heuristic of what is considered "too much" for potentially invisible text could be tuned further
            // Here we do a simple comparison "text with possibly hidden parts" <= "visible text * 2"
            // For the most part we just need to prevent extreme cases - when collapsed text contains pages and pages of text.
            if (formattedSpan.Snapshot == snapshot && (formattedLength / 2 <= visualLength))
            {
                return new NormalizedSnapshotSpanCollection(formattedSpan);
            }

            return ComputeVisibleSnapshotSpans(textView, snapshot);
        }

        private static NormalizedSnapshotSpanCollection ComputeVisibleSnapshotSpans(ITextView textView, ITextSnapshot snapshot)
        {
            // We don't want to just return this.TextView.TextViewLines.FormattedSpan.  This is
            // because the formatted span may be huge (in the case of collapsed outlining regions).
            // So we instead only pick the lines that are in some way visible on the screen.
            // but we let the editor do the heavy lifting for us - we just map the FormattedSpan up to the visual snapshot,
            // then map that back down which gives us a set of spans that contains only the visible stuff.
            var bufferGraph = textView.BufferGraph;
            var visualSnapshot = textView.VisualSnapshot;
            var visualSpans = bufferGraph.MapUpToSnapshot(textView.TextViewLines.FormattedSpan, SpanTrackingMode.EdgeExclusive, visualSnapshot);
            Debug.Assert(visualSpans.Count == 1, "More than one visual span?");
            var spans = bufferGraph.MapDownToSnapshot(visualSpans.Single(), SpanTrackingMode.EdgeExclusive, snapshot);
            return spans;
        }
    }
}
