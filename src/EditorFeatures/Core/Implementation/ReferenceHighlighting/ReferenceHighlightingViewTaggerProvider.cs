// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.ReferenceHighlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
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
            IForegroundNotificationService notificationService,
            ISemanticChangeNotificationService semanticChangeNotificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ReferenceHighlighting), notificationService)
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
            return textViewOpt.Caret.Position.Point.GetPoint(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType), PositionAffinity.Successor);
        }

        protected override IEnumerable<SnapshotSpan> GetSpansToTag(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            return textViewOpt.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType))
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
                return SpecializedTasks.EmptyTask;
            }

            var caretPosition = context.CaretPosition.Value;

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(caretPosition.Snapshot.AsText().Container, out workspace))
            {
                return SpecializedTasks.EmptyTask;
            }

            var document = context.SpansToTag.First(vt => vt.SnapshotSpan.Snapshot == caretPosition.Snapshot).Document;
            if (document == null)
            {
                return SpecializedTasks.EmptyTask;
            }

            // Don't produce tags if the feature is not enabled.
            if (!workspace.Options.GetOption(FeatureOnOffOptions.ReferenceHighlighting, document.Project.Language))
            {
                return SpecializedTasks.EmptyTask;
            }

            var existingTags = context.GetExistingTags(new SnapshotSpan(caretPosition, 0));
            if (!existingTags.IsEmpty())
            {
                // We already have a tag at this position.  So the user is moving from one highlight
                // tag to another.  In this case we don't want to recompute anything.  Let our caller
                // know that we should preserve all tags.
                context.SetSpansTagged(SpecializedCollections.EmptyEnumerable<DocumentSnapshotSpan>());
                return SpecializedTasks.EmptyTask;
            }

            // Otherwise, we need to go produce all tags.
            return ProduceTagsAsync(context, caretPosition, workspace, document);
        }

        internal async Task ProduceTagsAsync(
            TaggerContext<NavigableHighlightTag> context,
            SnapshotPoint position,
            Workspace workspace,
            Document document)
        {
            var cancellationToken = context.CancellationToken;

            var solution = document.Project.Solution;

            using (Logger.LogBlock(FunctionId.Tagger_ReferenceHighlighting_TagProducer_ProduceTags, cancellationToken))
            {
                var result = new List<ITagSpan<NavigableHighlightTag>>();

                if (document != null)
                {
                    var documentHighlightsService = document.Project.LanguageServices.GetService<IDocumentHighlightsService>();
                    if (documentHighlightsService != null)
                    {
                        // We only want to search inside documents that correspond to the snapshots
                        // we're looking at
                        var documentsToSearch = ImmutableHashSet.CreateRange(context.SpansToTag.Select(vt => vt.Document).WhereNotNull());
                        var documentHighlightsList = await documentHighlightsService.GetDocumentHighlightsAsync(document, position, documentsToSearch, cancellationToken).ConfigureAwait(false);
                        if (documentHighlightsList != null)
                        {
                            foreach (var documentHighlights in documentHighlightsList)
                            {
                                await AddTagSpansAsync(context, solution, result, documentHighlights).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
        }

        private async Task AddTagSpansAsync(
            TaggerContext<NavigableHighlightTag> context,
            Solution solution,
            List<ITagSpan<NavigableHighlightTag>> tags,
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

            foreach (var span in documentHighlights.HighlightSpans)
            {
                var tag = GetTag(span);
                context.AddTag(new TagSpan<NavigableHighlightTag>(
                    textSnapshot.GetSpan(Span.FromBounds(span.TextSpan.Start, span.TextSpan.End)), tag));
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
    }
}