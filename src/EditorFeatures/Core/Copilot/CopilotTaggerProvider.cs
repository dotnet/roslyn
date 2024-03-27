// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Workspaces;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Copilot;

/// <summary>
/// A dummy tagger provider to trigger background Copilot code analysis after waiting
/// for user being idle, no documents edits and sufficient delay.
/// This tagger provider does not produce any tags, but is a convenient way to trigger
/// this background analysis with these guardrails to ensure the analysis is executed
/// very sparingly.
/// TODO: We should throttle the number of background analysis queries that are triggered
/// with an appropriately chosen throttle counter.
/// </summary>
[Export(typeof(IViewTaggerProvider))]
[VisualStudio.Utilities.Name(nameof(CopilotTaggerProvider))]
[VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
[TagType(typeof(ITextMarkerTag))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CopilotTaggerProvider(
    IThreadingContext threadingContext,
    IGlobalOptionService globalOptionService,
    [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
    IAsynchronousOperationListenerProvider listenerProvider)
    : AsynchronousViewTaggerProvider<ITextMarkerTag>(threadingContext, globalOptionService, visibilityTracker, listenerProvider.GetListener(FeatureAttribute.CopilotSuggestions))
{
    protected override TaggerDelay EventChangeDelay => TaggerDelay.OnIdleWithLongDelay;

    protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
    {
        // We want to cancel existing Copilot background analysis with change in caret position,
        // scrolling the active document or text changes to the active document.
        return TaggerEventSources.Compose(
            TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer),
            TaggerEventSources.OnViewSpanChanged(ThreadingContext, textView),
            TaggerEventSources.OnTextChanged(subjectBuffer));
    }

    protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView? textView, ITextBuffer subjectBuffer)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        Contract.ThrowIfNull(textView);

        // We only care about the cases where we have caret.
        if (textView.GetCaretPoint(subjectBuffer) is { } caret)
            return SpecializedCollections.SingletonEnumerable(new SnapshotSpan(caret, 0));

        return SpecializedCollections.EmptyEnumerable<SnapshotSpan>();
    }

    protected override async Task ProduceTagsAsync(TaggerContext<ITextMarkerTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition, CancellationToken cancellationToken)
    {
        if (spanToTag.Document is not { } document
            || document.GetLanguageService<ICopilotCodeAnalysisService>() is not { } service
            || !await service.IsCodeAnalysisOptionEnabledAsync().ConfigureAwait(false))
        {
            return;
        }

        // Fetch the Copilot code analysis prompt titles, each title can define a separate code analysis prompt.
        // Currently, we only support running the primary (first) code analysis prompt.
        var prompts = await service.GetAvailablePromptTitlesAsync(document, cancellationToken).ConfigureAwait(false);
        if (prompts.Length > 0)
        {
            await service.AnalyzeDocumentAsync(document, spanToTag.SnapshotSpan.Span.ToTextSpan(), prompts[0], cancellationToken).ConfigureAwait(false);
        }
    }
}
