// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal sealed partial class RenameTrackingTaggerProvider
    {
        private class Tagger : ITagger<RenameTrackingTag>, ITagger<IErrorTag>, IDisposable
        {
            private readonly StateMachine _stateMachine;

            public event EventHandler<SnapshotSpanEventArgs> TagsChanged = delegate { };

            public Tagger(StateMachine stateMachine)
            {
                _stateMachine = stateMachine;
                _stateMachine.Connect();
                _stateMachine.TrackingSessionUpdated += StateMachine_TrackingSessionUpdated;
                _stateMachine.TrackingSessionCleared += StateMachine_TrackingSessionCleared;
            }

            private void StateMachine_TrackingSessionCleared(ITrackingSpan trackingSpanToClear)
                => TagsChanged(this, new SnapshotSpanEventArgs(trackingSpanToClear.GetSpan(_stateMachine.Buffer.CurrentSnapshot)));

            private void StateMachine_TrackingSessionUpdated()
            {
                if (_stateMachine.TrackingSession != null)
                {
                    TagsChanged(this, new SnapshotSpanEventArgs(_stateMachine.TrackingSession.TrackingSpan.GetSpan(_stateMachine.Buffer.CurrentSnapshot)));
                }
            }

            public IEnumerable<ITagSpan<RenameTrackingTag>> GetTags(NormalizedSnapshotSpanCollection spans)
                => GetTags(spans, RenameTrackingTag.Instance);

            IEnumerable<ITagSpan<IErrorTag>> ITagger<IErrorTag>.GetTags(NormalizedSnapshotSpanCollection spans)
                => GetTags(spans, new ErrorTag(PredefinedErrorTypeNames.Suggestion));

            private IEnumerable<ITagSpan<T>> GetTags<T>(NormalizedSnapshotSpanCollection spans, T tag) where T : ITag
            {
                if (!_stateMachine.GlobalOptions.GetOption(InternalFeatureOnOffOptions.RenameTracking))
                {
                    // Changes aren't being triggered by the buffer, but there may still be taggers
                    // out there which we should prevent from doing work.
                    yield break;
                }

                if (_stateMachine.CanInvokeRename(out var trackingSession, isSmartTagCheck: true))
                {
                    foreach (var span in spans)
                    {
                        var snapshotSpan = trackingSession.TrackingSpan.GetSpan(span.Snapshot);
                        if (span.IntersectsWith(snapshotSpan))
                        {
                            yield return new TagSpan<T>(snapshotSpan, tag);
                        }
                    }
                }
            }

            public void Dispose()
            {
                _stateMachine.TrackingSessionUpdated -= StateMachine_TrackingSessionUpdated;
                _stateMachine.TrackingSessionCleared -= StateMachine_TrackingSessionCleared;
                _stateMachine.Disconnect();
            }
        }
    }
}
