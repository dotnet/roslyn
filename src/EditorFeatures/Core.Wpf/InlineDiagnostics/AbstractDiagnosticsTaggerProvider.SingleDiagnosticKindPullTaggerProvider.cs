// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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
        IDiagnosticAnalyzerService analyzerService,
        IGlobalOptionService globalOptions,
        ITextBufferVisibilityTracker? visibilityTracker,
        IAsynchronousOperationListener listener) : AsynchronousTaggerProvider<TTag>(threadingContext, globalOptions, visibilityTracker, listener)
    {
        private readonly DiagnosticKind _diagnosticKind = diagnosticKind;
        private readonly IDiagnosticAnalyzerService _analyzerService = analyzerService;

        // The following three fields are used to help calculate diagnostic performance for syntax errors upon file open.
        // During TagsChanged notification for syntax errors, VSPlatform will check the buffer's property bag for a 
        // key with name "syntax-squiggle-count". If found, it will determine that there were syntax errors in the document and
        // fire telemetry with timing information.
        //
        // From Roslyn's perspective, we need to put the "syntax-squiggle-count" entry in the property bag directly prior to
        // invoking TagsChanged when these conditions hold:
        // 1) This tagger provides compiler syntax diagnostics and is tagging IErrorTag tags.
        // 2) The diagnostic request yielded at least one appropriate diagnostic.
        // 3) This is in response to the initial pull diagnostic request. This property should only be set when there
        //    is an appropriate diagnostic upon opening the file. If there are no such diagnostics upon file open but one
        //    is later found after modification, then we do *not* add the entry to the property bag.
        private static readonly object s_initialDiagnosticRequestInfoKey = new();
        private const string SyntaxSquiggleCountPropertyName = "syntax-squiggle-count";
        private readonly bool _requiresBeforeTagsChangedNotification = diagnosticKind == DiagnosticKind.CompilerSyntax && typeof(TTag).IsAssignableFrom(typeof(IErrorTag));

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
        {
            // OnTextChanged is added for diagnostics in source generated files: it's possible that the analyzer driver
            // executed on content which was produced by a source generator but is not yet reflected in an open text
            // buffer for that generated file. In this case, we need to update the tags after the buffer updates (which
            // triggers a text changed event) to ensure diagnostics are positioned correctly.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer),
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer),
                TaggerEventSources.OnWorkspaceChanged(subjectBuffer, this.AsyncListener),
                TaggerEventSources.OnTextChanged(subjectBuffer));
        }

        protected sealed override Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition, CancellationToken cancellationToken)
        {
            return ProduceTagsAsync(context, spanToTag, cancellationToken);
        }

        private async Task ProduceTagsAsync(
            TaggerContext<TTag> context, DocumentSnapshotSpan documentSpanToTag, CancellationToken cancellationToken)
        {
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
            buffer.Properties.TryGetProperty(PredefinedPreviewTaggerKeys.SuppressDiagnosticsSpansKey, out suppressedDiagnosticsSpans);

            var sourceText = snapshot.AsText();

            try
            {
                var requestedSpan = documentSpanToTag.SnapshotSpan;

                CacheInitialDiagnosticRequestInfo(snapshot);

                // NOTE: We pass 'includeSuppressedDiagnostics: true' to ensure that IDE0079 (unnecessary suppressions)
                // are flagged and faded in the editor. IDE0079 analyzer requires all source suppressed diagnostics to
                // be provided to it to function correctly.
                var diagnostics = await _analyzerService.GetDiagnosticsForSpanAsync(
                    document,
                    requestedSpan.Span.ToTextSpan(),
                    diagnosticKind: _diagnosticKind,
                    includeSuppressedDiagnostics: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                // Copilot code analysis is a special analyzer that reports semantic correctness
                // issues in user's code. These diagnostics are computed by a special code analysis
                // service in the background. As computing these diagnostics can be expensive,
                // we only add cached Copilot diagnostics here.
                // Note that we consider Copilot diagnostics as special analyzer semantic diagnostics
                // and hence only report them for 'DiagnosticKind.AnalyzerSemantic'.
                if (_diagnosticKind == DiagnosticKind.AnalyzerSemantic)
                {
                    var copilotDiagnostics = await document.GetCachedCopilotDiagnosticsAsync(requestedSpan.Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                    diagnostics = diagnostics.AddRange(copilotDiagnostics);
                }

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

        private void CacheInitialDiagnosticRequestInfo(ITextSnapshot snapshot)
        {
            if (!_requiresBeforeTagsChangedNotification)
                return;

            var properties = snapshot.TextBuffer.Properties;
            if (!properties.ContainsProperty(s_initialDiagnosticRequestInfoKey))
                properties[s_initialDiagnosticRequestInfoKey] = snapshot.Version.VersionNumber;
        }

        protected override void BeforeTagsChanged(ITextSnapshot snapshot)
        {
            if (!_requiresBeforeTagsChangedNotification)
                return;

            var properties = snapshot.TextBuffer.Properties;

            // Verify this is the initial diagnostic result
            if (properties.GetProperty<int>(s_initialDiagnosticRequestInfoKey) != snapshot.Version.VersionNumber)
                return;

            // Verify we haven't already set the property used to determine time taken to first error calculated
            if (properties.ContainsProperty(SyntaxSquiggleCountPropertyName))
                return;

            // Set the property value to -1 indicating there were syntax errors
            properties[SyntaxSquiggleCountPropertyName] = -1;
        }

        private static bool IsSuppressed(NormalizedSnapshotSpanCollection? suppressedSpans, SnapshotSpan span)
            => suppressedSpans != null && suppressedSpans.IntersectsWith(span);
    }
}
