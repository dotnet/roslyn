// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.KeywordHighlighting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;

[Export(typeof(IViewTaggerProvider))]
[TagType(typeof(KeywordHighlightTag))]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class HighlighterViewTaggerProvider(TaggerHost taggerHost, IHighlightingService highlightingService)
    : AsynchronousViewTaggerProvider<KeywordHighlightTag>(taggerHost, FeatureAttribute.KeywordHighlighting)
{
    private readonly IHighlightingService _highlightingService = highlightingService;
    private static readonly PooledObjects.ObjectPool<List<TextSpan>> s_listPool = new(() => []);

    // Whenever an edit happens, clear all highlights.  When moving the caret, preserve 
    // highlights if the caret stays within an existing tag.
    protected override TaggerCaretChangeBehavior CaretChangeBehavior => TaggerCaretChangeBehavior.RemoveAllTagsOnCaretMoveOutsideOfTag;
    protected override TaggerTextChangeBehavior TextChangeBehavior => TaggerTextChangeBehavior.RemoveAllTags;

    protected override ImmutableArray<IOption2> Options { get; } = [KeywordHighlightingOptionsStorage.KeywordHighlighting];

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

        if (!GlobalOptions.GetOption(KeywordHighlightingOptionsStorage.KeywordHighlighting, document.Project.Language))
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

        // See if the user is just moving their caret around in an existing tag.  If so, we don't want to actually go
        // recompute things.  Note: this only works for containment.  If the user moves their caret to the end of a
        // highlighted reference, we do want to recompute as they may now be at the start of some other reference that
        // should be highlighted instead.
        var onExistingTags = context.HasExistingContainingTags(new SnapshotPoint(snapshot, position));
        if (onExistingTags)
        {
            context.SetSpansTagged([]);
            return;
        }

        using (Logger.LogBlock(FunctionId.Tagger_Highlighter_TagProducer_ProduceTags, cancellationToken))
        using (s_listPool.GetPooledObject(out var highlights))
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            _highlightingService.AddHighlights(root, position, highlights, cancellationToken);

            foreach (var span in highlights)
            {
                context.AddTag(new TagSpan<KeywordHighlightTag>(span.ToSnapshotSpan(snapshot), KeywordHighlightTag.Instance));
            }
        }
    }

    protected override bool TagEquals(KeywordHighlightTag tag1, KeywordHighlightTag tag2)
    {
        Contract.ThrowIfFalse(tag1 == tag2, "KeywordHighlightTag is supposed to be a singleton");
        return true;
    }
}
