// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class ActiveStatementTagger : ITagger<ITextMarkerTag>, IDisposable
    {
        private readonly IActiveStatementTrackingService _trackingService;
        private readonly ITextBuffer _buffer;

        public ActiveStatementTagger(IActiveStatementTrackingService trackingService, ITextBuffer buffer)
        {
            _trackingService = trackingService;
            _trackingService.TrackingSpansChanged += OnTrackingSpansChanged;
            _buffer = buffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void Dispose()
        {
            _trackingService.TrackingSpansChanged -= OnTrackingSpansChanged;
        }

        private void OnTrackingSpansChanged(bool leafChanged)
        {
            var handler = TagsChanged;
            if (handler != null)
            {
                // TODO: call the handler only if the spans affect this buffer
                var snapshot = _buffer.CurrentSnapshot;
                handler(this, new SnapshotSpanEventArgs(snapshot.GetFullSpan()));
            }
        }

        public IEnumerable<ITagSpan<ITextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            var snapshot = spans.First().Snapshot;

            foreach (ActiveStatementTextSpan asSpan in _trackingService.GetSpans(snapshot.AsText()))
            {
                if ((asSpan.Flags & ActiveStatementFlags.LeafFrame) != 0)
                {
                    continue;
                }

                var snapshotSpan = new SnapshotSpan(snapshot, Span.FromBounds(asSpan.Span.Start, asSpan.Span.End));

                if (spans.OverlapsWith(snapshotSpan))
                {
                    yield return new TagSpan<ITextMarkerTag>(snapshotSpan, ActiveStatementTag.Instance);
                }
            }
        }
    }
}
