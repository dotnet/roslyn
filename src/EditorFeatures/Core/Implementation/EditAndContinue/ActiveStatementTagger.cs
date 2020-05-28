// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

        private IActiveStatementTrackingService? _trackingService;

        public event EventHandler<SnapshotSpanEventArgs>? TagsChanged;

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

        private void OnWorkspaceChanged(object? sender, EventArgs e)
            => ConnectToWorkspace(_workspaceRegistration.Workspace);

        private void ConnectToWorkspace(Workspace? workspace)
        {
            var newService = workspace?.Services.GetService<IActiveStatementTrackingService>();
            if (newService != null)
            {
                newService.TrackingSpansChanged += OnTrackingSpansChanged;
            }

            var previousService = Interlocked.Exchange(ref _trackingService, newService);
            if (previousService != null)
            {
                previousService.TrackingSpansChanged -= OnTrackingSpansChanged;
            }
        }

        public void Dispose()
        {
            AssertIsForeground();

            _workspaceRegistration.WorkspaceChanged -= OnWorkspaceChanged;
            ConnectToWorkspace(workspace: null);
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
            AssertIsForeground();

            var service = _trackingService;
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
