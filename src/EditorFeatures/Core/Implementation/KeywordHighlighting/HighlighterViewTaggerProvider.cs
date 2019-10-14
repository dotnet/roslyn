// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
            IThreadingContext threadingContext,
            IHighlightingService highlightingService,
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, listenerProvider.GetListener(FeatureAttribute.KeywordHighlighting), notificationService)
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

            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/763988
            // It turns out a document might be associated with a project of wrong language, e.g. C# document in a Xaml project. 
            // Even though we couldn't repro the crash above, a fix is made in one of possibly multiple code paths that could cause 
            // us to end up in this situation. 
            // Regardless of the effective of the fix, we want to enhance the guard against such scenario here until an audit in 
            // workspace is completed to eliminate the root cause.
            if (document?.SupportsSyntaxTree != true)
            {
                return;
            }

            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            if (!documentOptions.GetOption(FeatureOnOffOptions.KeywordHighlighting))
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

            // See if the user is just moving their caret around in an existing tag.  If so, we don't
            // want to actually go recompute things.  Note: this only works for containment.  If the
            // user moves their caret to the end of a highlighted reference, we do want to recompute
            // as they may now be at the start of some other reference that should be highlighted instead.
            var existingTags = context.GetExistingContainingTags(new SnapshotPoint(snapshot, position));
            if (!existingTags.IsEmpty())
            {
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
