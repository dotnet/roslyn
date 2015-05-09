// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV1;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract partial class AbstractAggregatedDiagnosticsTagSource<TTag> : TagSource<TTag> where TTag : ITag
    {
        private class DiagnosticsTagSource
        {
            private readonly AbstractAggregatedDiagnosticsTagSource<TTag> _owner;
            private readonly object _id;

            private readonly AsynchronousSerialWorkQueue _workQueue;

            private IntervalTree<Data> _lastDiagnostics;

            public DiagnosticsTagSource(AbstractAggregatedDiagnosticsTagSource<TTag> owner, object id)
            {
                _owner = owner;
                _id = id;

                _workQueue = new AsynchronousSerialWorkQueue(_owner.Listener);
                _lastDiagnostics = IntervalTree<Data>.Empty;
            }

            public void Shutdown()
            {
                _workQueue.CancelCurrentWork();

                ClearExistingTags();
            }

            public void OnDiagnosticsUpdated(DiagnosticsUpdatedArgs e, SourceText text, ITextSnapshot snapshot, int delay)
            {
                // cancel pending work
                _workQueue.CancelCurrentWork();

                // enqueue new work
                var cancellationToken = _workQueue.CancellationToken;
                _workQueue.EnqueueBackgroundWork(() => RecomputeTagsBackground(e, text, snapshot, cancellationToken), "RecomputeTagsBackground", delay, cancellationToken);
            }

            private void RecomputeTagsBackground(DiagnosticsUpdatedArgs e, SourceText text, ITextSnapshot snapshot, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(e.Solution);

                cancellationToken.ThrowIfCancellationRequested();

                // no diagnostics
                if (e.Diagnostics.IsEmpty)
                {
                    ClearExistingTags(snapshot);
                    return;
                }

                // e.Diagnostics is unordered list and the list could contains overlapping elements
                var diagnostics = GetAugmentedDiagnostics(e.Diagnostics, text, snapshot);

                var introspector = new IntervalIntrospector(snapshot);
                var newTree = IntervalTree.Create(introspector, diagnostics);

                // new diagnostics
                if (_lastDiagnostics.IsEmpty())
                {
                    _lastDiagnostics = newTree;
                    RaiseTagsChanged(newTree, snapshot);
                    return;
                }

                // diagnostics changed
                var oldTree = _lastDiagnostics;
                _lastDiagnostics = newTree;

                var spans = new NormalizedSnapshotSpanCollection(Difference(newTree, oldTree, new DiffSpanComparer(snapshot, _owner.MinimumLength)));
                if (spans.Count == 0)
                {
                    // no changes
                    return;
                }

                _owner.RaiseTagsChanged(snapshot.TextBuffer, spans);
            }

            public void AppendIntersectingSpans(int start, int length, IntervalIntrospector introspector, List<ITagSpan<TTag>> list)
            {
                if (_owner.SubjectBuffer != introspector.Snapshot.TextBuffer)
                {
                    // in venus case, buffer comes and goes and tag source might hold onto diagnostics that belong to
                    // old/new buffers which are different than current subject buffer.
                    return;
                }

                var result = _lastDiagnostics.GetIntersectingInOrderIntervals(start, length, introspector);
                if (result.Count == 0)
                {
                    return;
                }

                // only follow minimum length for live diagnostic. otherwise, let it be zero length.
                var minimumLegnth = _id is ISupportLiveUpdate ? _owner.MinimumLength : 0;

                foreach (var data in result)
                {
                    var span = data.GetSnapshotSpan(introspector.Snapshot, minimumLegnth);
                    if (span.Length == 0)
                    {
                        continue;
                    }

                    list.Add(_owner.CreateTagSpan(span, data.Diagnostic));
                }
            }

            private void ClearExistingTags(ITextSnapshot snapshot = null)
            {
                var local = _lastDiagnostics;
                _lastDiagnostics = IntervalTree<Data>.Empty;

                if (local.IsEmpty())
                {
                    return;
                }

                snapshot = snapshot ?? local.First().TrackingSpan.TextBuffer.CurrentSnapshot;
                RaiseTagsChanged(local, snapshot);
            }

            private void RaiseTagsChanged(IntervalTree<Data> tree, ITextSnapshot snapshot)
            {
                if (_owner.SubjectBuffer != snapshot.TextBuffer)
                {
                    return;
                }

                var spans = new NormalizedSnapshotSpanCollection(tree.Select(d => d.GetSnapshotSpan(snapshot, _owner.MinimumLength)));
                if (spans.Count == 0)
                {
                    return;
                }

                _owner.RaiseTagsChanged(snapshot.TextBuffer, spans);
            }

            private IEnumerable<Data> GetAugmentedDiagnostics(ImmutableArray<DiagnosticData> diagnostics, SourceText text, ITextSnapshot snapshot)
            {
                return diagnostics.Where(_owner.ShouldInclude)
                                  .Select(d => new Data(d, snapshot.CreateTrackingSpan(AdjustSpanRange(text, d.GetExistingOrCalculatedTextSpan(text)), SpanTrackingMode.EdgeExclusive)));
            }

            private static Span AdjustSpanRange(SourceText text, TextSpan span)
            {
                // make sure given span is within the range of given text. diagnostic from other source than live analyzer can have 
                // range outside of the given text since those doesn't have a way to track versions properly
                return Span.FromBounds(Math.Min(Math.Max(span.Start, 0), text.Length), Math.Min(Math.Max(span.End, 0), text.Length));
            }

            public class DiffSpanComparer : IDiffSpanComparer<Data>
            {
                private readonly int _minimumLength;
                private readonly ITextSnapshot _snapshot;

                public DiffSpanComparer(ITextSnapshot snapshot, int minimumLength)
                {
                    _snapshot = snapshot;
                    _minimumLength = minimumLength;
                }

                public bool IsDefault(Data data)
                {
                    return data.IsDefault;
                }

                public bool Equals(Data data1, Data data2)
                {
                    return data1.Diagnostic.Id == data2.Diagnostic.Id && data1.Diagnostic.Severity == data2.Diagnostic.Severity;
                }

                public SnapshotSpan GetSpan(Data data)
                {
                    if (data.TrackingSpan.TextBuffer != _snapshot.TextBuffer)
                    {
                        // when two different buffers are compared, we make whole buffer as changed.
                        return new SnapshotSpan(_snapshot, 0, _snapshot.Length);
                    }

                    return data.GetSnapshotSpan(_snapshot, _minimumLength);
                }
            }
        }
    }
}
