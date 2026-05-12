// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.FoldingRanges;

internal partial class FoldingRangeService(
    IDocumentMappingService documentMappingService,
    IEnumerable<IRazorFoldingRangeProvider> foldingRangeProviders,
    ILoggerFactory loggerFactory)
    : IFoldingRangeService
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IEnumerable<IRazorFoldingRangeProvider> _foldingRangeProviders = foldingRangeProviders;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<FoldingRangeService>();

    public ImmutableArray<FoldingRange> GetFoldingRanges(RazorCodeDocument codeDocument, FoldingRange[] csharpRanges, ImmutableArray<FoldingRange> htmlRanges, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilderPool<FoldingRange>.GetPooledObject(out var mappedRanges);

        // We have no idea how many ranges we'll end up with, because we expect to filter out a lot of C# ranges,
        // but we will at least have one per html range so can avoid some initial resizing of the backing data store.
        mappedRanges.SetCapacityIfLarger(htmlRanges.Length);

        var csharpDocument = codeDocument.GetRequiredCSharpDocument();

        foreach (var foldingRange in csharpRanges)
        {
            var span = GetLinePositionSpan(foldingRange);

            if (_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, span, out var mappedSpan))
            {
                foldingRange.StartLine = mappedSpan.Start.Line;
                foldingRange.StartCharacter = mappedSpan.Start.Character;
                foldingRange.EndLine = mappedSpan.End.Line;
                foldingRange.EndCharacter = mappedSpan.End.Character;

                mappedRanges.Add(foldingRange);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Html ranges don't need mapping. Yay!
        mappedRanges.AddRange(htmlRanges);

        foreach (var provider in _foldingRangeProviders)
        {
            var ranges = provider.GetFoldingRanges(codeDocument);
            mappedRanges.AddRange(ranges);
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Reduce ranges to only multi-line ranges, and preferring the largest range if there is more than
        // one on a single line.
        using var _1 = DictionaryPool<int, FoldingRange>.GetPooledObject(out var reducedRanges);
        foreach (var mappedRange in mappedRanges)
        {
            // Don't allow ranges to be reported if they aren't spanning at least one line
            if (mappedRange.StartLine == mappedRange.EndLine)
            {
                continue;
            }

            // Reduce ranges with the same StartLine, preferring larger ranges.
            if (!reducedRanges.TryGetValue(mappedRange.StartLine, out var existingRange) ||
                RangeContains(mappedRange, existingRange))
            {
                reducedRanges[mappedRange.StartLine] = mappedRange;
            }
        }

        // Fix the starting range so the "..." is shown at the end
        return reducedRanges.Values.SelectAsArray(r => FixFoldingRangeStart(r, codeDocument));
    }

    private static bool RangeContains(FoldingRange x, FoldingRange y)
    {
        if (x.StartLine > y.StartLine ||
            x.EndLine < y.EndLine)
        {
            return false;
        }

        if (x.StartLine == y.StartLine &&
            x.StartCharacter.GetValueOrDefault() > y.StartCharacter.GetValueOrDefault())
        {
            return false;
        }

        if (x.EndLine == y.EndLine &&
            x.EndCharacter.GetValueOrDefault() < y.EndCharacter.GetValueOrDefault())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Fixes the start of a range so that the offset of the first line is the last character on that line. This makes
    /// it so collapsing will still show the text instead of just "..."
    /// </summary>
    private FoldingRange FixFoldingRangeStart(FoldingRange range, RazorCodeDocument codeDocument)
    {
        Debug.Assert(range.StartLine < range.EndLine);

        var sourceText = codeDocument.Source.Text;
        var startLine = range.StartLine;

        if (startLine >= sourceText.Lines.Count)
        {
            // Sometimes VS Code seems to send us wildly out-of-range folding ranges for Html, so log a warning,
            // but prevent a toast from appearing from an exception.
            _logger.LogWarning($"Got a folding range of ({range.StartLine}-{range.EndLine}) but Razor document {codeDocument.Source.FilePath} only has {sourceText.Lines.Count} lines.");
            return range;
        }

        var lineSpan = sourceText.Lines[startLine].Span;

        // Search from the end of the line to the beginning for the first non whitespace character. We want that
        // to be the offset for the range
        if (sourceText.TryGetLastNonWhitespaceOffset(lineSpan, out var offset))
        {
            // +1 to the offset value because the helper goes to the character position
            // that we want to be after. Make sure we don't exceed the line end
            var newCharacter = Math.Min(offset + 1, lineSpan.Length);

            range.StartCharacter = newCharacter;
            range.CollapsedText = null; // Let the client decide what to show
            return range;
        }

        return range;
    }

    private static LinePositionSpan GetLinePositionSpan(FoldingRange foldingRange)
        => new(new(foldingRange.StartLine, foldingRange.StartCharacter.GetValueOrDefault()), new(foldingRange.EndLine, foldingRange.EndCharacter.GetValueOrDefault()));
}
