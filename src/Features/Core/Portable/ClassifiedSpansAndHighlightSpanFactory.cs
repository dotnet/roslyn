// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal static class ClassifiedSpansAndHighlightSpanFactory
{
    public static async Task<ClassifiedSpansAndHighlightSpan> ClassifyAsync(
        DocumentSpan documentSpan, ClassifiedSpansAndHighlightSpan? classifiedSpans, ClassificationOptions options, CancellationToken cancellationToken)
    {
        // If the document span is providing us with the classified spans up front, then we
        // can just use that.  Otherwise, go back and actually classify the text for the line
        // the document span is on.
        if (classifiedSpans != null)
            return classifiedSpans.Value;

        return await ClassifyAsync(
            documentSpan.Document, documentSpan.SourceSpan, options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ClassifiedSpansAndHighlightSpan> ClassifyAsync(
        Document document, TextSpan sourceSpan, ClassificationOptions options, CancellationToken cancellationToken)
    {
        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var narrowSpan = sourceSpan;
        var lineSpan = GetLineSpanForReference(sourceText, narrowSpan);

        var taggedLineParts = await GetTaggedTextForDocumentRegionAsync(
            document, narrowSpan, lineSpan, options, cancellationToken).ConfigureAwait(false);
        return taggedLineParts;
    }

    private static TextSpan GetLineSpanForReference(SourceText sourceText, TextSpan referenceSpan)
    {
        var sourceLine = sourceText.Lines.GetLineFromPosition(referenceSpan.Start);
        var firstNonWhitespacePosition = sourceLine.GetFirstNonWhitespacePosition().Value;

        // Get the span of the line from the first non-whitespace character to the end of it. Note: the reference
        // span might actually start in the leading whitespace of the line (nothing prevents any of our
        // languages/providers from doing that), so ensure that the line snap we clip out at least starts at that
        // position so that our span math will be correct.
        return TextSpan.FromBounds(Math.Min(firstNonWhitespacePosition, referenceSpan.Start), sourceLine.End);
    }

    private static async Task<ClassifiedSpansAndHighlightSpan> GetTaggedTextForDocumentRegionAsync(
        Document document, TextSpan narrowSpan, TextSpan widenedSpan, ClassificationOptions options, CancellationToken cancellationToken)
    {
        var highlightSpan = new TextSpan(
            start: narrowSpan.Start - widenedSpan.Start,
            length: narrowSpan.Length);

        var classifiedSpans = await GetClassifiedSpansAsync(
            document, narrowSpan, widenedSpan, options, cancellationToken).ConfigureAwait(false);
        return new ClassifiedSpansAndHighlightSpan(classifiedSpans, highlightSpan);
    }

    private static async Task<ImmutableArray<ClassifiedSpan>> GetClassifiedSpansAsync(
        Document document, TextSpan narrowSpan, TextSpan widenedSpan, ClassificationOptions options, CancellationToken cancellationToken)
    {
        // We don't present things like static/assigned variables differently.  So pass `includeAdditiveSpans:
        // false` as we don't need that data.
        var result = await ClassifierHelper.GetClassifiedSpansAsync(
            document, widenedSpan, options, includeAdditiveSpans: false, cancellationToken).ConfigureAwait(false);
        if (!result.IsDefault)
            return result;

        // For languages that don't expose a classification service, we show the entire
        // item as plain text. Break the text into three spans so that we can properly
        // highlight the 'narrow-span' later on when we display the item.
        return
        [
            new ClassifiedSpan(ClassificationTypeNames.Text, TextSpan.FromBounds(widenedSpan.Start, narrowSpan.Start)),
            new ClassifiedSpan(ClassificationTypeNames.Text, narrowSpan),
            new ClassifiedSpan(ClassificationTypeNames.Text, TextSpan.FromBounds(narrowSpan.End, widenedSpan.End)),
        ];
    }
}
