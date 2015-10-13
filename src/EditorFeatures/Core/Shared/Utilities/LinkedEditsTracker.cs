// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal class LinkedEditsTracker
    {
        private static readonly object s_propagateSpansEditTag = new object();

        private readonly ITextBuffer _subjectBuffer;

        /// <summary>
        /// The list of active tracking spans.
        /// </summary>
        private readonly List<ITrackingSpan> _trackingSpans;

        public LinkedEditsTracker(ITextBuffer subjectBuffer)
        {
            Contract.ThrowIfNull(subjectBuffer);

            _trackingSpans = new List<ITrackingSpan>();
            _subjectBuffer = subjectBuffer;
        }

        public IList<SnapshotSpan> GetActiveSpansForSnapshot(ITextSnapshot snapshot)
        {
            return _trackingSpans.Select(ts => ts.GetSpan(snapshot)).ToList();
        }

        public void AddSpans(IEnumerable<ITrackingSpan> spans)
        {
            var currentActiveSpans = GetActiveSpansForSnapshot(_subjectBuffer.CurrentSnapshot).ToSet();

            foreach (var newTrackingSpan in spans)
            {
                if (!currentActiveSpans.Contains(newTrackingSpan.GetSpan(_subjectBuffer.CurrentSnapshot)))
                {
                    _trackingSpans.Add(newTrackingSpan);
                }
            }
        }

        public void AddSpans(NormalizedSnapshotSpanCollection snapshotSpanCollection)
        {
            // TODO: custom tracking spans!
            var newTrackingSpans = snapshotSpanCollection.Select(ss => ss.Snapshot.CreateTrackingSpan(ss, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Forward));
            AddSpans(newTrackingSpans);
        }

        public bool MyOwnChanges(TextContentChangedEventArgs args)
        {
            return args.EditTag == s_propagateSpansEditTag;
        }

        public bool TryGetTextChanged(TextContentChangedEventArgs args, out string replacementText)
        {
            // make sure I am not called with my own changes
            Contract.ThrowIfTrue(this.MyOwnChanges(args));

            // initialize out parameter
            replacementText = null;

            var trackingSpansAfterEdit = GetActiveSpansForSnapshot(args.After).Select(ss => (Span)ss).ToList();
            var normalizedTrackingSpansAfterEdit = new NormalizedSpanCollection(trackingSpansAfterEdit);

            if (trackingSpansAfterEdit.Count != normalizedTrackingSpansAfterEdit.Count)
            {
                // Because of this edit, some spans merged together. We'll abort
                return false;
            }

            // We want to find the single tracking span that encompasses all the edits made.  If there 
            // is no such tracking span (or there are multiple), then we consider the change invalid
            // and we don't return any changed text.  If there is only one, then we find the text in
            // the new document.
            //
            // Note there may be multiple intersecting spans in the case where user typing causes 
            // multiple edits to happen.  For example, if the user has "Sub" and replaces it with "fu<tab>"
            // Then there will be multiple edits due to the text change and then the case correction.
            // However, both edits will be encompassed in one tracking span.
            var spansTouchedInEdit = new NormalizedSpanCollection(args.Changes.Select(c => c.NewSpan));
            var intersection = NormalizedSpanCollection.Intersection(normalizedTrackingSpansAfterEdit, spansTouchedInEdit);

            var query = from trackingSpan in _trackingSpans
                        let mappedSpan = trackingSpan.GetSpan(args.After)
                        where intersection.All(intersectionSpan => mappedSpan.IntersectsWith(intersectionSpan))
                        select trackingSpan;

            var trackingSpansThatIntersect = query.ToList();
            if (trackingSpansThatIntersect.Count != 1)
            {
                return false;
            }

            var singleIntersectingTrackingSpan = trackingSpansThatIntersect.Single();
            replacementText = singleIntersectingTrackingSpan.GetText(args.After);
            return true;
        }

        public void ApplyReplacementText(string replacementText)
        {
            using (var edit = _subjectBuffer.CreateEdit(new EditOptions(), null, s_propagateSpansEditTag))
            {
                foreach (var span in _trackingSpans)
                {
                    if (span.GetText(_subjectBuffer.CurrentSnapshot) != replacementText)
                    {
                        edit.Replace(span.GetSpan(_subjectBuffer.CurrentSnapshot), replacementText);
                    }
                }

                edit.Apply();
            }
        }
    }
}
