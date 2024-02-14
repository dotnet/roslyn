// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.Copilot;

[Export(typeof(IViewTaggerProvider))]
[VisualStudio.Utilities.Name(nameof(CSharpCopilotTaggerProvider))]
[VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
[TagType(typeof(ITextMarkerTag))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpCopilotTaggerProvider(
    IThreadingContext threadingContext,
    IGlobalOptionService globalOptionService,
    [Import(AllowDefault = true)] ITextBufferVisibilityTracker? visibilityTracker,
    IAsynchronousOperationListenerProvider listenerProvider) : AsynchronousViewTaggerProvider<ITextMarkerTag>(threadingContext,
          globalOptionService,
          visibilityTracker,
          listenerProvider.GetListener(FeatureAttribute.CopilotSuggestions))
{
    private const int Delay = 10000;
    private readonly CancellationSeries _cancellationSeries = new();

    protected override TaggerDelay EventChangeDelay => TaggerDelay.OnIdle;

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

        return textView.GetCaretPoint(subjectBuffer) is { } caret
            ? SpecializedCollections.SingletonEnumerable(new SnapshotSpan(caret, 0))
            : base.GetSpansToTag(textView, subjectBuffer);
    }

    protected override async Task ProduceTagsAsync(TaggerContext<ITextMarkerTag> context, DocumentSnapshotSpan spanToTag, int? caretPosition, CancellationToken cancellationToken)
    {
        if (spanToTag.Document is not { } document
            || document.Project.Solution.Services.GetService<ICopilotCodeAnalysisService>() is not { } service
            || !service.IsCodeAnalysisOptionEnabled(document))
        {
            return;
        }

        // Cancel any prior analysis requests.
        cancellationToken = _cancellationSeries.CreateNext(cancellationToken);

        // PERF: Ensure that we wait for a reasonable time before kicking off analysis.
        //       This enables us to throttle the requests when user is editing the document.
        Thread.Sleep(Delay);
        cancellationToken.ThrowIfCancellationRequested();

        // Execute the first prompt (built-in code analysis) for the containing method in background.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var containingMethod = CSharpSyntaxFacts.Instance.GetContainingMethodDeclaration(root, spanToTag.SnapshotSpan.Start, useFullSpan: false);
        if (containingMethod != null)
        {
            var prompts = await service.GetAvailablePromptTitlesAsync(document, cancellationToken).ConfigureAwait(false);
            await service.AnalyzeDocumentAsync(document, containingMethod.Span, prompts[0], cancellationToken).ConfigureAwait(false);
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete

