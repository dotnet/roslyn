// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract partial class AbstractAggregatedDiagnosticsTagSource<TTag> : TagSource<TTag> where TTag : ITag
    {
        private readonly DiagnosticService _service;
        private readonly Mode _mode;

        protected AbstractAggregatedDiagnosticsTagSource(
            ITextBuffer subjectBuffer,
            IForegroundNotificationService notificationService,
            DiagnosticService service,
            IAsynchronousOperationListener asyncListener)
                : base(subjectBuffer, notificationService, asyncListener)
        {
            _service = service;
            _mode = GetMode(subjectBuffer);
        }

        private Mode GetMode(ITextBuffer subjectBuffer)
        {
            Workspace workspace;
            if (Workspace.TryGetWorkspace(subjectBuffer.AsTextContainer(), out workspace) && workspace.Kind == WorkspaceKind.Preview)
            {
                return new ReadOnlyMode(this);
            }

            return new InteractiveMode(this);
        }

        protected override void Disconnect()
        {
            base.Disconnect();

            _mode.Disconnect();
        }

        protected abstract int MinimumLength { get; }
        protected abstract bool ShouldInclude(DiagnosticData diagnostic);
        protected abstract TagSpan<TTag> CreateTagSpan(SnapshotSpan span, DiagnosticData diagnostic);

        public override ITagSpanIntervalTree<TTag> GetTagIntervalTreeForBuffer(ITextBuffer buffer)
        {
            if (buffer == this.SubjectBuffer)
            {
                return _mode;
            }

            return null;
        }

        private static SnapshotSpan AdjustSnapshotSpan(SnapshotSpan span, int minimumLength)
        {
            var snapshot = span.Snapshot;

            // new length
            var length = Math.Max(span.Length, minimumLength);

            // make sure start + length is smaller than snapshot.Length and start is >= 0
            var start = Math.Max(0, Math.Min(span.Start, snapshot.Length - length));

            // make sure length is smaller than snapshot.Length which can happen if start == 0
            return new SnapshotSpan(snapshot, start, Math.Min(start + length, snapshot.Length) - start);
        }

        protected override void RecomputeTagsForeground()
        {
            // do nothing, we don't use this.
        }

        private class IntervalIntrospector : IIntervalIntrospector<Data>
        {
            public readonly ITextSnapshot Snapshot;

            public IntervalIntrospector(ITextSnapshot snapshot)
            {
                this.Snapshot = snapshot;
            }

            public int GetStart(Data value)
            {
                return value.TrackingSpan.GetStartPoint(this.Snapshot);
            }

            public int GetLength(Data value)
            {
                return value.TrackingSpan.GetSpan(this.Snapshot).Length;
            }
        }

        private struct Data
        {
            public readonly DiagnosticData Diagnostic;
            public readonly ITrackingSpan TrackingSpan;

            public Data(DiagnosticData diagnostic, ITrackingSpan trackingSpan)
            {
                this.Diagnostic = diagnostic;
                this.TrackingSpan = trackingSpan;
            }

            public bool IsDefault
            {
                get { return this.Diagnostic == null; }
            }

            public SnapshotSpan GetSnapshotSpan(ITextSnapshot snapshot, int minimumLength)
            {
                var span = this.TrackingSpan.GetSpan(snapshot);
                return AdjustSnapshotSpan(span, minimumLength);
            }
        }

        private abstract class Mode : ITagSpanIntervalTree<TTag>
        {
            protected readonly AbstractAggregatedDiagnosticsTagSource<TTag> Owner;

            public Mode(AbstractAggregatedDiagnosticsTagSource<TTag> owner)
            {
                this.Owner = owner;
            }

            public abstract void Disconnect();

            public abstract IList<ITagSpan<TTag>> GetIntersectingSpans(SnapshotSpan snapshotSpan);

            protected DiagnosticService DiagnosticService
            {
                get { return this.Owner._service; }
            }

            protected ITextBuffer SubjectBuffer
            {
                get { return this.Owner.SubjectBuffer; }
            }

            protected IAsynchronousOperationListener Listener
            {
                get { return this.Owner.Listener; }
            }

            protected void RefreshEntireBuffer()
            {
                var snapshot = this.SubjectBuffer.CurrentSnapshot;
                this.Owner.RaiseTagsChanged(this.SubjectBuffer, new NormalizedSnapshotSpanCollection(snapshot, new Span(0, snapshot.Length)));
            }
        }
    }
}
