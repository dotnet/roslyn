// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.DocumentExcerpt;

internal static class DocumentExcerptHelper
{
    public static async Task<ImmutableArray<ClassifiedSpan>.Builder> ClassifyPreviewAsync(
        TextSpan excerptSpan,
        Document generatedDocument,
        ImmutableArray<SourceMapping> mappingsSortedByOriginal,
        RazorClassificationOptionsWrapper options,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<ClassifiedSpan>();

        // The algorithm here is to iterate through the source mappings (sorted) and use the C# classifier
        // on the spans that are known to be C#. For the spans that are not known to be C# then
        // we just treat them as text since we'd don't currently have our own classifications.

        if (excerptSpan.Length == 0)
        {
            return builder;
        }

        var remainingSpan = excerptSpan;
        foreach (var span in mappingsSortedByOriginal)
        {
            var primarySpan = span.OriginalSpan.AsTextSpan();
            if (primarySpan.Intersection(remainingSpan) is not TextSpan intersection)
            {
                if (primarySpan.Start > remainingSpan.End)
                {
                    // This span (and all following) are after the area we're interested in
                    break;
                }

                // This span precedes the area we're interested in
                continue;
            }

            // OK this span intersects with the excerpt span, so we will process it. Let's compute
            // the secondary span that matches the intersection.
            var secondarySpan = span.GeneratedSpan.AsTextSpan();
            secondarySpan = new TextSpan(secondarySpan.Start + intersection.Start - primarySpan.Start, intersection.Length);
            primarySpan = intersection;

            if (remainingSpan.Start < primarySpan.Start)
            {
                // The position is before the next C# span. Classify everything up to the C# start
                // as text.
                builder.Add(new ClassifiedSpan(ClassificationTypeNames.Text, new TextSpan(remainingSpan.Start, primarySpan.Start - remainingSpan.Start)));

                // Advance to the start of the C# span.
                remainingSpan = new TextSpan(primarySpan.Start, remainingSpan.Length - (primarySpan.Start - remainingSpan.Start));
            }

            // We should be able to process this whole span as C#, so classify it.
            //
            // However, we'll have to translate it to the the generated document's coordinates to do that.
            Debug.Assert(remainingSpan.Contains(primarySpan) && remainingSpan.Start == primarySpan.Start);
            var classifiedSecondarySpans = await RazorClassifierAccessor.GetClassifiedSpansAsync(
                generatedDocument,
                secondarySpan,
                options,
                cancellationToken).ConfigureAwait(false);

            // NOTE: The Classifier will only returns spans for things that it understands. That means
            // that whitespace is not classified. The preview expects us to provide contiguous spans,
            // so we are going to have to fill in the gaps.

            // Now we have to translate back to the primary document's coordinates.
            var offset = primarySpan.Start - secondarySpan.Start;
            foreach (var classifiedSecondarySpan in classifiedSecondarySpans)
            {
                // It's possible for the classified span to extend past our secondary span, so we cap it
                var classifiedSpan = classifiedSecondarySpan.TextSpan.End > secondarySpan.End
                    ? TextSpan.FromBounds(classifiedSecondarySpan.TextSpan.Start, secondarySpan.End)
                    : classifiedSecondarySpan.TextSpan;
                Debug.Assert(secondarySpan.Contains(classifiedSpan));

                var updated = new TextSpan(classifiedSpan.Start + offset, classifiedSpan.Length);
                Debug.Assert(primarySpan.Contains(updated));

                // Make sure that we're not introducing a gap. Remember, we need to fill in the whitespace.
                if (remainingSpan.Start < updated.Start)
                {
                    builder.Add(new ClassifiedSpan(
                        ClassificationTypeNames.Text,
                        new TextSpan(remainingSpan.Start, updated.Start - remainingSpan.Start)));
                    remainingSpan = new TextSpan(updated.Start, remainingSpan.Length - (updated.Start - remainingSpan.Start));
                }

                builder.Add(new ClassifiedSpan(classifiedSecondarySpan.ClassificationType, updated));
                remainingSpan = new TextSpan(updated.End, remainingSpan.Length - (updated.End - remainingSpan.Start));
            }

            // Make sure that we're not introducing a gap. Remember, we need to fill in the whitespace.
            if (remainingSpan.Start < primarySpan.End)
            {
                builder.Add(new ClassifiedSpan(
                    ClassificationTypeNames.Text,
                    new TextSpan(remainingSpan.Start, primarySpan.End - remainingSpan.Start)));
                remainingSpan = new TextSpan(primarySpan.End, remainingSpan.Length - (primarySpan.End - remainingSpan.Start));
            }
        }

        // Deal with residue
        if (remainingSpan.Length > 0)
        {
            // Trailing Razor/markup text.
            builder.Add(new ClassifiedSpan(ClassificationTypeNames.Text, remainingSpan));
        }

        return builder;
    }

    public static TextSpan ChooseExcerptSpan(SourceText text, TextSpan span, RazorExcerptMode mode)
    {
        var startLine = text.Lines.GetLineFromPosition(span.Start);
        var endLine = text.Lines.GetLineFromPosition(span.End);

        if (mode == RazorExcerptMode.Tooltip)
        {
            // Expand the range by 3 in each direction (if possible).
            var startIndex = Math.Max(startLine.LineNumber - 3, 0);
            startLine = text.Lines[startIndex];

            var endIndex = Math.Min(endLine.LineNumber + 3, text.Lines.Count - 1);
            endLine = text.Lines[endIndex];
            return CreateTextSpan(startLine, endLine);
        }
        else
        {
            // Trim leading whitespace in a single line excerpt
            var excerptSpan = CreateTextSpan(startLine, endLine);
            var trimmedExcerptSpan = excerptSpan.TrimLeadingWhitespace(text);
            return trimmedExcerptSpan;
        }

        static TextSpan CreateTextSpan(TextLine startLine, TextLine endLine)
        {
            return new TextSpan(startLine.Start, endLine.End - startLine.Start);
        }
    }

    public static SourceText GetTranslatedExcerptText(
        SourceText razorDocumentText,
        ref TextSpan razorDocumentSpan,
        ref TextSpan excerptSpan,
        ImmutableArray<ClassifiedSpan>.Builder classifiedSpans)
    {
        // Now translate everything to be relative to the excerpt
        var offset = 0 - excerptSpan.Start;
        var excerptText = razorDocumentText.GetSubText(excerptSpan);
        excerptSpan = new TextSpan(0, excerptSpan.Length);
        razorDocumentSpan = new TextSpan(razorDocumentSpan.Start + offset, razorDocumentSpan.Length);

        for (var i = 0; i < classifiedSpans.Count; i++)
        {
            var classifiedSpan = classifiedSpans[i];
            var updated = new TextSpan(classifiedSpan.TextSpan.Start + offset, classifiedSpan.TextSpan.Length);
            Debug.Assert(excerptSpan.Contains(updated));

            classifiedSpans[i] = new ClassifiedSpan(classifiedSpan.ClassificationType, updated);
        }

        return excerptText;
    }
}
