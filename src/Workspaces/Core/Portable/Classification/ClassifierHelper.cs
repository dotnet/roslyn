// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static partial class ClassifierHelper
    {
        /// <summary>
        /// Classifies the provided <paramref name="span"/> in the given <paramref name="document"/>. This will do this
        /// using an appropriate <see cref="IClassificationService"/> if that can be found.  <see
        /// cref="ImmutableArray{T}.IsDefault"/> will be returned if this fails.
        /// </summary>
        /// <param name="includeAdditiveSpans">Whether or not 'additive' classification spans are included in the
        /// results or not.  'Additive' spans are things like 'this variable is static' or 'this variable is
        /// overwritten'.  i.e. they add additional information to a previous classification.</param>
        public static async Task<ImmutableArray<ClassifiedSpan>> GetClassifiedSpansAsync(
            Document document,
            TextSpan span,
            ClassificationOptions options,
            bool includeAdditiveSpans,
            CancellationToken cancellationToken)
        {
            var classificationService = document.GetLanguageService<IClassificationService>();
            if (classificationService == null)
                return default;

            // Call out to the individual language to classify the chunk of text around the
            // reference. We'll get both the syntactic and semantic spans for this region.
            // Because the semantic tags may override the semantic ones (for example, 
            // "DateTime" might be syntactically an identifier, but semantically a struct
            // name), we'll do a later merging step to get the final correct list of 
            // classifications.  For tagging, normally the editor handles this.  But as
            // we're producing the list of Inlines ourselves, we have to handles this here.
            using var _1 = Classifier.GetPooledList(out var syntaxSpans);
            using var _2 = Classifier.GetPooledList(out var semanticSpans);

            await classificationService.AddSyntacticClassificationsAsync(document, span, syntaxSpans, cancellationToken).ConfigureAwait(false);

            // Intentional that we're adding both semantic and embedded lang classifications to the same array.  Both
            // are 'semantic' from the perspective of this helper method.
            await classificationService.AddSemanticClassificationsAsync(document, span, options, semanticSpans, cancellationToken).ConfigureAwait(false);
            await classificationService.AddEmbeddedLanguageClassificationsAsync(document, span, options, semanticSpans, cancellationToken).ConfigureAwait(false);

            // MergeClassifiedSpans will ultimately filter multiple classifications for the same
            // span down to one. We know that additive classifications are there just to 
            // provide additional information about the true classification. By default, we will
            // remove additive ClassifiedSpans until we have support for additive classifications
            // in classified spans. https://github.com/dotnet/roslyn/issues/32770
            // The exception to this is LSP, which expects the additive spans.
            if (!includeAdditiveSpans)
            {
                RemoveAdditiveSpans(syntaxSpans);
                RemoveAdditiveSpans(semanticSpans);
            }

            var classifiedSpans = MergeClassifiedSpans(syntaxSpans, semanticSpans, span);
            return classifiedSpans;
        }

        private static void RemoveAdditiveSpans(SegmentedList<ClassifiedSpan> spans)
        {
            for (var i = spans.Count - 1; i >= 0; i--)
            {
                var span = spans[i];
                if (ClassificationTypeNames.AdditiveTypeNames.Contains(span.ClassificationType))
                    spans.RemoveAt(i);
            }
        }

        private static ImmutableArray<ClassifiedSpan> MergeClassifiedSpans(
            SegmentedList<ClassifiedSpan> syntaxSpans,
            SegmentedList<ClassifiedSpan> semanticSpans,
            TextSpan widenedSpan)
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

            using var _1 = Classifier.GetPooledList(out var mergedSpans);

            MergeParts(syntaxSpans, semanticSpans, mergedSpans);
            Order(mergedSpans);

            // The classification service will only produce classifications for things it knows about.  i.e. there will
            // be gaps in what it produces. Fill in those gaps so we have *all* parts of the span classified properly.
            using var _2 = Classifier.GetPooledList(out var filledInSpans);
            FillInClassifiedSpanGaps(widenedSpan.Start, mergedSpans, filledInSpans);
            return filledInSpans.ToImmutableArray();
        }

        private static readonly Comparison<ClassifiedSpan> s_spanComparison = static (s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start;

        private static void Order(SegmentedList<ClassifiedSpan> syntaxSpans)
            => syntaxSpans.Sort(s_spanComparison);

        /// <summary>
        /// Ensures that all spans in <paramref name="spans"/> do not go beyond the spans in <paramref
        /// name="widenedSpan"/>. Any spans that are entirely outside of <paramref name="widenedSpan"/> are replaced
        /// with <see langword="default"/>.
        /// </summary>
        private static void AdjustSpans(SegmentedList<ClassifiedSpan> spans, TextSpan widenedSpan)
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
                    // The additiveType's may appear before or after their modifier due to sorting.
                    var previousSpan = spans[i - 1];
                    var isAdditiveClassification = previousSpan.TextSpan == span.TextSpan &&
                        ClassificationTypeNames.AdditiveTypeNames.Contains(span.ClassificationType) || ClassificationTypeNames.AdditiveTypeNames.Contains(previousSpan.ClassificationType);

                    // Additive classifications are intended to overlap so do not ignore it.
                    if (!isAdditiveClassification && previousSpan.TextSpan.End > intersection.Value.Start)
                    {
                        // This span isn't strictly after the previous span.  Ignore it.
                        intersection = null;
                    }
                }

                var newSpan = new ClassifiedSpan(span.ClassificationType, intersection.GetValueOrDefault());
                spans[i] = newSpan;
            }
        }

        public static void FillInClassifiedSpanGaps(
            int startPosition, SegmentedList<ClassifiedSpan> classifiedSpans, SegmentedList<ClassifiedSpan> result)
        {
            foreach (var span in classifiedSpans)
            {
                // Should not get empty spans.  They are filtered out in MergeParts
                Debug.Assert(!span.TextSpan.IsEmpty);

                // Ignore empty spans.  We can get those when the classification service
                // returns spans outside of the range of the span we asked to classify.
                if (span.TextSpan.Length == 0)
                    continue;

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

        /// <summary>
        /// Adds all semantic parts to final parts, and adds all portions of <paramref name="syntaxParts"/> that do not
        /// overlap with any semantic parts as well.  All final parts will be non-empty.
        /// </summary>
        private static void MergeParts(
            SegmentedList<ClassifiedSpan> syntaxParts,
            SegmentedList<ClassifiedSpan> semanticParts,
            SegmentedList<ClassifiedSpan> finalParts)
        {
            // Create an interval tree so we can easily determine which semantic parts intersect with the 
            // syntactic parts we're looking at.
            var semanticPartsTree = new SimpleIntervalTree<ClassifiedSpan, ClassifiedSpanIntervalIntrospector>(
                ClassifiedSpanIntervalIntrospector.Instance, values: null);

            // Add all the non-empty semantic parts to the tree.
            foreach (var part in semanticParts)
            {
                if (!part.TextSpan.IsEmpty)
                {
                    semanticPartsTree.AddIntervalInPlace(part);
                    finalParts.Add(part);
                }
            }

            using var tempBuffer = TemporaryArray<ClassifiedSpan>.Empty;

            foreach (var syntacticPart in syntaxParts)
            {
                // ignore empty parts.
                if (syntacticPart.TextSpan.IsEmpty)
                    continue;

                tempBuffer.Clear();
                semanticPartsTree.FillWithIntervalsThatOverlapWith(
                    syntacticPart.TextSpan.Start, syntacticPart.TextSpan.Length, ref tempBuffer.AsRef());

                if (tempBuffer.Count == 0)
                {
                    // semantic parts didn't overlap with this syntax part at all.  Just add in directly.
                    finalParts.Add(syntacticPart);
                    continue;
                }

                // One or Multiple semantic parts.
                // Add the syntactic portion before the first semantic part,
                // the syntactic pieces between the semantic parts,
                // and the syntactic portion after the last semantic part.

                var firstSemanticPart = tempBuffer[0];
                var lastSemanticPart = tempBuffer[tempBuffer.Count - 1];

                Debug.Assert(firstSemanticPart.TextSpan.OverlapsWith(syntacticPart.TextSpan));
                Debug.Assert(lastSemanticPart.TextSpan.OverlapsWith(syntacticPart.TextSpan));

                if (syntacticPart.TextSpan.Start < firstSemanticPart.TextSpan.Start)
                {
                    finalParts.Add(new ClassifiedSpan(syntacticPart.ClassificationType, TextSpan.FromBounds(
                        syntacticPart.TextSpan.Start,
                        firstSemanticPart.TextSpan.Start)));
                }

                for (var i = 0; i < tempBuffer.Count - 1; i++)
                {
                    var semanticPart1 = tempBuffer[i];
                    var semanticPart2 = tempBuffer[i + 1];

                    Debug.Assert(semanticPart1.TextSpan.OverlapsWith(syntacticPart.TextSpan));
                    Debug.Assert(semanticPart1.TextSpan.OverlapsWith(syntacticPart.TextSpan));

                    if (semanticPart1.TextSpan.End < semanticPart2.TextSpan.Start)
                    {
                        finalParts.Add(new ClassifiedSpan(syntacticPart.ClassificationType, TextSpan.FromBounds(
                            semanticPart1.TextSpan.End,
                            semanticPart2.TextSpan.Start)));
                    }
                }

                if (lastSemanticPart.TextSpan.End < syntacticPart.TextSpan.End)
                {
                    finalParts.Add(new ClassifiedSpan(syntacticPart.ClassificationType, TextSpan.FromBounds(
                        lastSemanticPart.TextSpan.End,
                        syntacticPart.TextSpan.End)));
                }
            }

            // now that we've added all semantic parts and syntactic-portions, sort the final result.
            finalParts.Sort(s_spanComparison);
        }
    }
}
