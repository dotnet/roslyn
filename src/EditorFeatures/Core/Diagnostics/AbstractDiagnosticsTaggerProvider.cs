// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Base type for all taggers that interact with the <see cref="IDiagnosticAnalyzerService"/> and produce tags for
    /// the diagnostics with different UI presentations.
    /// </summary>
    internal abstract partial class AbstractDiagnosticsTaggerProvider<TTag> : AsynchronousTaggerProvider<TTag>
        where TTag : ITag
    {
        private readonly IDiagnosticService _diagnosticService;
        private readonly IDiagnosticAnalyzerService _analyzerService;

        protected AbstractDiagnosticsTaggerProvider(
            IThreadingContext threadingContext,
            IDiagnosticService diagnosticService,
            IDiagnosticAnalyzerService analyzerService,
            IGlobalOptionService globalOptions,
            ITextBufferVisibilityTracker? visibilityTracker,
            IAsynchronousOperationListener listener)
            : base(threadingContext, globalOptions, visibilityTracker, listener)
        {
            _diagnosticService = diagnosticService;
            _analyzerService = analyzerService;
        }

        protected internal abstract bool IsEnabled { get; }
        protected internal abstract bool SupportsDignosticMode(DiagnosticMode mode);
        protected internal abstract bool IncludeDiagnostic(DiagnosticData data);
        protected internal abstract ITagSpan<TTag>? CreateTagSpan(Workspace workspace, SnapshotSpan span, DiagnosticData data);

        protected override TaggerDelay EventChangeDelay => TaggerDelay.Short;
        protected override TaggerDelay AddedTagNotificationDelay => TaggerDelay.OnIdle;

        protected override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
        {
            // OnTextChanged is added for diagnostics in source generated files: it's possible that the analyzer driver
            // executed on content which was produced by a source generator but is not yet reflected in an open text
            // buffer for that generated file. In this case, we need to update the tags after the buffer updates (which
            // triggers a text changed event) to ensure diagnostics are positioned correctly.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer),
                TaggerEventSources.OnDiagnosticsChanged(subjectBuffer, _diagnosticService),
                TaggerEventSources.OnTextChanged(subjectBuffer));
        }

        /// <summary>
        /// Get the <see cref="DiagnosticDataLocation"/> that should have the tag applied to it.
        /// In most cases, this is the <see cref="DiagnosticData.DataLocation"/> but overrides can change it (e.g. unnecessary classifications).
        /// </summary>
        /// <param name="diagnosticData">the diagnostic containing the location(s).</param>
        /// <returns>an array of locations that should have the tag applied.</returns>
        protected internal virtual ImmutableArray<DiagnosticDataLocation> GetLocationsToTag(DiagnosticData diagnosticData)
            => diagnosticData.DataLocation is not null ? ImmutableArray.Create(diagnosticData.DataLocation) : ImmutableArray<DiagnosticDataLocation>.Empty;

        protected override Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition, CancellationToken cancellationToken)
        {
            return ProduceTagsAsync(context, spanToTag, cancellationToken);
        }

        private async Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, CancellationToken cancellationToken)
        {
            if (!this.IsEnabled)
                return;

            var diagnosticMode = GlobalOptions.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode);
            if (!SupportsDignosticMode(diagnosticMode))
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

            var sourceText = snapshot.AsText();

            try
            {
                var diagnostics = await _analyzerService.GetDiagnosticsForSpanAsync(
                    document, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                var requestedSpan = spanToTag.SnapshotSpan;

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

                        var diagnosticSpans = this.GetLocationsToTag(diagnosticData)
                            .Select(loc => loc.UnmappedFileSpan.GetClampedTextSpan(sourceText).ToSnapshotSpan(snapshot));
                        foreach (var diagnosticSpan in diagnosticSpans)
                        {
                            if (diagnosticSpan.IntersectsWith(requestedSpan) && !IsSuppressed(suppressedDiagnosticsSpans, diagnosticSpan))
                            {
                                var tagSpan = this.CreateTagSpan(workspace, diagnosticSpan, diagnosticData);
                                if (tagSpan != null)
                                    context.AddTag(tagSpan);
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
                return;
            }
        }

        private static bool IsSuppressed(NormalizedSnapshotSpanCollection? suppressedSpans, SnapshotSpan span)
            => suppressedSpans != null && suppressedSpans.IntersectsWith(span);
    }
}
