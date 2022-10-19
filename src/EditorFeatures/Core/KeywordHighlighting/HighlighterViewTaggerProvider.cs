// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
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
        private static readonly PooledObjects.ObjectPool<List<TextSpan>> s_listPool = new(() => new List<TextSpan>());

        // Whenever an edit happens, clear all highlights.  When moving the caret, preserve 
        // highlights if the caret stays within an existing tag.
        protected override TaggerCaretChangeBehavior CaretChangeBehavior => TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag;
        protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveAllTags;
        protected override IEnumerable<PerLanguageOption2<bool>> PerLanguageOptions => SpecializedCollections.SingletonEnumerable(FeatureOnOffOptions.KeywordHighlighting);

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public HighlighterViewTaggerProvider(
            IThreadingContext threadingContext,
            IHighlightingService highlightingService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, globalOptions, listenerProvider.GetListener(FeatureAttribute.KeywordHighlighting))
        {
            _highlightingService = highlightingService;
        }

        protected override TaggerDelay EventChangeDelay => TaggerDelay.NearImmediate;

        protected override ITaggerEventSource CreateEventSource(ITextView textView, ITextBuffer subjectBuffer)
        {
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer),
                TaggerEventSources.OnCaretPositionChanged(textView, subjectBuffer),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer));
        }

        protected override async Task ProduceTagsAsync(
            TaggerContext<KeywordHighlightTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition, CancellationToken cancellationToken)
        {
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

            if (!GlobalOptions.GetOption(FeatureOnOffOptions.KeywordHighlighting, document.Project.Language))
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
                context.SetSpansTagged(ImmutableArray<SnapshotSpan>.Empty);
                return;
            }

            using (Logger.LogBlock(FunctionId.Tagger_Highlighter_TagProducer_ProduceTags, cancellationToken))
            using (s_listPool.GetPooledObject(out var highlights))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                _highlightingService.AddHighlights(root, position, highlights, cancellationToken);

                foreach (var span in highlights)
                {
                    context.AddTag(new TagSpan<KeywordHighlightTag>(span.ToSnapshotSpan(snapshot), KeywordHighlightTag.Instance));
                }
            }
        }
    }
}
