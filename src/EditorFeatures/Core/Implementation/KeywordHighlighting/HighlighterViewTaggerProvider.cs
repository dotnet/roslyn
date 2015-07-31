// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(KeywordHighlightTag))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [ContentType(ContentTypeNames.VisualBasicContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal class HighlighterViewTaggerProvider : AsynchronousViewTaggerProvider<KeywordHighlightTag>
    {
        private readonly IHighlightingService _highlightingService;

        // Whenever an edit happens, clear all highlights.  When moving the caret, preserve 
        // highlights if the caret stays within an existing tag.
        protected override TaggerCaretChangeBehavior CaretChangeBehavior => TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag;
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveAllTags;
        protected override IEnumerable<PerLanguageOption<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.KeywordHighlighting);

        [ImportingConstructor]
        public HighlighterViewTaggerProvider(
            IHighlightingService highlightingService,
            IForegroundNotificationService notificationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
            : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.KeywordHighlighting), notificationService)
        {
            _highlightingService = highlightingService;
        }

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer, TaggerDelay.NearImmediate),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer, TaggerDelay.NearImmediate));
        }

        protected override async Task ProduceTagsAsync(TaggerContext<KeywordHighlightTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            var cancellationToken = context.CancellationToken;
            var document = documentSnapshotSpan.Document;
            if (document == null)
            {
                return;
            }

            var options = document.Project.Solution.Workspace.Options;
            if (!options.GetOption(FeatureOnOffOptions.KeywordHighlighting, document.Project.Language))
            {
                return;
            }

            if (!caretPosition.HasValue)
            {
                return;
            }

            var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
            var position = caretPosition.Value;
            var snapshot = snapshotSpan.Snapshot;

            var existingTags = context.GetExistingTags(new SnapshotSpan(snapshot, position, 0));
            if (!existingTags.IsEmpty())
            {
                // We already have a tag at this position.  So the user is moving from one highlight
                // tag to another.  In this case we don't want to recompute anything.  Let our caller
                // know that we should preserve all tags.
                context.SetSpansTagged(SpecializedCollections.EmptyEnumerable<DocumentSnapshotSpan>());
                return;
            }

            using (Logger.LogBlock(FunctionId.Tagger_Highlighter_TagProducer_ProduceTags, cancellationToken))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var spans = _highlightingService.GetHighlights(root, position, cancellationToken);
                foreach (var span in spans)
                {
                    context.AddTag(new TagSpan<KeywordHighlightTag>(span.ToSnapshotSpan(snapshot), KeywordHighlightTag.Instance));
                }
            }
        }
    }
}