// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TagType(typeof(NavigableHighlightTag))]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal partial class ReferenceHighlightingViewTaggerProvider : AsynchronousViewTaggerProvider<NavigableHighlightTag>
    {
        private readonly ISemanticChangeNotificationService _semanticChangeNotificationService;

        // Whenever an edit happens, clear all highlights.  When moving the caret, preserve 
        // highlights if the caret stays within an existing tag.
        protected override TaggerCaretChangeBehavior CaretChangeBehavior => TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag;
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveAllTags;
        protected override IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.ReferenceHighlighting);

        [ImportingConstructor]
        public ReferenceHighlightingViewTaggerProvider(
            IThreadingContext threadingContext,
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.ReferenceHighlighting), notificationService)
        {
            _semanticChangeNotificationService = semanticChangeNotificationService;
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            // Note: we don't listen for OnTextChanged.  Text changes to this buffer will get
            // reported by OnSemanticChanged.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnCaretPositionChanged(textView, textView.TextBuffer, TaggerDelay.Short),
                TaggerEventSources.OnSemanticChanged(subjectBuffer, TaggerDelay.OnIdle, _semanticChangeNotificationService),
                TaggerEventSources.OnDocumentActiveContextChanged(subjectBuffer, TaggerDelay.Short));
        }

        protected override SnapshotPoint? GetCaretPoint(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return textViewOpt.Caret.Position.Point.GetPoint(b => IsSupportedContentType(b.ContentType), PositionAffinity.Successor);
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // Note: this may return no snapshot spans.  We have to be resilient to that
            // when processing the TaggerContext<>.SpansToTag below.
            return textViewOpt.BufferGraph.GetTextBuffers(b => IsSupportedContentType(b.ContentType))
                              .Select(b => b.CurrentSnapshot.GetFullSpan())
                              .ToList();
        }

        protected override Task ProduceTagsAsync(TaggerContext<NavigableHighlightTag> context)
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
            if (!Workspace.TryGetWorkspace(caretPosition.Snapshot.AsText().Container, out var workspace))
            {
                return Task.CompletedTask;
            }

            // GetSpansToTag may have produced no actual spans to tag.  Be resilient to that.
            var document = context.SpansToTag.FirstOrDefault(vt => vt.SnapshotSpan.Snapshot == caretPosition.Snapshot).Document;
            if (document == null)
            {
                return Task.CompletedTask;
            }

            // Don't produce tags if the feature is not enabled.
            if (!workspace.Options.GetOption(FeatureOnOffOptions.ReferenceHighlighting, document.Project.Language))
            {
                return Task.CompletedTask;
            }

            // See if the user is just moving their caret around in an existing tag.  If so, we don't
            // want to actually go recompute things.  Note: this only works for containment.  If the
            // user moves their caret to the end of a highlighted reference, we do want to recompute
            // as they may now be at the start of some other reference that should be highlighted instead.
            var existingTags = context.GetExistingContainingTags(caretPosition);
            if (!existingTags.IsEmpty())
            {
                context.SetSpansTagged(SpecializedCollections.EmptyEnumerable<DocumentSnapshotSpan>());
                return Task.CompletedTask;
            }

            // Otherwise, we need to go produce all tags.
            return ProduceTagsAsync(context, caretPosition, document);
        }

        internal async Task ProduceTagsAsync(
            TaggerContext<NavigableHighlightTag> context,
            SnapshotPoint position,
            Document document)
        {
            var cancellationToken = context.CancellationToken;

            var solution = document.Project.Solution;

            using (Logger.LogBlock(FunctionId.Tagger_ReferenceHighlighting_TagProducer_ProduceTags, cancellationToken))
            {
                if (document != null)
                {
                    var service = document.GetLanguageService<IDocumentHighlightsService>();
                    if (service != null)
                    {
                        // We only want to search inside documents that correspond to the snapshots
                        // we're looking at
                        var documentsToSearch = ImmutableHashSet.CreateRange(context.SpansToTag.Select(vt => vt.Document).WhereNotNull());
                        var documentHighlightsList = await service.GetDocumentHighlightsAsync(
                            document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);
                        if (documentHighlightsList != null)
                        {
                            foreach (var documentHighlights in documentHighlightsList)
                            {
                                await AddTagSpansAsync(
                                    context, document.Project.Solution, documentHighlights).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        private async Task AddTagSpansAsync(
            TaggerContext<NavigableHighlightTag> context,
            Solution solution,
            DocumentHighlights documentHighlights)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentHighlights.Document;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textSnapshot = text.FindCorrespondingEditorTextSnapshot();
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
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
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
    }
}
