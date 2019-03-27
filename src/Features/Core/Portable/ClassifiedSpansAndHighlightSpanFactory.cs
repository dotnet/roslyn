// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal static class ClassifiedSpansAndHighlightSpanFactory
    {
        public static async Task<DocumentSpan> GetClassifiedDocumentSpanAsync(
            Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var classifiedSpans = await ClassifyAsync(
                document, sourceSpan, cancellationToken).ConfigureAwait(false);

            var properties = ImmutableDictionary<string, object>.Empty.Add(
                ClassifiedSpansAndHighlightSpan.Key, classifiedSpans);

            return new DocumentSpan(document, sourceSpan, properties);
        }

        public static async Task<ClassifiedSpansAndHighlightSpan> ClassifyAsync(
            DocumentSpan documentSpan, CancellationToken cancellationToken)
        {
            // If the document span is providing us with the classified spans up front, then we
            // can just use that.  Otherwise, go back and actually classify the text for the line
            // the document span is on.
            if (documentSpan.Properties != null &&
                documentSpan.Properties.TryGetValue(ClassifiedSpansAndHighlightSpan.Key, out var value))
            {
                return (ClassifiedSpansAndHighlightSpan)value;
            }

            return await ClassifyAsync(
                documentSpan.Document, documentSpan.SourceSpan, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ClassifiedSpansAndHighlightSpan> ClassifyAsync(
            Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var narrowSpan = sourceSpan;
            var lineSpan = GetLineSpanForReference(sourceText, narrowSpan);

            var taggedLineParts = await GetTaggedTextForDocumentRegionAsync(
                document, narrowSpan, lineSpan, cancellationToken).ConfigureAwait(false);
            return taggedLineParts;
        }

        private static TextSpan GetLineSpanForReference(SourceText sourceText, TextSpan referenceSpan)
        {
            var sourceLine = sourceText.Lines.GetLineFromPosition(referenceSpan.Start);
            var firstNonWhitespacePosition = sourceLine.GetFirstNonWhitespacePosition().Value;

            return TextSpan.FromBounds(firstNonWhitespacePosition, sourceLine.End);
        }

        private static async Task<ClassifiedSpansAndHighlightSpan> GetTaggedTextForDocumentRegionAsync(
            Document document, TextSpan narrowSpan, TextSpan widenedSpan, CancellationToken cancellationToken)
        {
            var highlightSpan = new TextSpan(
                start: narrowSpan.Start - widenedSpan.Start,
                length: narrowSpan.Length);

            var classifiedSpans = await GetClassifiedSpansAsync(
                document, narrowSpan, widenedSpan, cancellationToken).ConfigureAwait(false);
            return new ClassifiedSpansAndHighlightSpan(classifiedSpans, highlightSpan);
        }

        private static async Task<ImmutableArray<ClassifiedSpan>> GetClassifiedSpansAsync(
            Document document, TextSpan narrowSpan, TextSpan widenedSpan, CancellationToken cancellationToken)
        {
            var result = await ClassifierHelper.GetClassifiedSpansAsync(
                document, widenedSpan, cancellationToken).ConfigureAwait(false);
            if (!result.IsDefault)
            {
                return result;
            }

            // For languages that don't expose a classification service, we show the entire
            // item as plain text. Break the text into three spans so that we can properly
            // highlight the 'narrow-span' later on when we display the item.
            return ImmutableArray.Create(
                new ClassifiedSpan(ClassificationTypeNames.Text, TextSpan.FromBounds(widenedSpan.Start, narrowSpan.Start)),
                new ClassifiedSpan(ClassificationTypeNames.Text, narrowSpan),
                new ClassifiedSpan(ClassificationTypeNames.Text, TextSpan.FromBounds(narrowSpan.End, widenedSpan.End)));
        }
    }
}
