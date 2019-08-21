// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassifierHelper
    {
        /// <summary>
        /// Classifies the provided <paramref name="span"/> in the given <paramref name="document"/>.
        /// This will do this using an appropriate <see cref="IClassificationService"/>
        /// if that can be found.  <see cref="ImmutableArray{T}.IsDefault"/> will be returned if this
        /// fails.
        /// </summary>
        public static async Task<ImmutableArray<ClassifiedSpan>> GetClassifiedSpansAsync(
            Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var classificationService = document.GetLanguageService<IClassificationService>();
            if (classificationService == null)
            {
                return default;
            }

            // Call out to the individual language to classify the chunk of text around the
            // reference. We'll get both the syntactic and semantic spans for this region.
            // Because the semantic tags may override the semantic ones (for example, 
            // "DateTime" might be syntactically an identifier, but semantically a struct
            // name), we'll do a later merging step to get the final correct list of 
            // classifications.  For tagging, normally the editor handles this.  But as
            // we're producing the list of Inlines ourselves, we have to handles this here.
            var syntaxSpans = ListPool<ClassifiedSpan>.Allocate();
            var semanticSpans = ListPool<ClassifiedSpan>.Allocate();
            try
            {
                await classificationService.AddSyntacticClassificationsAsync(document, span, syntaxSpans, cancellationToken).ConfigureAwait(false);
                await classificationService.AddSemanticClassificationsAsync(document, span, semanticSpans, cancellationToken).ConfigureAwait(false);

                // MergeClassifiedSpans will ultimately filter multiple classifications for the same
                // span down to one. We know that additive classifications are there just to 
                // provide additional information about the true classification. We will remove
                // additive ClassifiedSpans until we have support for additive classifications
                // in classified spans. https://github.com/dotnet/roslyn/issues/32770
                RemoveAdditiveSpans(syntaxSpans);
                RemoveAdditiveSpans(semanticSpans);

                var classifiedSpans = MergeClassifiedSpans(syntaxSpans, semanticSpans, span);
                return classifiedSpans;
            }
            finally
            {
                ListPool<ClassifiedSpan>.Free(syntaxSpans);
                ListPool<ClassifiedSpan>.Free(semanticSpans);
            }
        }

        private static void RemoveAdditiveSpans(List<ClassifiedSpan> spans)
        {
            for (var i = spans.Count - 1; i >= 0; i--)
            {
                var span = spans[i];
                if (ClassificationTypeNames.AdditiveTypeNames.Contains(span.ClassificationType))
                {
                    spans.RemoveAt(i);
                }
            }
        }

        private static ImmutableArray<ClassifiedSpan> MergeClassifiedSpans(
            List<ClassifiedSpan> syntaxSpans, List<ClassifiedSpan> semanticSpans, TextSpan widenedSpan)
        {
            // The spans produced by the language services may not be ordered
            // (indeed, this happens with semantic classification as different
            // providers produce different results in an arbitrary order).  Order
            // them first before proceeding.
            Order(syntaxSpans);
            Order(semanticSpans);

            // It's possible for us to get classified spans that occur *before*
            // or after the span we want to present. This happens because the calls to
            // AddSyntacticClassificationsAsync and AddSemanticClassificationsAsync 
            // may return more spans than the range asked for.  While bad form,
            // it's never been a requirement that implementation not do that.
            // For example, the span may be the non-full-span of a node, but the
            // classifiers may still return classifications for leading/trailing
            // trivia even if it's out of the bounds of that span.
            // 
            // To deal with that, we adjust all spans so that they don't go outside
            // of the range we care about.
            AdjustSpans(syntaxSpans, widenedSpan);
            AdjustSpans(semanticSpans, widenedSpan);

            // The classification service will only produce classifications for
            // things it knows about.  i.e. there will be gaps in what it produces.
            // Fill in those gaps so we have *all* parts of the span 
            // classified properly.
            var filledInSyntaxSpans = ArrayBuilder<ClassifiedSpan>.GetInstance();
            var filledInSemanticSpans = ArrayBuilder<ClassifiedSpan>.GetInstance();

            try
            {
                FillInClassifiedSpanGaps(widenedSpan.Start, syntaxSpans, filledInSyntaxSpans);
                FillInClassifiedSpanGaps(widenedSpan.Start, semanticSpans, filledInSemanticSpans);

                // Now merge the lists together, taking all the results from syntaxParts
                // unless they were overridden by results in semanticParts.
                return MergeParts(filledInSyntaxSpans, filledInSemanticSpans);
            }
            finally
            {
                filledInSyntaxSpans.Free();
                filledInSemanticSpans.Free();
            }
        }

        private static void Order(List<ClassifiedSpan> syntaxSpans)
            => syntaxSpans.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);

        private static void AdjustSpans(List<ClassifiedSpan> spans, TextSpan widenedSpan)
        {
            for (var i = 0; i < spans.Count; i++)
            {
                var span = spans[i];

                // Make sure the span actually intersects 'widenedSpan'.  If it 
                // does not, just put in an empty length span.  It will get ignored later
                // when we walk through this list.
                var intersection = span.TextSpan.Intersection(widenedSpan);

                if (i > 0 && intersection != null)
                {
                    var isAdditiveClassification = spans[i - 1].TextSpan == span.TextSpan &&
                        ClassificationTypeNames.AdditiveTypeNames.Contains(span.ClassificationType);

                    // Additive classifications are intended to overlap so do not ignore it.
                    if (!isAdditiveClassification && spans[i - 1].TextSpan.End > intersection.Value.Start)
                    {
                        // This span isn't strictly after the previous span.  Ignore it.
                        intersection = null;
                    }
                }

                var newSpan = new ClassifiedSpan(span.ClassificationType,
                    intersection ?? new TextSpan());
                spans[i] = newSpan;
            }
        }

        public static void FillInClassifiedSpanGaps(
            int startPosition, IEnumerable<ClassifiedSpan> classifiedSpans, ArrayBuilder<ClassifiedSpan> result)
        {
            foreach (var span in classifiedSpans)
            {
                // Ignore empty spans.  We can get those when the classification service
                // returns spans outside of the range of the span we asked to classify.
                if (span.TextSpan.Length == 0)
                {
                    continue;
                }

                // If there is space between this span and the last one, then add a space.
                if (startPosition < span.TextSpan.Start)
                {
                    result.Add(new ClassifiedSpan(ClassificationTypeNames.Text,
                        TextSpan.FromBounds(
                            startPosition, span.TextSpan.Start)));
                }

                result.Add(span);
                startPosition = span.TextSpan.End;
            }
        }

        private static ImmutableArray<ClassifiedSpan> MergeParts(
            ArrayBuilder<ClassifiedSpan> syntaxParts,
            ArrayBuilder<ClassifiedSpan> semanticParts)
        {
            // Take all the syntax parts.  However, if any have been overridden by a 
            // semantic part, then choose that one.

            var finalParts = ArrayBuilder<ClassifiedSpan>.GetInstance();
            var lastReplacementIndex = 0;
            for (int i = 0, n = syntaxParts.Count; i < n; i++)
            {
                var syntaxPartAndSpan = syntaxParts[i];

                // See if we can find a semantic part to replace this syntax part.
                var replacementIndex = semanticParts.FindIndex(
                    lastReplacementIndex, t => t.TextSpan == syntaxPartAndSpan.TextSpan);

                // Take the semantic part if it's just 'text'.  We want to keep it if
                // the semantic classifier actually produced an interesting result 
                // (as opposed to it just being a 'gap' classification).
                var part = replacementIndex >= 0 && !IsClassifiedAsText(semanticParts[replacementIndex])
                    ? semanticParts[replacementIndex]
                    : syntaxPartAndSpan;
                finalParts.Add(part);

                if (replacementIndex >= 0)
                {
                    // If we found a semantic replacement, update the lastIndex.
                    // That way we can start searching from that point instead 
                    // of checking all the elements each time.
                    lastReplacementIndex = replacementIndex + 1;
                }
            }

            return finalParts.ToImmutableAndFree();
        }

        private static bool IsClassifiedAsText(ClassifiedSpan partAndSpan)
        {
            // Don't take 'text' from the semantic parts.  We'll get those for the 
            // spaces between the actual interesting semantic spans, and we don't 
            // want them to override actual good syntax spans.
            return partAndSpan.ClassificationType == ClassificationTypeNames.Text;
        }
    }
}
