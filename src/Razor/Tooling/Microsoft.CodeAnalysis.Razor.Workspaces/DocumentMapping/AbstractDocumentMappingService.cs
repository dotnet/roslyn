// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal abstract class AbstractDocumentMappingService(ILogger logger) : IDocumentMappingService
{
    protected readonly ILogger Logger = logger;

    public bool TryMapToRazorDocumentRange(RazorCSharpDocument csharpDocument, LinePositionSpan csharpRange, MappingBehavior mappingBehavior, out LinePositionSpan razorRange)
    {
        if (mappingBehavior == MappingBehavior.Strict)
        {
            return TryMapToRazorDocumentRangeStrict(csharpDocument, csharpRange, out razorRange);
        }
        else if (mappingBehavior == MappingBehavior.Inclusive)
        {
            return TryMapToRazorDocumentRangeInclusive(csharpDocument, csharpRange, out razorRange);
        }
        else if (mappingBehavior == MappingBehavior.Inferred)
        {
            return TryMapToRazorDocumentRangeInferred(csharpDocument, csharpRange, out razorRange);
        }
        else
        {
            throw new InvalidOperationException(SR.Unknown_mapping_behavior);
        }
    }

    public bool TryMapToCSharpDocumentRange(RazorCSharpDocument csharpDocument, LinePositionSpan razorRange, out LinePositionSpan csharpRange)
    {
        csharpRange = default;

        if (razorRange.End.Line < razorRange.Start.Line ||
            (razorRange.End.Line == razorRange.Start.Line &&
             razorRange.End.Character < razorRange.Start.Character))
        {
            Logger.LogWarning($"RazorDocumentMappingService:TryMapToGeneratedDocumentRange original range end < start '{razorRange}'");
            Debug.Fail($"RazorDocumentMappingService:TryMapToGeneratedDocumentRange original range end < start '{razorRange}'");
            return false;
        }

        var sourceText = csharpDocument.CodeDocument.Source.Text;
        var range = razorRange;
        if (!IsSpanWithinDocument(range, sourceText))
        {
            return false;
        }

        if (!sourceText.TryGetAbsoluteIndex(range.Start, out var startIndex) ||
            !TryMapToCSharpDocumentPosition(csharpDocument, startIndex, out var generatedRangeStart, out _))
        {
            return false;
        }

        if (!sourceText.TryGetAbsoluteIndex(range.End, out var endIndex) ||
            !TryMapToCSharpDocumentPosition(csharpDocument, endIndex, out var generatedRangeEnd, out _))
        {
            return false;
        }

        // Ensures a valid range is returned.
        // As we're doing two separate TryMapToGeneratedDocumentPosition calls,
        // it's possible the generatedRangeStart and generatedRangeEnd positions are in completely
        // different places in the document, including the possibility that the
        // generatedRangeEnd position occurs before the generatedRangeStart position.
        // We explicitly disallow such ranges where the end < start.
        if (generatedRangeEnd < generatedRangeStart)
        {
            return false;
        }

        csharpRange = new LinePositionSpan(generatedRangeStart, generatedRangeEnd);

        return true;
    }

    public ImmutableArray<LinePositionSpan> GetCSharpSpansOverlappingRazorSpan(RazorCSharpDocument csharpDocument, LinePositionSpan razorSpan)
    {
        var sourceText = csharpDocument.CodeDocument.Source.Text;
        if (!IsSpanWithinDocument(razorSpan, sourceText))
        {
            return [];
        }

        using var builder = new PooledArrayBuilder<LinePositionSpan>();

        foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
        {
            var originalSpan = mapping.OriginalSpan.ToLinePositionSpan();

            if (razorSpan.OverlapsWith(originalSpan))
            {
                var generatedSpan = mapping.GeneratedSpan.ToLinePositionSpan();

                builder.Add(generatedSpan);
            }
            else if (originalSpan.Start > razorSpan.End)
            {
                // This span (and all following) are after the area we're interested in
                break;
            }
        }

        return builder.ToImmutableAndClear();
    }

    public bool TryMapToRazorDocumentPosition(RazorCSharpDocument csharpDocument, int csharpIndex, out LinePosition razorPosition, out int razorIndex)
    {
        var sourceMappings = csharpDocument.SourceMappingsSortedByGenerated;

        var index = sourceMappings.BinarySearchBy(csharpIndex, static (mapping, generatedDocumentIndex) =>
        {
            var generatedSpan = mapping.GeneratedSpan;
            var generatedAbsoluteIndex = generatedSpan.AbsoluteIndex;
            if (generatedAbsoluteIndex <= generatedDocumentIndex)
            {
                var distanceIntoGeneratedSpan = generatedDocumentIndex - generatedAbsoluteIndex;
                if (distanceIntoGeneratedSpan <= generatedSpan.Length)
                {
                    return 0;
                }

                return -1;
            }

            return 1;
        });

        if (index >= 0)
        {
            var mapping = sourceMappings[index];

            var generatedAbsoluteIndex = mapping.GeneratedSpan.AbsoluteIndex;
            var distanceIntoGeneratedSpan = csharpIndex - generatedAbsoluteIndex;

            razorIndex = mapping.OriginalSpan.AbsoluteIndex + distanceIntoGeneratedSpan;
            razorPosition = csharpDocument.CodeDocument.Source.Text.GetLinePosition(razorIndex);
            return true;
        }

        razorPosition = default;
        razorIndex = default;
        return false;
    }

    public bool TryMapToCSharpPositionOrNext(RazorCSharpDocument csharpDocument, int hostDocumentIndex, out LinePosition generatedPosition, out int generatedIndex)
        => TryMapToCSharpDocumentPositionInternal(csharpDocument, hostDocumentIndex, nextCSharpPositionOnFailure: true, out generatedPosition, out generatedIndex);

    public bool TryMapToCSharpDocumentPosition(RazorCSharpDocument csharpDocument, int hostDocumentIndex, out LinePosition generatedPosition, out int generatedIndex)
        => TryMapToCSharpDocumentPositionInternal(csharpDocument, hostDocumentIndex, nextCSharpPositionOnFailure: false, out generatedPosition, out generatedIndex);

    private static bool TryMapToCSharpDocumentPositionInternal(RazorCSharpDocument csharpDocument, int razorIndex, bool nextCSharpPositionOnFailure, out LinePosition csharpPosition, out int csharpIndex)
    {
        SourceMapping? nextCSharpMapping = null;

        var hostDocumentLine = csharpDocument.CodeDocument.Source.Text.GetLinePosition(razorIndex).Line;

        foreach (var mapping in csharpDocument.SourceMappingsSortedByOriginal)
        {
            var originalSpan = mapping.OriginalSpan;
            var originalAbsoluteIndex = originalSpan.AbsoluteIndex;
            if (originalAbsoluteIndex <= razorIndex)
            {
                // Treat the mapping as owning the edge at its end (hence <= originalSpan.Length),
                // otherwise we wouldn't handle the cursor being right after the final C# char
                var distanceIntoOriginalSpan = razorIndex - originalAbsoluteIndex;
                if (distanceIntoOriginalSpan <= originalSpan.Length)
                {
                    csharpIndex = mapping.GeneratedSpan.AbsoluteIndex + distanceIntoOriginalSpan;
                    csharpPosition = csharpDocument.Text.GetLinePosition(csharpIndex);
                    return true;
                }
            }
            else if (nextCSharpPositionOnFailure &&
                mapping.OriginalSpan.LineIndex == hostDocumentLine &&
                mapping.OriginalSpan.AbsoluteIndex >= razorIndex &&
                (nextCSharpMapping is null || mapping.OriginalSpan.AbsoluteIndex < nextCSharpMapping.OriginalSpan.AbsoluteIndex))
            {
                // The "next" C# location is only valid if it is on the same line in the source document
                // as the requested position, and before than any previous "next" C# position we have found,
                // comparing their original positions.  Due to source mappings being ordered by generated span,
                // not original span, its possible for things to be out of order.
                nextCSharpMapping = mapping;
            }
            else
            {
                // This span (and all following) are after the area we're interested in
                break;
            }
        }

        if (nextCSharpPositionOnFailure && nextCSharpMapping is not null)
        {
            csharpIndex = nextCSharpMapping.GeneratedSpan.AbsoluteIndex;
            csharpPosition = csharpDocument.Text.GetLinePosition(csharpIndex);
            return true;
        }

        csharpPosition = default;
        csharpIndex = default;
        return false;
    }

    private bool TryMapToRazorDocumentRangeStrict(RazorCSharpDocument csharpDocument, LinePositionSpan csharpRange, out LinePositionSpan razorRange)
    {
        razorRange = default;

        var csharpSourceText = csharpDocument.Text;
        var range = csharpRange;
        if (!IsSpanWithinDocument(range, csharpSourceText))
        {
            return false;
        }

        if (!csharpSourceText.TryGetAbsoluteIndex(range.Start, out var startIndex) ||
            !TryMapToRazorDocumentPosition(csharpDocument, startIndex, out var hostDocumentStart, out _))
        {
            return false;
        }

        if (!csharpSourceText.TryGetAbsoluteIndex(range.End, out var endIndex) ||
            !TryMapToRazorDocumentPosition(csharpDocument, endIndex, out var hostDocumentEnd, out _))
        {
            return false;
        }

        // Ensures a valid range is returned, as we're doing two separate TryMapToGeneratedDocumentPosition calls.
        if (hostDocumentEnd < hostDocumentStart)
        {
            return false;
        }

        razorRange = new LinePositionSpan(hostDocumentStart, hostDocumentEnd);

        return true;
    }

    private bool TryMapToRazorDocumentRangeInclusive(RazorCSharpDocument csharpDocument, LinePositionSpan csharpRange, out LinePositionSpan rangeRange)
    {
        rangeRange = default;

        var csharpSourceText = csharpDocument.Text;

        if (!IsSpanWithinDocument(csharpRange, csharpSourceText))
        {
            return false;
        }

        var startIndex = csharpSourceText.GetRequiredAbsoluteIndex(csharpRange.Start);
        var startMappedDirectly = TryMapToRazorDocumentPosition(csharpDocument, startIndex, out var hostDocumentStart, out _);

        var endIndex = csharpSourceText.GetRequiredAbsoluteIndex(csharpRange.End);
        var endMappedDirectly = TryMapToRazorDocumentPosition(csharpDocument, endIndex, out var hostDocumentEnd, out _);

        if (startMappedDirectly && endMappedDirectly && hostDocumentStart <= hostDocumentEnd)
        {
            // We strictly mapped the start/end of the generated range.
            rangeRange = new LinePositionSpan(hostDocumentStart, hostDocumentEnd);
            return true;
        }

        using var _1 = ListPool<SourceMapping>.GetPooledObject(out var candidateMappings);
        var sourceMappings = csharpDocument.SourceMappingsSortedByGenerated;
        if (startMappedDirectly)
        {
            // Start of generated range intersects with a mapping
            candidateMappings.AddRange(
                sourceMappings.Where(mapping => IntersectsWith(startIndex, mapping.GeneratedSpan)));
        }
        else if (endMappedDirectly)
        {
            // End of generated range intersects with a mapping
            candidateMappings.AddRange(
                sourceMappings.Where(mapping => IntersectsWith(endIndex, mapping.GeneratedSpan)));
        }
        else
        {
            // Our range does not intersect with any mapping; we should see if it overlaps generated locations
            candidateMappings.AddRange(
                sourceMappings
                    .Where(mapping => Overlaps(csharpSourceText.GetTextSpan(csharpRange), mapping.GeneratedSpan)));
        }

        if (candidateMappings.Count == 1)
        {
            // We're intersecting or overlapping a single mapping, lets choose that.

            var mapping = candidateMappings[0];
            rangeRange = csharpDocument.CodeDocument.Source.Text.GetLinePositionSpan(mapping.OriginalSpan);
            return true;
        }
        else
        {
            // More then 1 or exactly 0 intersecting/overlapping mappings
            return false;
        }

        bool Overlaps(TextSpan generatedRangeAsSpan, SourceSpan span)
        {
            var overlapStart = Math.Max(generatedRangeAsSpan.Start, span.AbsoluteIndex);
            var overlapEnd = Math.Min(generatedRangeAsSpan.End, span.AbsoluteIndex + span.Length);

            return overlapStart < overlapEnd;
        }

        bool IntersectsWith(int position, SourceSpan span)
        {
            return unchecked((uint)(position - span.AbsoluteIndex) <= (uint)span.Length);
        }
    }

    private bool TryMapToRazorDocumentRangeInferred(RazorCSharpDocument csharpDocument, LinePositionSpan csharpRange, out LinePositionSpan razorRange)
    {
        // Inferred mapping behavior is a superset of inclusive mapping behavior so if the range is "inclusive" lets use that mapping.
        if (TryMapToRazorDocumentRangeInclusive(csharpDocument, csharpRange, out razorRange))
        {
            return true;
        }

        // Doesn't map so lets try and infer some mappings

        razorRange = default;
        var csharpSourceText = csharpDocument.Text;

        if (!IsSpanWithinDocument(csharpRange, csharpSourceText))
        {
            return false;
        }

        var generatedRangeAsSpan = csharpSourceText.GetTextSpan(csharpRange);
        SourceMapping? mappingBeforeGeneratedRange = null;
        SourceMapping? mappingAfterGeneratedRange = null;
        var sourceMappings = csharpDocument.SourceMappingsSortedByGenerated;

        for (var i = sourceMappings.Length - 1; i >= 0; i--)
        {
            var sourceMapping = sourceMappings[i];
            var sourceMappingEnd = sourceMapping.GeneratedSpan.AbsoluteIndex + sourceMapping.GeneratedSpan.Length;
            if (generatedRangeAsSpan.Start >= sourceMappingEnd)
            {
                // This is the source mapping that's before us!
                mappingBeforeGeneratedRange = sourceMapping;

                if (i + 1 < sourceMappings.Length)
                {
                    // We're not at the end of the document there's another source mapping after us
                    mappingAfterGeneratedRange = sourceMappings[i + 1];
                }

                break;
            }
        }

        if (mappingBeforeGeneratedRange == null)
        {
            // Could not find a mapping before
            return false;
        }

        var sourceDocument = csharpDocument.CodeDocument.Source;
        var originalSpanBeforeGeneratedRange = mappingBeforeGeneratedRange.OriginalSpan;
        var originalEndBeforeGeneratedRange = originalSpanBeforeGeneratedRange.AbsoluteIndex + originalSpanBeforeGeneratedRange.Length;
        var inferredStartPosition = sourceDocument.Text.GetLinePosition(originalEndBeforeGeneratedRange);

        if (mappingAfterGeneratedRange != null)
        {
            // There's a mapping after the "generated range" lets use its start position as our inferred end position.

            var originalSpanAfterGeneratedRange = mappingAfterGeneratedRange.OriginalSpan;
            var originalStartPositionAfterGeneratedRange = sourceDocument.Text.GetLinePosition(originalSpanAfterGeneratedRange.AbsoluteIndex);

            // The mapping in the generated file is after the start, but when mapped back to the host file that may not be true
            if (originalStartPositionAfterGeneratedRange >= inferredStartPosition)
            {
                razorRange = new LinePositionSpan(inferredStartPosition, originalStartPositionAfterGeneratedRange);
                return true;
            }
        }

        // There was no projection after the "generated range". Therefore, lets fallback to the end-document location.

        Debug.Assert(sourceDocument.Text.Length > 0, "Source document length should be greater than 0 here because there's a mapping before us");

        var endOfDocumentPosition = sourceDocument.Text.GetLinePosition(sourceDocument.Text.Length);

        Debug.Assert(endOfDocumentPosition >= inferredStartPosition, "Some how we found a start position that is after the end of the document?");

        razorRange = new LinePositionSpan(inferredStartPosition, endOfDocumentPosition);
        return true;
    }

    private static bool s_haveAsserted = false;

    private bool IsSpanWithinDocument(LinePositionSpan span, SourceText sourceText)
    {
        // This might happen when the document that ranges were created against was not the same as the document we're consulting.
        var result = IsPositionWithinDocument(span.Start, sourceText) && IsPositionWithinDocument(span.End, sourceText);

        if (!s_haveAsserted && !result)
        {
            s_haveAsserted = true;
            var sourceTextLinesCount = sourceText.Lines.Count;
            Logger.LogWarning($"Attempted to map a range ({span.Start.Line},{span.Start.Character})-({span.End.Line},{span.End.Character}) outside of the Source (line count {sourceTextLinesCount}.) This could happen if the Roslyn and Razor LSP servers are not in sync.");
        }

        return result;

        static bool IsPositionWithinDocument(LinePosition linePosition, SourceText sourceText)
        {
            return sourceText.TryGetAbsoluteIndex(linePosition, out _);
        }
    }
}
