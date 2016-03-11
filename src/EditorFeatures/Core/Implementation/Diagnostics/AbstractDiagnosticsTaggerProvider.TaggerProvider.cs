// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    internal abstract partial class AbstractDiagnosticsTaggerProvider<TTag>
    {
        /// <summary>
        /// we create an instance of this async tagger provider for each diagnostic source
        /// we hear about for a particular buffer.  Each async tagger is then responsible
        /// for asynchronous producing tags for that diagnostic source.  This allows each 
        /// individual async tagger to collect diagnostics, diff them against the last set
        /// produced by that diagnostic source, and then notify any interested parties about
        /// what changed.
        /// </summary>
        private class TaggerProvider : AsynchronousTaggerProvider<TTag>, ITaggerEventSource
        {
            private readonly AbstractDiagnosticsTaggerProvider<TTag> _owner;
            private readonly object _gate = new object();

            // The latest diagnostics we've head about for this 
            private object _latestId;
            private ImmutableArray<DiagnosticData> _latestDiagnostics;
            private ITextSnapshot _latestEditorSnapshot;
            private SourceText _latestSourceText;

            protected override IEnumerable<Option<bool>> Options => _owner.Options;

            public TaggerProvider(AbstractDiagnosticsTaggerProvider<TTag> owner)
                : base(owner._listener, owner._notificationService)
            {
                _owner = owner;
            }

            public event EventHandler<TaggerEventArgs> Changed;
            event EventHandler ITaggerEventSource.UIUpdatesPaused { add { } remove { } }
            event EventHandler ITaggerEventSource.UIUpdatesResumed { add { } remove { } }

            void ITaggerEventSource.Connect() { }
            void ITaggerEventSource.Disconnect() { }

            // we will show new tags to users very slowly. 
            // don't confused this with data changed event which is for tag producer (which is set to NearImmediate).
            // this delay is for letting editor know about newly added tags.
            protected override TaggerDelay AddedTagNotificationDelay => TaggerDelay.OnIdle;

            protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
            {
                // We act as a source of events ourselves.  When the diagnostics service tells
                // us about new diagnostics, we'll use that to kick of the asynchronous tagging
                // work.
                return TaggerEventSources.Compose(
                    TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer, TaggerDelay.Medium),
                    this);
            }

            protected override Task ProduceTagsAsync(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
            {
                ProduceTags(context, spanToTag);
                return SpecializedTasks.EmptyTask;
            }

            private void ProduceTags(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag)
            {
                if (!_owner.IsEnabled)
                {
                    return;
                }

                var document = spanToTag.Document;
                if (document == null)
                {
                    return;
                }

                // See if we've marked any spans as those we want to suppress diagnostics for.
                // This can happen for buffers used in the preview workspace where some feature
                // is generating code that it doesn't want errors shown for.
                var buffer = spanToTag.SnapshotSpan.Snapshot.TextBuffer;
                NormalizedSnapshotSpanCollection suppressedDiagnosticsSpans = null;
                buffer?.Properties.TryGetProperty(PredefinedPreviewTaggerKeys.SuppressDiagnosticsSpansKey, out suppressedDiagnosticsSpans);

                // Producing tags is simple.  We just grab the diagnostics we were already told about,
                // and we convert them to tag spans.
                object id;
                ImmutableArray<DiagnosticData> diagnostics;
                SourceText sourceText;
                ITextSnapshot editorSnapshot;
                lock (_gate)
                {
                    id = _latestId;
                    diagnostics = _latestDiagnostics;
                    sourceText = _latestSourceText;
                    editorSnapshot = _latestEditorSnapshot;
                }

                if (sourceText == null || editorSnapshot == null)
                {
                    return;
                }

                var project = document.Project;

                var requestedSnapshot = spanToTag.SnapshotSpan.Snapshot;
                var requestedSpan = spanToTag.SnapshotSpan;
                var isLiveUpdate = id is ISupportLiveUpdate;

                foreach (var diagnosticData in diagnostics)
                {
                    if (_owner.IncludeDiagnostic(diagnosticData))
                    {
                        var actualSpan = AdjustSpan(diagnosticData.GetExistingOrCalculatedTextSpan(sourceText), sourceText)
                            .ToSnapshotSpan(editorSnapshot)
                            .TranslateTo(requestedSnapshot, SpanTrackingMode.EdgeExclusive);

                        if (actualSpan.IntersectsWith(requestedSpan) &&
                            !IsSuppressed(suppressedDiagnosticsSpans, actualSpan))
                        {
                            var tagSpan = _owner.CreateTagSpan(isLiveUpdate, actualSpan, diagnosticData);
                            if (tagSpan != null)
                            {
                                context.AddTag(tagSpan);
                            }
                        }
                    }
                }
            }

            private TextSpan AdjustSpan(TextSpan span, SourceText text)
            {
                var start = Math.Max(0, Math.Min(span.Start, text.Length));
                var end = Math.Max(0, Math.Min(span.End, text.Length));

                if (start > end)
                {
                    var temp = end;
                    end = start;
                    start = temp;
                }

                return TextSpan.FromBounds(start, end);
            }

            private bool IsSuppressed(NormalizedSnapshotSpanCollection suppressedSpans, SnapshotSpan span)
            {
                return suppressedSpans != null && suppressedSpans.IntersectsWith(span);
            }

            internal void OnDiagnosticsUpdated(DiagnosticsUpdatedArgs e, SourceText sourceText, ITextSnapshot editorSnapshot)
            {
                // We were told about new diagnostics.  Store them, and then let the 
                // AsynchronousTaggerProvider know it should ProduceTags again.
                lock (_gate)
                {
                    _latestId = e.Id;
                    _latestDiagnostics = e.Diagnostics;
                    _latestSourceText = sourceText;
                    _latestEditorSnapshot = editorSnapshot;
                }

                // unlike any other tagger, actual work to produce data is done by other service rather than tag provider itself.
                // so we don't need to do any big delay for diagnostic tagger (producer) to reduce doing expensive work repeatedly. that is already
                // taken cared by the external service (diagnostic service).
                this.Changed?.Invoke(this, new TaggerEventArgs(TaggerDelay.NearImmediate));
            }
        }
    }
}