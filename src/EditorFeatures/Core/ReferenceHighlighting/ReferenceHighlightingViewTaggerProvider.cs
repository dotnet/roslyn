// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;

[Export(typeof(IViewTaggerProvider))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TagType(typeof(NavigableHighlightTag))]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class ReferenceHighlightingViewTaggerProvider(TaggerHost taggerHost)
    : AsynchronousViewTaggerProvider<NavigableHighlightTag>(taggerHost, FeatureAttribute.ReferenceHighlighting)
{
    // Whenever an edit happens, clear all highlights.  When moving the caret, preserve 
    // highlights if the caret stays within an existing tag.
    protected override TaggerCaretChangeBehavior CaretChangeBehavior => TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag;
    protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveAllTags;

    protected override ImmutableArray<IOption2> Options { get; } = [ReferenceHighlightingOptionsStorage.ReferenceHighlighting];

    protected override TaggerDelay EventChangeDelay => TaggerDelay.Medium;

    /// <summary>
    /// We support frozen partial semantics, so we can quickly get reference highlights without building SG docs.  We
    /// will still run a tagging pass after the frozen-pass where we run again on non-frozen docs.
    /// </summary>
    protected override bool SupportsFrozenPartialSemantics => true;

    protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
    {
        // Note: we don't listen for OnTextChanged.  Text changes to this buffer will get
        // reported by OnSemanticChanged.
        return TaggerEventSources.Compose(
            TaggerEventSources.OnCaretPositionChanged(textView, textView.TextBuffer),
            TaggerEventSources.OnWorkspaceChanged(subjectBuffer, AsyncListener),
            TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer));
    }

    protected override SnapshotPoint? GetCaretPoint(ITextView textViewOpt, ITextBuffer subjectBuffer)
    {
        // With no selection we just use the caret position as expected
        if (textViewOpt.Selection.IsEmpty)
        {
            return textViewOpt.Caret.Position.Point.GetPoint(b => IsSupportedContentType(b.ContentType), PositionAffinity.Successor);
        }

        // If there is a selection then it makes more sense for highlighting to apply to the token at the start
        // of the selection rather than where the caret is, otherwise you can be in a situation like [|count$$|]++
        // and it will try to highlight the operator.
        return textViewOpt.BufferGraph.MapDownToFirstMatch(textViewOpt.Selection.Start.Position, PointTrackingMode.Positive, b => IsSupportedContentType(b.ContentType), PositionAffinity.Successor);
    }

    protected override bool TryAddSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer, ref TemporaryArray<SnapshotSpan> result)
    {
        // Note: this may return no snapshot spans.  We have to be resilient to that
        // when processing the TaggerContext<>.SpansToTag below.
        foreach (var buffer in textViewOpt.BufferGraph.GetTextBuffers(b => IsSupportedContentType(b.ContentType)))
            result.Add(buffer.CurrentSnapshot.GetFullSpan());

        // Fine to always return 'true' here.  If we get no spans to tag, we can move ourselves to the no-tags state and
        // update the editor.
        return true;
    }

    protected override Task ProduceTagsAsync(
        TaggerContext<NavigableHighlightTag> context, CancellationToken cancellationToken)
    {
        // NOTE(cyrusn): Normally we'd limit ourselves to producing tags in the span we were
        // asked about.  However, we want to produce all tags here so that the user can actually
        // navigate between all of them using the appropriate tag navigation commands.  If we
        // don't generate all the tags then the user will cycle through an incorrect subset.
        if (context.CaretPosition == null)
        {
            return Task.CompletedTask;
        }

        var caretPosition = context.CaretPosition.Value;

        // GetSpansToTag may have produced no actual spans to tag.  Be resilient to that.
        var document = context.SpansToTag.FirstOrDefault(vt => vt.SnapshotSpan.Snapshot == caretPosition.Snapshot).Document;
        if (document == null)
        {
            return Task.CompletedTask;
        }

        // Don't produce tags if the feature is not enabled.
        if (!this.GlobalOptions.GetOption(ReferenceHighlightingOptionsStorage.ReferenceHighlighting, document.Project.Language))
        {
            return Task.CompletedTask;
        }

        // See if the user is just moving their caret around in an existing tag.  If so, we don't want to actually go
        // recompute things.  Note: this only works for containment.  If the user moves their caret to the end of a
        // highlighted reference, we do want to recompute as they may now be at the start of some other reference that
        // should be highlighted instead.
        var onExistingTags = context.HasExistingContainingTags(caretPosition);
        if (onExistingTags)
        {
            context.SetSpansTagged([]);
            return Task.CompletedTask;
        }

        // Otherwise, we need to go produce all tags.
        var options = this.GlobalOptions.GetHighlightingOptions(document.Project.Language);
        return ProduceTagsAsync(context, caretPosition, document, options, cancellationToken);
    }

    private static async Task ProduceTagsAsync(
        TaggerContext<NavigableHighlightTag> context,
        SnapshotPoint position,
        Document document,
        HighlightingOptions options,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Tagger_ReferenceHighlighting_TagProducer_ProduceTags, cancellationToken))
        {
            if (document != null)
            {
                var service = document.GetLanguageService<IDocumentHighlightsService>();
                if (service != null)
                {
                    // Ensure that if we're producing tags for frozen/partial documents, that we pass along that info so
                    // that we preserve that same behavior in OOP if we end up computing the tags there.
                    options = options with { FrozenPartialSemantics = context.FrozenPartialSemantics };

                    // We only want to search inside documents that correspond to the snapshots
                    // we're looking at
                    var documentsToSearchBuilder = ImmutableHashSet.CreateBuilder<Document>();
                    foreach (var snapshotSpan in context.SpansToTag)
                        documentsToSearchBuilder.AddIfNotNull(snapshotSpan.Document);

                    var documentsToSearch = documentsToSearchBuilder.ToImmutable();
                    var documentHighlightsList = await service.GetDocumentHighlightsAsync(
                        document, position, documentsToSearch, options, cancellationToken).ConfigureAwait(false);
                    if (documentHighlightsList != null)
                    {
                        foreach (var documentHighlights in documentHighlightsList)
                            AddTagSpans(context, documentHighlights, cancellationToken);
                    }
                }
            }
        }
    }

    private static void AddTagSpans(
        TaggerContext<NavigableHighlightTag> context,
        DocumentHighlights documentHighlights,
        CancellationToken cancellationToken)
    {
        var document = documentHighlights.Document;

        var textSnapshot = context.SpansToTag.FirstOrDefault(s => s.Document == document).SnapshotSpan.Snapshot;
        if (textSnapshot == null)
        {
            // There is no longer an editor snapshot for this document, so we can't care about the
            // results.
            return;
        }

        try
        {
            foreach (var span in documentHighlights.HighlightSpans)
            {
                var tag = GetTag(span);
                context.AddTag(new TagSpan<NavigableHighlightTag>(
                    textSnapshot.GetSpan(Span.FromBounds(span.TextSpan.Start, span.TextSpan.End)), tag));
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken, ErrorSeverity.General))
        {
            // report NFW and continue.
            // also, rather than return partial results, return nothing
            context.ClearTags();
        }
    }

    private static NavigableHighlightTag GetTag(HighlightSpan span)
    {
        switch (span.Kind)
        {
            case HighlightSpanKind.WrittenReference:
                return WrittenReferenceHighlightTag.Instance;

            case HighlightSpanKind.Definition:
                return DefinitionHighlightTag.Instance;

            case HighlightSpanKind.Reference:
            case HighlightSpanKind.None:
            default:
                return ReferenceHighlightTag.Instance;
        }
    }

    private static bool IsSupportedContentType(IContentType contentType)
    {
        // This list should match the list of exported content types above
        return contentType.IsOfType(ContentTypeNames.RoslynContentType) ||
               contentType.IsOfType(ContentTypeNames.XamlContentType);
    }

    // Safe to directly reference compare as all the NavigableHighlightTag subclasses are singletons.
    protected override bool TagEquals(NavigableHighlightTag tag1, NavigableHighlightTag tag2)
        => tag1 == tag2;
}
