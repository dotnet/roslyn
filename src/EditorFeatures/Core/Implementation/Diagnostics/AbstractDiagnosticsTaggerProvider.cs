// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

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

        protected AbstractDiagnosticsTaggerProvider(
            IDiagnosticService diagnosticService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListener listener)
            : base(listener, notificationService)
        {
            _diagnosticService = diagnosticService;
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
            return SpecializedTasks.EmptyTask;
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

            var sourceText = editorSnapshot.AsText();
            foreach (var updateArg in eventArgs)
            {
                ProduceTags(
                    context, spanToTag, workspace, document, sourceText,
                    suppressedDiagnosticsSpans, updateArg, cancellationToken);
            }
        }

        private void ProduceTags(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag,
            Workspace workspace, Document document, SourceText sourceText, 
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
                                                           .ToSnapshotSpan(editorSnapshot);

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
