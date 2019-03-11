// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
{
    /// <summary>
    /// Diagnostics works slightly differently than the rest of the taggers.  For diagnostics,
    /// we want to try to have an individual tagger per diagnostic producer per buffer.  
    /// However, the editor only allows a single tagger provider per buffer.  So in order to
    /// get the abstraction we want, we create one outer tagger provider that is associated
    /// with the buffer.  Then, under the covers, we create individual async taggers for each
    /// diagnostic producer we hear about for that buffer.   
    /// 
    /// In essence, we have one tagger that wraps a multitude of taggers it delegates to.
    /// Each of these taggers is nicely asynchronous and properly works within the async
    /// tagging infrastructure. 
    /// </summary>
    internal abstract partial class AbstractDiagnosticsTaggerProvider<TTag> : AsynchronousTaggerProvider<TTag>
        where TTag : ITag
    {
        private readonly IDiagnosticService _diagnosticService;

        /// <summary>
        /// Keep track of the ITextSnapshot for the open Document that was used when diagnostics were
        /// produced for it.  We need that because the DiagnoticService does not keep track of this
        /// snapshot (so as to not hold onto a lot of memory), which means when we query it for 
        /// diagnostics, we don't know how to map the span of the diagnostic to the current snapshot
        /// we're tagging.
        /// </summary>
        private static readonly ConditionalWeakTable<object, ITextSnapshot> _diagnosticIdToTextSnapshot =
            new ConditionalWeakTable<object, ITextSnapshot>();

        protected AbstractDiagnosticsTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListener listener)
            : base(threadingContext, listener, notificationService)
        {
            _diagnosticService = diagnosticService;
            _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
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

        protected override TaggerDelay AddedTagNotificationDelay => TaggerDelay.OnIdle;

        protected override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Medium),
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer, TaggerDelay.Medium),
                TaggerEventSources.OnDiagnosticsChanged(subjectBuffer, _diagnosticService, TaggerDelay.Short));
        }

        protected internal abstract bool IsEnabled { get; }
        protected internal abstract bool IncludeDiagnostic(DiagnosticData data);
        protected internal abstract ITagSpan<TTag> CreateTagSpan(bool isLiveUpdate, SnapshotSpan span, DiagnosticData data);

        protected override Task ProduceTagsAsync(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition)
        {
            ProduceTags(context, spanToTag);
            return Task.CompletedTask;
        }

        private void ProduceTags(TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag)
        {
            if (!this.IsEnabled)
            {
                return;
            }

            var document = spanToTag.Document;
            if (document == null)
            {
                return;
            }

            var editorSnapshot = spanToTag.SnapshotSpan.Snapshot;

            var cancellationToken = context.CancellationToken;
            var workspace = document.Project.Solution.Workspace;

            // See if we've marked any spans as those we want to suppress diagnostics for.
            // This can happen for buffers used in the preview workspace where some feature
            // is generating code that it doesn't want errors shown for.
            var buffer = editorSnapshot.TextBuffer;
            var suppressedDiagnosticsSpans = default(NormalizedSnapshotSpanCollection);
            buffer?.Properties.TryGetProperty(PredefinedPreviewTaggerKeys.SuppressDiagnosticsSpansKey, out suppressedDiagnosticsSpans);

            var eventArgs = _diagnosticService.GetDiagnosticsUpdatedEventArgs(
                workspace, document.Project.Id, document.Id, context.CancellationToken);

            foreach (var updateArg in eventArgs)
            {
                ProduceTags(
                    context, spanToTag, workspace, document,
                    suppressedDiagnosticsSpans, updateArg, cancellationToken);
            }
        }

        private void ProduceTags(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag,
            Workspace workspace, Document document,
            NormalizedSnapshotSpanCollection suppressedDiagnosticsSpans,
            UpdatedEventArgs updateArgs, CancellationToken cancellationToken)
        {
            try
            {
                var id = updateArgs.Id;
                var diagnostics = _diagnosticService.GetDiagnostics(
                    workspace, document.Project.Id, document.Id, id, false, cancellationToken);

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
                    if (this.IncludeDiagnostic(diagnosticData))
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

                        var diagnosticSpan = diagnosticData.GetExistingOrCalculatedTextSpan(sourceText)
                                                           .ToSnapshotSpan(diagnosticSnapshot)
                                                           .TranslateTo(editorSnapshot, SpanTrackingMode.EdgeExclusive);

                        if (diagnosticSpan.IntersectsWith(requestedSpan) &&
                            !IsSuppressed(suppressedDiagnosticsSpans, diagnosticSpan))
                        {
                            var tagSpan = this.CreateTagSpan(isLiveUpdate, diagnosticSpan, diagnosticData);
                            if (tagSpan != null)
                            {
                                context.AddTag(tagSpan);
                            }
                        }
                    }
                }
            }
            catch (ArgumentOutOfRangeException ex) when (FatalError.ReportWithoutCrash(ex))
            {
                // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=428328&_a=edit&triage=false
                // explicitly report NFW to find out what is causing us for out of range.
                // stop crashing on such occations
                return;
            }
        }

        private bool IsSuppressed(NormalizedSnapshotSpanCollection suppressedSpans, SnapshotSpan span)
            => suppressedSpans != null && suppressedSpans.IntersectsWith(span);
    }
}
