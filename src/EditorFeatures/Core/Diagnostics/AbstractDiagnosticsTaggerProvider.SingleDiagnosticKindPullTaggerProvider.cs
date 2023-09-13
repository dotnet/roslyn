// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
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

namespace Microsoft.CodeAnalysis.Diagnostics;

internal abstract partial class AbstractDiagnosticsTaggerProvider<TTag>
{
    /// <summary>
    /// Low level tagger responsible for producing specific diagnostics tags for some feature for some particular <see
    /// cref="DiagnosticKind"/>.  It is itself never exported directly, but it it is used by the <see
    /// cref="AbstractDiagnosticsTaggerProvider{TTag}"/> which aggregates its results and the results for all the other <see
    /// cref="DiagnosticKind"/> to produce all the diagnostics for that feature.
    /// </summary>
    private sealed class SingleDiagnosticKindPullTaggerProvider(
        AbstractDiagnosticsTaggerProvider<TTag> callback,
        DiagnosticKind diagnosticKind,
        IThreadingContext threadingContext,
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListener listener) : AsynchronousTaggerProvider<TTag>(threadingContext, globalOptions, visibilityTracker, listener)
    {
        private readonly DiagnosticKind _diagnosticKind = diagnosticKind;
        private readonly IDiagnosticService _diagnosticService = diagnosticService;
        private readonly IDiagnosticAnalyzerService _analyzerService = analyzerService;

        private readonly AbstractDiagnosticsTaggerProvider<TTag> _callback = callback;

        protected override ImmutableArray<IOption2> Options => _callback.Options;

        protected sealed override TaggerDelay EventChangeDelay => TaggerDelay.Short;
        protected sealed override TaggerDelay AddedTagNotificationDelay => TaggerDelay.OnIdle;

        /// <summary>
        /// When we hear about a new event cancel the costly work we're doing and compute against the latest snapshot.
        /// </summary>
        protected sealed override bool CancelOnNewWork => true;

        protected sealed override bool TagEquals(TTag tag1, TTag tag2)
            => _callback.TagEquals(tag1, tag2);

        protected sealed override ITaggerEventSource CreateEventSource(ITextView? textView, ITextBuffer subjectBuffer)
            => CreateEventSourceWorker(subjectBuffer, _diagnosticService);

        protected sealed override Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition, CancellationToken cancellationToken)
        {
            return ProduceTagsAsync(context, spanToTag, cancellationToken);
        }

        private async Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan documentSpanToTag, CancellationToken cancellationToken)
        {
            if (!_callback.IsEnabled)
                return;

            var diagnosticMode = GlobalOptions.GetDiagnosticMode();
            if (!_callback.SupportsDiagnosticMode(diagnosticMode))
                return;

            var document = documentSpanToTag.Document;
            if (document == null)
                return;

            var snapshot = documentSpanToTag.SnapshotSpan.Snapshot;

            var project = document.Project;
            var workspace = project.Solution.Workspace;

            // See if we've marked any spans as those we want to suppress diagnostics for.
            // This can happen for buffers used in the preview workspace where some feature
            // is generating code that it doesn't want errors shown for.
            var buffer = snapshot.TextBuffer;
            var suppressedDiagnosticsSpans = (NormalizedSnapshotSpanCollection?)null;
            buffer?.Properties.TryGetProperty(PredefinedPreviewTaggerKeys.SuppressDiagnosticsSpansKey, out suppressedDiagnosticsSpans);

            var sourceText = snapshot.AsText();

            try
            {
                var requestedSpan = documentSpanToTag.SnapshotSpan;

                // NOTE: We pass 'includeSuppressedDiagnostics: true' to ensure that IDE0079 (unnecessary suppressions)
                // are flagged and faded in the editor. IDE0079 analyzer requires all source suppressed diagnostics to
                // be provided to it to function correctly.
                var diagnostics = await _analyzerService.GetDiagnosticsForSpanAsync(
                    document,
                    requestedSpan.Span.ToTextSpan(),
                    diagnosticKind: _diagnosticKind,
                    includeSuppressedDiagnostics: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var diagnosticData in diagnostics)
                {
                    if (_callback.IncludeDiagnostic(diagnosticData) && !diagnosticData.IsSuppressed)
                    {
                        var diagnosticSpans = _callback.GetLocationsToTag(diagnosticData)
                            .Select(loc => loc.UnmappedFileSpan.GetClampedTextSpan(sourceText).ToSnapshotSpan(snapshot));
                        foreach (var diagnosticSpan in diagnosticSpans)
                        {
                            if (diagnosticSpan.IntersectsWith(requestedSpan) && !IsSuppressed(suppressedDiagnosticsSpans, diagnosticSpan))
                            {
                                var tagSpan = _callback.CreateTagSpan(workspace, diagnosticSpan, diagnosticData);
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
