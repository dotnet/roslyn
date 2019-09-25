// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attemp to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    internal abstract partial class AbstractAsyncClassificationTaggerProvider :
        AsynchronousViewTaggerProvider<IClassificationTag>
    {
        protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeInclusive;

        protected AbstractAsyncClassificationTaggerProvider(
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider asyncListenerProvider,
            IThreadingContext threadingContext)
                : base(threadingContext, asyncListenerProvider.GetListener(FeatureAttribute.Classification), notificationService)
        {
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<IClassificationTag> context,
            DocumentSnapshotSpan snapshotSpan,
            int? caretPosition)
        {
            // Asked to classify when the document is no longer part of a workspace,
            // this can happen when the document/project is being closed.
            if (snapshotSpan.Document == null)
            {
                return;
            }

            // Classify documents in chunks of 50k.   This allows more important work (like 
            // completion) to pre-empt classification on our semantic thread.  It also keeps
            // the size of the responses that we need to marshal from the script side smaller.

            var document = snapshotSpan.Document;
            var snapshot = snapshotSpan.SnapshotSpan.Snapshot;
            var start = snapshotSpan.SnapshotSpan.Start.Position;
            var end = snapshotSpan.SnapshotSpan.End.Position;
            const int chunkSize = 50 * 1024;

            for (var i = start; i < end; i += chunkSize)
            {
                var subSpan = Span.FromBounds(i, Math.Min(i + chunkSize, end));
                await this.ProduceTagsAsync(
                    context,
                    new DocumentSnapshotSpan(document, new SnapshotSpan(snapshot, subSpan))).ConfigureAwait(false);
            }
        }

        protected abstract Task ProduceTagsAsync(TaggerContext<IClassificationTag> context, DocumentSnapshotSpan snapshotSpan);

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textView, ITextBuffer subjectBuffer)
        {
            if (textView == null)
            {
                var currentSnapshot = subjectBuffer.CurrentSnapshot;
                return new[] { new SnapshotSpan(currentSnapshot, Span.FromBounds(0, currentSnapshot.Length)) };
            }

            // Determine the range of text that is visible in the view.  Then map this down to our
            // specific buffer.  From that, determine the start/end line for our buffer that is in
            // view.  Then grow that a bit on either end (so the user can scroll up/down without
            // noticing any classification) and return as the span we want to tag.

            var visibleSpan = textView.TextViewLines.FormattedSpan;
            var visibleSpansInBuffer = textView.BufferGraph.MapDownToBuffer(visibleSpan, SpanTrackingMode.EdgeInclusive, subjectBuffer);
            var snapshot = subjectBuffer.CurrentSnapshot;
            if (visibleSpansInBuffer.Count == 0)
            {
                // Roslyn expects a non-empty list of snapshot spans so if there
                // are no visible spans in the buffer return the full snapshot.
                // This can occur in an HTML file with script blocks when the user
                // scrolls down and the script blocks are no longer visible in view.
                return new[] { new SnapshotSpan(snapshot, 0, snapshot.Length) };
            }

            var visibleStart = visibleSpansInBuffer.First().Start;
            var visibleEnd = visibleSpansInBuffer.Last().End;

            var startLine = snapshot.GetLineNumberFromPosition(visibleStart);
            var endLine = snapshot.GetLineNumberFromPosition(visibleEnd);

            // Actually classify the region +/- about 50 lines.  Make sure we stay within bounds
            // of the file.
            startLine = Math.Max(startLine - 50, 0);
            endLine = Math.Min(endLine + 50, snapshot.LineCount - 1);

            var start = snapshot.GetLineFromLineNumber(startLine).Start;
            var end = snapshot.GetLineFromLineNumber(endLine).EndIncludingLineBreak;

            var span = new SnapshotSpan(snapshot, Span.FromBounds(start, end));

            return new[] { span };
        }
    }
}
