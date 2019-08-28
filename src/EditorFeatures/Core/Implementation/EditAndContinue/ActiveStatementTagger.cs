// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal sealed class ActiveStatementTagger : ForegroundThreadAffinitizedObject, ITagger<ITextMarkerTag>, IDisposable
    {
        private readonly WorkspaceRegistration _workspaceRegistration;
        private readonly ITextBuffer _buffer;

        private IActiveStatementTrackingService _trackingServiceOpt;

        public ActiveStatementTagger(IThreadingContext threadingContext, ITextBuffer buffer)
            : base(threadingContext)
        {
            // A buffer can switch between workspaces (from misc files workspace to primary workspace, etc.).
            // The following code handles such transitions.

            _workspaceRegistration = Workspace.GetWorkspaceRegistration(buffer.AsTextContainer());
            ConnectToWorkspace(_workspaceRegistration.Workspace);
            _workspaceRegistration.WorkspaceChanged += OnWorkspaceChanged;

            _buffer = buffer;
        }

        private void OnWorkspaceChanged(object sender, EventArgs e)
            => ConnectToWorkspace(_workspaceRegistration.Workspace);

        private void ConnectToWorkspace(Workspace workspaceOpt)
        {
            var newServiceOpt = workspaceOpt?.Services.GetService<IActiveStatementTrackingService>();
            if (newServiceOpt != null)
            {
                newServiceOpt.TrackingSpansChanged += OnTrackingSpansChanged;
            }

            var previousServiceOpt = Interlocked.Exchange(ref _trackingServiceOpt, newServiceOpt);
            if (previousServiceOpt != null)
            {
                previousServiceOpt.TrackingSpansChanged -= OnTrackingSpansChanged;
            }
        }

        public void Dispose()
        {
            AssertIsForeground();

            _workspaceRegistration.WorkspaceChanged -= OnWorkspaceChanged;
            ConnectToWorkspace(workspaceOpt: null);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

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
            AssertIsForeground();

            var service = _trackingServiceOpt;
            if (service == null)
            {
                yield break;
            }

            var snapshot = spans.First().Snapshot;

            foreach (var asSpan in service.GetSpans(snapshot.AsText()))
            {
                if (asSpan.IsLeaf)
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
