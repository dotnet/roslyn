using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
            private readonly object gate = new object();

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
                ProduceTagsAsync(context, spanToTag);
                return SpecializedTasks.EmptyTask;
            }

            private void ProduceTagsAsync(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag)
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

                // Producing tags is simple.  We just grab the diagnostics we were already told about,
                // and we convert them to tag spans.
                object id;
                ImmutableArray<DiagnosticData> diagnostics;
                SourceText sourceText;
                ITextSnapshot editorSnapshot;
                lock (gate)
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
                        var actualSpan = diagnosticData
                            .GetExistingOrCalculatedTextSpan(sourceText)
                            .ToSnapshotSpan(editorSnapshot)
                            .TranslateTo(requestedSnapshot, SpanTrackingMode.EdgeExclusive);

                        if (actualSpan.IntersectsWith(requestedSpan))
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

            internal void OnDiagnosticsUpdated(DiagnosticsUpdatedArgs e, SourceText sourceText, ITextSnapshot editorSnapshot)
            {
                // We were told about new diagnostics.  Store them, and then let the 
                // AsynchronousTaggerProvider know it should ProduceTags again.
                lock (gate)
                {
                    _latestId = e.Id;
                    _latestDiagnostics = e.Diagnostics;
                    _latestSourceText = sourceText;
                    _latestEditorSnapshot = editorSnapshot;
                }
                this.Changed?.Invoke(this, new TaggerEventArgs(TaggerDelay.Medium));
            }
        }
    }
}