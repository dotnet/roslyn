// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineHints;

/// <summary>
/// The TaggerProvider that calls upon the service in order to locate the spans and names
/// </summary>
internal sealed partial class InlineHintsDataTaggerProvider<TAdditionalInformation>(
    TaggerHost taggerHost,
    IInlineHintKeyProcessor inlineHintKeyProcessor)
    : AsynchronousViewportTaggerProvider<InlineHintDataTag<TAdditionalInformation>>(taggerHost, FeatureAttribute.InlineHints)
    where TAdditionalInformation : class
{
    private readonly IInlineHintKeyProcessor _inlineHintKeyProcessor = inlineHintKeyProcessor;

    protected override SpanTrackingMode SpanTrackingMode => SpanTrackingMode.EdgeInclusive;

    /// <summary>
    /// We want to make sure that if the user edits the space that the tag exists in that it goes away and they
    /// don't see stale tags sticking around in random locations until the next update.  A good example of when this
    /// is desirable is 'cut line'. If the tags aren't removed, then the line will be gone but the tags will remain
    /// at whatever points the tracking spans moved them to.
    /// </summary>
    protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveTagsThatIntersectEdits;

    protected override TaggerDelay EventChangeDelay => TaggerDelay.Short;

    protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
    {
        return TaggerEventSources.Compose(
            TaggerEventSources.OnViewSpanChanged(this.ThreadingContext, textView),
            TaggerEventSources.OnWorkspaceChanged(subjectBuffer, this.AsyncListener),
            new InlineHintKeyProcessorEventSource(_inlineHintKeyProcessor),
            TaggerEventSources.OnGlobalOptionChanged(GlobalOptions, static option =>
                option.Equals(InlineHintsOptionsStorage.EnabledForParameters) ||
                option.Equals(InlineHintsOptionsStorage.ForLiteralParameters) ||
                option.Equals(InlineHintsOptionsStorage.ForIndexerParameters) ||
                option.Equals(InlineHintsOptionsStorage.ForObjectCreationParameters) ||
                option.Equals(InlineHintsOptionsStorage.ForOtherParameters) ||
                option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatMatchMethodIntent) ||
                option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatDifferOnlyBySuffix) ||
                option.Equals(InlineHintsOptionsStorage.SuppressForParametersThatMatchArgumentName) ||
                option.Equals(InlineHintsOptionsStorage.EnabledForTypes) ||
                option.Equals(InlineHintsOptionsStorage.ForImplicitVariableTypes) ||
                option.Equals(InlineHintsOptionsStorage.ForLambdaParameterTypes) ||
                option.Equals(InlineHintsOptionsStorage.ForImplicitObjectCreation) ||
                option.Equals(InlineHintsOptionsStorage.ForCollectionExpressions)));
    }

    protected override async Task ProduceTagsAsync(
        TaggerContext<InlineHintDataTag<TAdditionalInformation>> context,
        DocumentSnapshotSpan spanToTag,
        CancellationToken cancellationToken)
    {
        var document = spanToTag.Document;
        if (document == null)
            return;

        // The LSP client will handle producing tags when running under the LSP editor.
        // Our tagger implementation should return nothing to prevent conflicts.
        var workspaceContextService = document.Project.Solution.Services.GetRequiredService<IWorkspaceContextService>();
        if (workspaceContextService.IsInLspEditorContext())
            return;

        var service = document.GetLanguageService<IInlineHintsService>();
        if (service == null)
            return;

        var options = GlobalOptions.GetInlineHintsOptions(document.Project.Language);

        var snapshotSpan = spanToTag.SnapshotSpan;
        var hints = await service.GetInlineHintsAsync(
            document, snapshotSpan.Span.ToTextSpan(), options,
            displayAllOverride: _inlineHintKeyProcessor?.State is true,
            cancellationToken).ConfigureAwait(false);

        foreach (var hint in hints)
        {
            // If we don't have any text to actually show the user, then don't make a tag.
            if (hint.DisplayParts.Sum(p => p.ToString().Length) == 0)
                continue;

            context.AddTag(new TagSpan<InlineHintDataTag<TAdditionalInformation>>(
                hint.Span.ToSnapshotSpan(snapshotSpan.Snapshot),
                new(this, snapshotSpan.Snapshot, hint)));
        }
    }

    protected override bool TagEquals(InlineHintDataTag<TAdditionalInformation> tag1, InlineHintDataTag<TAdditionalInformation> tag2)
        => tag1.Equals(tag2);
}
