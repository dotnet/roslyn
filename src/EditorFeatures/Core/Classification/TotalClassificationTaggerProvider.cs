// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification;

[Export(typeof(IViewTaggerProvider))]
[TagType(typeof(IClassificationTag))]
[Microsoft.VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class TotalClassificationTaggerProvider(TaggerHost taggerHost, ClassificationTypeMap typeMap) : IViewTaggerProvider
{
    private readonly SyntacticClassificationTaggerProvider _syntacticTaggerProvider = new(taggerHost, typeMap);
    private readonly SemanticClassificationViewTaggerProvider _semanticTaggerProvider = new(taggerHost, typeMap);
    private readonly EmbeddedLanguageClassificationViewTaggerProvider _embeddedTaggerProvider = new(taggerHost, typeMap);

    ITagger<T>? IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer buffer)
    {
        var tagger = CreateTagger(textView, buffer);
        if (tagger is not ITagger<T> typedTagger)
        {
            tagger?.Dispose();
            return null;
        }

        return typedTagger;
    }

    public TotalClassificationAggregateTagger? CreateTagger(ITextView textView, ITextBuffer buffer)
    {
        var syntacticTagger = _syntacticTaggerProvider.CreateTagger(buffer);
        var semanticTagger = _semanticTaggerProvider.CreateTagger(textView, buffer);
        var embeddedTagger = _embeddedTaggerProvider.CreateTagger(textView, buffer);

        if (syntacticTagger is null || semanticTagger is null || embeddedTagger is null)
        {
            syntacticTagger?.Dispose();
            semanticTagger?.Dispose();
            embeddedTagger?.Dispose();
            return null;
        }

        return new TotalClassificationAggregateTagger(syntacticTagger, semanticTagger, embeddedTagger);
    }
}

internal sealed class TotalClassificationAggregateTagger(
    EfficientTagger<IClassificationTag> syntacticTagger,
    EfficientTagger<IClassificationTag> semanticTagger,
    EfficientTagger<IClassificationTag> embeddedTagger)
    : AbstractAggregateTagger<IClassificationTag>([syntacticTagger, semanticTagger, embeddedTagger])
{
    private static readonly Comparison<TagSpan<IClassificationTag>> s_spanComparison = static (s1, s2) => s1.Span.Start.Position - s2.Span.Start.Position;

    public override void AddTags(NormalizedSnapshotSpanCollection spans, SegmentedList<TagSpan<IClassificationTag>> totalTags)
    {
        // Everything we pass in is synchronous, so we should immediately get a completed task back out.
        AddTagsAsync(
            spans,
            totalTags,
            addSyntacticSpansAsync: static (spans, tags, arg) =>
            {
                arg.syntacticTagger.AddTags(spans, tags);
                return Task.CompletedTask;
            },
            addSemanticSpansAsync: static (spans, tags, arg) =>
            {
                arg.semanticTagger.AddTags(spans, tags);
                return Task.CompletedTask;
            },
            addEmbeddedSpansAsync: static (spans, tags, arg) =>
            {
                arg.embeddedTagger.AddTags(spans, tags);
                return Task.CompletedTask;
            },
            arg: (syntacticTagger, semanticTagger, embeddedTagger)).VerifyCompleted();
    }

    public static async Task AddTagsAsync<TArg>(
        NormalizedSnapshotSpanCollection spans,
        SegmentedList<TagSpan<IClassificationTag>> totalTags,
        Func<NormalizedSnapshotSpanCollection, SegmentedList<TagSpan<IClassificationTag>>, TArg, Task> addSyntacticSpansAsync,
        Func<NormalizedSnapshotSpanCollection, SegmentedList<TagSpan<IClassificationTag>>, TArg, Task> addSemanticSpansAsync,
        Func<NormalizedSnapshotSpanCollection, SegmentedList<TagSpan<IClassificationTag>>, TArg, Task> addEmbeddedSpansAsync,
        TArg arg)
    {
        // First, get all the syntactic tags.  While they are generally overridden by semantic tags (since semantics
        // allows us to understand better what things like identifiers mean), they do take precedence for certain
        // tags like 'Comments' and 'Excluded Code'.  In those cases we want the classification to 'snap' instantly to
        // the syntactic state, and we do not want things like semantic classifications showing up over that.

        using var _1 = SegmentedListPool.GetPooledList<TagSpan<IClassificationTag>>(out var stringLiterals);
        using var _2 = SegmentedListPool.GetPooledList<TagSpan<IClassificationTag>>(out var syntacticSpans);
        using var _3 = SegmentedListPool.GetPooledList<TagSpan<IClassificationTag>>(out var semanticSpans);

        await addSyntacticSpansAsync(spans, syntacticSpans, arg).ConfigureAwait(false);
        await addSemanticSpansAsync(spans, semanticSpans, arg).ConfigureAwait(false);

        syntacticSpans.Sort(s_spanComparison);
        semanticSpans.Sort(s_spanComparison);

        using var syntacticEnumerator = syntacticSpans.GetEnumerator();
        using var semanticEnumerator = semanticSpans.GetEnumerator();

        var currentSyntactic = GetNextSyntacticSpan();
        var currentSemantic = GetNextSemanticSpan();

        while (currentSyntactic != null && currentSemantic != null)
        {
            // If both the syntactic and semantic tags are for the same span, and the semantic tag is more specific,
            // then just prefer that one (and eschew the syntactic one). Semantics is more accurate, but often will
            // produce these accurate tags more slowly than the syntactic classifier.  This allows the syntactic
            // classifier to produce an initial result, which the semantic classifier can refine.
            if (currentSyntactic.Span == currentSemantic.Span &&
                currentSemantic.Tag.ClassificationType.IsOfType(currentSyntactic.Tag.ClassificationType.Classification))
            {
                totalTags.Add(currentSemantic);
                currentSyntactic = GetNextSyntacticSpan();
                currentSemantic = GetNextSemanticSpan();
            }
            else if (currentSemantic.Span.Start <= currentSyntactic.Span.Start)
            {
                // as long as we see semantic spans before the next syntactic one, keep adding them.
                totalTags.Add(currentSemantic);
                currentSemantic = GetNextSemanticSpan();
            }
            else
            {
                // We're on a syntactic span before the next semantic one.

                // If it's a comment or excluded code, then we want to ignore every semantic classification that
                // potentially overlaps with it so that semantic classifications don't show up *on top of* them.  We
                // want commenting out code to feel like' it instantly snaps to that state.
                if (TryProcessCommentOrExcludedCode())
                    continue;

                // If we have a string literal of some sort add it to the list to be processed later. We'll want to
                // compute embedded classifications for them, and have those classifications override the string
                // literals.
                if (TryProcessSyntacticStringLiteral())
                    continue;

                // Normal case.  Just add the syntactic span and continue.
                totalTags.Add(currentSyntactic);
                currentSyntactic = GetNextSyntacticSpan();
            }
        }

        // Add any remaining semantic spans following the syntactic ones.
        while (currentSemantic != null)
        {
            totalTags.Add(currentSemantic);
            currentSemantic = GetNextSemanticSpan();
        }

        // Add any remaining syntactic spans following the semantic ones.
        while (currentSyntactic != null)
        {
            // don't have to worry about comments/excluded code since there are no semantic tags we want to override.
            if (TryProcessSyntacticStringLiteral())
                continue;

            totalTags.Add(currentSyntactic);
            currentSyntactic = GetNextSyntacticSpan();
        }

        // We've added almost all the syntactic and semantic tags (properly skipping any semantic tags that are
        // overridden by comments or excluded code).  All that remains is adding back the string literals we
        // skipped.  However, when we do so, we'll see if those string literals themselves should be overridden
        // by any embedded classifications.
        await AddEmbeddedClassificationsAsync().ConfigureAwait(false);

        return;

        bool TryProcessSyntacticStringLiteral()
        {
            if (currentSyntactic.Tag.ClassificationType.Classification is not ClassificationTypeNames.StringLiteral and not ClassificationTypeNames.VerbatimStringLiteral)
                return false;

            stringLiterals.Add(currentSyntactic);
            currentSyntactic = GetNextSyntacticSpan();
            return true;
        }

        bool TryProcessCommentOrExcludedCode()
        {
            if (currentSyntactic.Tag.ClassificationType.Classification is not ClassificationTypeNames.Comment and not ClassificationTypeNames.ExcludedCode)
                return false;

            // Keep skipping semantic tags that overlaps with this syntactic tag.
            while (currentSemantic != null && currentSemantic.Span.OverlapsWith(currentSyntactic.Span.Span))
                currentSemantic = GetNextSemanticSpan();

            // now add that syntactic span.
            totalTags.Add(currentSyntactic);
            currentSyntactic = GetNextSyntacticSpan();
            return true;
        }

        async Task AddEmbeddedClassificationsAsync()
        {
            // nothing to do if we didn't run into any string literals.
            if (stringLiterals.Count == 0)
                return;

            // Only need to ask for the spans that overlapped the string literals.
            using var _1 = SegmentedListPool.GetPooledList<TagSpan<IClassificationTag>>(out var embeddedClassifications);

            var stringLiteralSpansFull = new NormalizedSnapshotSpanCollection(stringLiterals.Select(s => s.Span));

            // The spans of the string literal itself may be far off screen.  Intersect the string literal spans
            // with the view spans to get the actual spans we want to classify.
            var stringLiteralSpans = NormalizedSnapshotSpanCollection.Intersection(stringLiteralSpansFull, spans);

            await addEmbeddedSpansAsync(stringLiteralSpans, embeddedClassifications, arg).ConfigureAwait(false);

            // Nothing complex to do if we got no embedded classifications back.  Just add in all the string
            // classifications, untouched.
            if (embeddedClassifications.Count == 0)
            {
                totalTags.AddRange(stringLiterals);
                return;
            }

            // ClassifierHelper.MergeParts requires these to be sorted.
            stringLiterals.Sort(s_spanComparison);
            embeddedClassifications.Sort(s_spanComparison);

            // Call into the helper to merge the string literals and embedded classifications into the final result.
            // The helper will add all the embedded classifications first, then add string literal classifications
            // in the the space between the embedded classifications that were originally classified as a string
            // literal.
            ClassifierHelper.MergeParts<TagSpan<IClassificationTag>, ClassificationTagSpanIntervalIntrospector>(
                stringLiterals,
                embeddedClassifications,
                totalTags,
                static tag => tag.Span.Span.ToTextSpan(),
                static (original, final) => new TagSpan<IClassificationTag>(new SnapshotSpan(original.Span.Snapshot, final.ToSpan()), original.Tag));
        }

        TagSpan<IClassificationTag>? GetNextSyntacticSpan()
            => syntacticEnumerator.MoveNext() ? syntacticEnumerator.Current : null;

        TagSpan<IClassificationTag>? GetNextSemanticSpan()
            => semanticEnumerator.MoveNext() ? semanticEnumerator.Current : null;
    }

    private readonly struct ClassificationTagSpanIntervalIntrospector : IIntervalIntrospector<TagSpan<IClassificationTag>>
    {
        public TextSpan GetSpan(TagSpan<IClassificationTag> value)
            => value.Span.Span.ToTextSpan();
    }
}
