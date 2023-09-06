// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract partial class AbstractPushOrPullDiagnosticsTaggerProvider<TTag> where TTag : ITag
{
    private sealed class PushDiagnosticsTaggerProvider : AsynchronousTaggerProvider<TTag>
    {
        private readonly AbstractPushOrPullDiagnosticsTaggerProvider<TTag> _callback;
        private readonly IDiagnosticService _diagnosticService;

        /// <summary>
        /// Keep track of the ITextSnapshot for the open Document that was used when diagnostics were produced for it.
        /// We need that because the DiagnosticService does not keep track of this snapshot (so as to not hold onto a
        /// lot of memory), which means when we query it for diagnostics, we don't know how to map the span of the
        /// diagnostic to the current snapshot we're tagging.
        /// </summary>
        private static readonly ConditionalWeakTable<object, ITextSnapshot> _diagnosticIdToTextSnapshot = new();

        public PushDiagnosticsTaggerProvider(
            AbstractPushOrPullDiagnosticsTaggerProvider<TTag> callback,
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IGlobalOptionService globalOptions,
            ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext, globalOptions, visibilityTracker, asyncListener)
        {
            _callback = callback;
            _diagnosticService = diagnosticService;
            _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedArgs e)
        {
            if (e.Solution == null || e.DocumentId == null)
            {
                return;
            }

            if (_diagnosticIdToTextSnapshot.TryGetValue(e.Id, out var snapshot))
            {
                return;
            }

            var document = e.Solution.GetDocument(e.DocumentId);

            // If we couldn't find a normal document, and all features are enabled for source generated documents,
            // attempt to locate a matching source generated document in the project.
            if (document is null
                && e.Workspace.Services.GetService<IWorkspaceConfigurationService>()?.Options.EnableOpeningSourceGeneratedFiles == true
                && e.Solution.GetProject(e.DocumentId.ProjectId) is { } project)
            {
                var documentId = e.DocumentId;
                document = ThreadingContext.JoinableTaskFactory.Run(() => project.GetSourceGeneratedDocumentAsync(documentId, CancellationToken.None).AsTask());
            }

            // Open documents *should* always have their SourceText available, but we cannot guarantee
            // (i.e. assert) that they do.  That's because we're not on the UI thread here, so there's
            // a small risk that between calling .IsOpen the file may then close, which then would
            // cause TryGetText to fail.  However, that's ok.  In that case, if we do need to tag this
            // document, we'll just use the current editor snapshot.  If that's the same, then the tags
            // will be hte same.  If it is different, we'll eventually hear about the new diagnostics 
            // for it and we'll reach our fixed point.
            if (document != null && document.IsOpen())
            {
                // This should always be fast since the document is open.
                var sourceText = document.State.GetTextSynchronously(cancellationToken: default);
                snapshot = sourceText.FindCorrespondingEditorTextSnapshot();
                if (snapshot != null)
                {
                    _diagnosticIdToTextSnapshot.GetValue(e.Id, _ => snapshot);
                }
            }
        }

        protected sealed override TaggerDelay EventChangeDelay => TaggerDelay.Short;
        protected sealed override TaggerDelay AddedTagNotificationDelay => TaggerDelay.OnIdle;

        protected sealed override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
            => CreateEventSourceWorker(subjectBuffer, _diagnosticService);

        protected override Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition, CancellationToken cancellationToken)
        {
            return ProduceTagsAsync(context, spanToTag, cancellationToken);
        }

        private async Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, CancellationToken cancellationToken)
        {
            if (!_callback.IsEnabled)
                return;

            var diagnosticMode = GlobalOptions.GetDiagnosticMode();
            if (!_callback.SupportsDiagnosticMode(diagnosticMode))
                return;

            var document = spanToTag.Document;
            if (document == null)
                return;

            var snapshot = spanToTag.SnapshotSpan.Snapshot;
            var workspace = document.Project.Solution.Workspace;

            // See if we've marked any spans as those we want to suppress diagnostics for.
            // This can happen for buffers used in the preview workspace where some feature
            // is generating code that it doesn't want errors shown for.
            var buffer = snapshot.TextBuffer;
            var suppressedDiagnosticsSpans = (NormalizedSnapshotSpanCollection?)null;
            buffer?.Properties.TryGetProperty(PredefinedPreviewTaggerKeys.SuppressDiagnosticsSpansKey, out suppressedDiagnosticsSpans);

            var buckets = _diagnosticService.GetDiagnosticBuckets(
                workspace, document.Project.Id, document.Id, cancellationToken);

            foreach (var bucket in buckets)
            {
                await ProduceTagsAsync(
                    context, spanToTag, workspace, document, suppressedDiagnosticsSpans, bucket, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProduceTagsAsync(
            TaggerContext<TTag> context,
            DocumentSnapshotSpan spanToTag,
            Workspace workspace,
            Document document,
            NormalizedSnapshotSpanCollection? suppressedDiagnosticsSpans,
            DiagnosticBucket bucket,
            CancellationToken cancellationToken)
        {
            try
            {
                var diagnosticMode = GlobalOptions.GetDiagnosticMode();

                var id = bucket.Id;
                var diagnostics = await _diagnosticService.GetDiagnosticsAsync(
                    workspace, document.Project.Id, document.Id, id,
                    includeSuppressedDiagnostics: false,
                    cancellationToken).ConfigureAwait(false);

                var isLiveUpdate = id is ISupportLiveUpdate;

                var requestedSpan = spanToTag.SnapshotSpan;
                var editorSnapshot = requestedSpan.Snapshot;

                // Try to get the text snapshot that these diagnostics were created against.
                // This may fail if this tagger was created *after* the notification for the
                // diagnostics was already issued.  That's ok.  We'll take the spans as reported
                // and apply them directly to the snapshot we have.  Either no new changes will
                // have happened, and these spans will be accurate, or a change will happen
                // and we'll hear about and it update the spans shortly to the right position.
                //
                // Also, only use the diagnoticSnapshot if its text buffer matches our.  The text
                // buffer might be different if the file was closed/reopened.
                // Note: when this happens, the diagnostic service will reanalyze the file.  So
                // up to date diagnostic spans will appear shortly after this.
                _diagnosticIdToTextSnapshot.TryGetValue(id, out var diagnosticSnapshot);
                diagnosticSnapshot = diagnosticSnapshot?.TextBuffer == editorSnapshot.TextBuffer
                    ? diagnosticSnapshot
                    : editorSnapshot;

                var sourceText = diagnosticSnapshot.AsText();

                foreach (var diagnosticData in diagnostics)
                {
                    if (_callback.IncludeDiagnostic(diagnosticData))
                    {
                        // We're going to be retrieving the diagnostics against the last time the engine
                        // computed them against this document *id*.  That might have been a different
                        // version of the document vs what we're looking at now.  But that's ok:
                        // 
                        // 1) GetExistingOrCalculatedTextSpan will ensure that the diagnostics spans are
                        //    contained within 'editorSnapshot'.
                        // 2) We'll eventually hear about an update to the diagnostics for this document
                        //    for whatever edits happened between the last time and this current snapshot.
                        //    So we'll eventually reach a point where the diagnostics exactly match the
                        //    editorSnapshot.

                        var diagnosticSpans = _callback.GetLocationsToTag(diagnosticData)
                            .Select(location => GetDiagnosticSnapshotSpan(location, diagnosticSnapshot, editorSnapshot, sourceText));
                        foreach (var diagnosticSpan in diagnosticSpans)
                        {
                            if (diagnosticSpan.IntersectsWith(requestedSpan) && !IsSuppressed(suppressedDiagnosticsSpans, diagnosticSpan))
                            {
                                var tagSpan = _callback.CreateTagSpan(workspace, isLiveUpdate, diagnosticSpan, diagnosticData);
                                if (tagSpan != null)
                                {
                                    context.AddTag(tagSpan);
                                }
                            }
                        }
                    }
                }
            }
            catch (ArgumentOutOfRangeException ex) when (FatalError.ReportAndCatch(ex))
            {
                // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=428328&_a=edit&triage=false
                // explicitly report NFW to find out what is causing us for out of range. stop crashing on such
                // occasions
                // explicitly report NFW to find out what is causing us for out of range.
                // stop crashing on such occasions
                return;
            }

            static SnapshotSpan GetDiagnosticSnapshotSpan(DiagnosticDataLocation diagnosticDataLocation, ITextSnapshot diagnosticSnapshot,
                ITextSnapshot editorSnapshot, SourceText sourceText)
            {
                return diagnosticDataLocation.UnmappedFileSpan.GetClampedTextSpan(sourceText)
                    .ToSnapshotSpan(diagnosticSnapshot)
                    .TranslateTo(editorSnapshot, SpanTrackingMode.EdgeExclusive);
            }
        }

        private static bool IsSuppressed(NormalizedSnapshotSpanCollection? suppressedSpans, SnapshotSpan span)
            => suppressedSpans != null && suppressedSpans.IntersectsWith(span);
    }
}
