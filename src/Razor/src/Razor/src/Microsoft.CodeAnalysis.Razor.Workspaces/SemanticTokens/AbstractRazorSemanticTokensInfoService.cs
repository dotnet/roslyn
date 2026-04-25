// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal abstract partial class AbstractRazorSemanticTokensInfoService(
    IDocumentMappingService documentMappingService,
    ISemanticTokensLegendService semanticTokensLegendService,
    ICSharpSemanticTokensProvider csharpSemanticTokensProvider,
    ILogger logger)
    : IRazorSemanticTokensInfoService
{
    private const int TokenSize = 5;

    // Use a custom pool as these lists commonly exceed the size threshold for returning into the default ListPool.
    // These lists are significantly larger than DefaultPool.MaximumObjectSize as these arrays are commonly large.
    // The 2048 limit should be large enough for nearly all semantic token requests, while still
    // keeping the backing arrays off the LOH.

    private static readonly ListPool<SemanticRange> s_pool = ListPool<SemanticRange>.Create(maximumObjectSize: 2048, poolSize: 8);

    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly ISemanticTokensLegendService _semanticTokensLegendService = semanticTokensLegendService;
    private readonly ICSharpSemanticTokensProvider _csharpSemanticTokensProvider = csharpSemanticTokensProvider;
    private readonly ILogger _logger = logger;

    public async Task<int[]?> GetSemanticTokensAsync(
        DocumentContext documentContext,
        LinePositionSpan span,
        bool colorBackground,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var semanticTokens = await GetSemanticTokensAsync(documentContext, span, correlationId, colorBackground, cancellationToken).ConfigureAwait(false);

        var amount = semanticTokens is null ? "no" : (semanticTokens.Length / TokenSize).ToString(Thread.CurrentThread.CurrentCulture);

        _logger.LogDebug($"Returned {amount} semantic tokens for span {span} in {documentContext.Uri}.");

        if (semanticTokens is not null)
        {
            Debug.Assert(semanticTokens.Length % TokenSize == 0, $"Number of semantic token-ints should be divisible by {TokenSize}. Actual number: {semanticTokens.Length}");
            Debug.Assert(semanticTokens.Length == 0 || semanticTokens[0] >= 0, $"Line offset should not be negative.");
        }

        return semanticTokens;
    }

    private async Task<int[]?> GetSemanticTokensAsync(
        DocumentContext documentContext,
        LinePositionSpan span,
        Guid correlationId,
        bool colorBackground,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var textSpan = codeDocument.Source.Text.GetTextSpan(span);
        using var _ = s_pool.GetPooledObject(out var combinedSemanticRanges);

        SemanticTokensVisitor.AddSemanticRanges(combinedSemanticRanges, codeDocument, textSpan, _semanticTokensLegendService, colorBackground);
        Debug.Assert(combinedSemanticRanges.SequenceEqual(combinedSemanticRanges.OrderBy(g => g)));

        var successfullyRetrievedCSharpSemanticRanges = false;

        try
        {
            successfullyRetrievedCSharpSemanticRanges = await AddCSharpSemanticRangesAsync(combinedSemanticRanges, documentContext, codeDocument, span, colorBackground, correlationId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error thrown while retrieving CSharp semantic range.");
        }

        // Didn't get any C# tokens, likely because the user kept typing and a future semantic tokens request will occur.
        // We return null (which to the LSP is a no-op) to prevent flashing of CSharp elements.
        if (!successfullyRetrievedCSharpSemanticRanges)
        {
            _logger.LogDebug($"Couldn't get C# tokens for version {documentContext.Snapshot.Version} of {documentContext.Uri}. Returning null");
            return null;
        }

        // If we have both types of tokens then we need to sort them all together, even though we know the Razor ranges will be sorted already,
        // because they can arbitrarily interleave. The SemanticRange.CompareTo method also has some logic to ensure that if Razor and C# ranges
        // are equivalent, the Razor range will be ordered first, so we can later drop the C# range, and prefer our classification over C#s.
        // Additionally, as mentioned above, the C# ranges are not guaranteed to be in order
        combinedSemanticRanges.Sort();

        return ConvertSemanticRangesToSemanticTokensData(combinedSemanticRanges, codeDocument);
    }

    // Virtual for benchmarks
    protected virtual async Task<bool> AddCSharpSemanticRangesAsync(
        List<SemanticRange> ranges,
        DocumentContext documentContext,
        RazorCodeDocument codeDocument,
        LinePositionSpan razorSpan,
        bool colorBackground,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var generatedDocument = codeDocument.GetRequiredCSharpDocument();

        // Get a list of precise ranges for the C# code embedded in the Razor document.
        if (!TryGetSortedCSharpRanges(codeDocument, razorSpan, out var csharpRanges))
        {
            // There's no C# in the range.
            return true;
        }

        _logger.LogDebug($"Requesting C# semantic tokens for host version {documentContext.Snapshot.Version}, correlation ID {correlationId}, and the server thinks there are {codeDocument.GetCSharpSourceText().Lines.Count} lines of C#");

        var csharpResponse = await _csharpSemanticTokensProvider.GetCSharpSemanticTokensResponseAsync(documentContext, csharpRanges, correlationId, cancellationToken).ConfigureAwait(false);

        // Indicates an issue with retrieving the C# response (e.g. no response or C# is out of sync with us).
        // Unrecoverable, return default to indicate no change. We've already queued up a refresh request in
        // the server call that will cause us to retry in a bit.
        if (csharpResponse is null)
        {
            return false;
        }

        ranges.SetCapacityIfLarger(csharpResponse.Length / TokenSize);

        var textClassification = _semanticTokensLegendService.TokenTypes.MarkupTextLiteral;
        var razorSource = codeDocument.Source.Text;

        SemanticRange previousSemanticRange = default;
        LinePositionSpan? previousRazorSemanticRange = null;

        for (var i = 0; i < csharpResponse.Length; i += TokenSize)
        {
            var lineDelta = csharpResponse[i];
            var charDelta = csharpResponse[i + 1];
            var length = csharpResponse[i + 2];
            var tokenType = csharpResponse[i + 3];
            var tokenModifiers = csharpResponse[i + 4];

            var semanticRange = CSharpDataToSemanticRange(lineDelta, charDelta, length, tokenType, tokenModifiers, previousSemanticRange);
            if (_documentMappingService.TryMapToRazorDocumentRange(generatedDocument, semanticRange.AsLinePositionSpan(), out var originalRange))
            {
                if (razorSpan.OverlapsWith(originalRange))
                {
                    if (colorBackground)
                    {
                        tokenModifiers |= _semanticTokensLegendService.TokenModifiers.RazorCodeModifier;
                        AddAdditionalCSharpWhitespaceRanges(ranges, textClassification, razorSource, previousRazorSemanticRange, originalRange);
                    }

                    ranges.Add(new SemanticRange(semanticRange.Kind, originalRange.Start.Line, originalRange.Start.Character, originalRange.End.Line, originalRange.End.Character, tokenModifiers, fromRazor: false));
                }

                previousRazorSemanticRange = originalRange;
            }

            previousSemanticRange = semanticRange;
        }

        return true;
    }

    private void AddAdditionalCSharpWhitespaceRanges(List<SemanticRange> razorRanges, int textClassification, SourceText razorSource, LinePositionSpan? previousRazorSemanticRange, LinePositionSpan originalRange)
    {
        var startLine = originalRange.Start.Line;
        var startChar = originalRange.Start.Character;
        if (previousRazorSemanticRange is { } previousRange &&
            previousRange.End.Line == startLine &&
            previousRange.End.Character < startChar &&
            razorSource.TryGetAbsoluteIndex(previousRange.End, out var previousSpanEndIndex) &&
            ContainsOnlySpacesOrTabs(razorSource, previousSpanEndIndex + 1, startChar - previousRange.End.Character - 1))
        {
            // we're on the same line as previous, lets extend ours to include whitespace between us and the proceeding range
            razorRanges.Add(new SemanticRange(textClassification, startLine, previousRange.End.Character, startLine, startChar, _semanticTokensLegendService.TokenModifiers.RazorCodeModifier, fromRazor: false, isCSharpWhitespace: true));
        }
        else if (startChar > 0 &&
            previousRazorSemanticRange?.End.Line != startLine &&
            razorSource.TryGetAbsoluteIndex(originalRange.Start, out var originalRangeStartIndex) &&
            ContainsOnlySpacesOrTabs(razorSource, originalRangeStartIndex - startChar + 1, startChar - 1))
        {
            // We're on a new line, and the start of the line is only whitespace, so give that a background color too
            razorRanges.Add(new SemanticRange(textClassification, startLine, 0, startLine, startChar, _semanticTokensLegendService.TokenModifiers.RazorCodeModifier, fromRazor: false, isCSharpWhitespace: true));
        }
    }

    private static bool ContainsOnlySpacesOrTabs(SourceText razorSource, int startIndex, int count)
    {
        var end = startIndex + count;
        for (var i = startIndex; i < end; i++)
        {
            if (razorSource[i] is not (' ' or '\t'))
            {
                return false;
            }
        }

        return true;
    }

    // Internal for testing only
    internal static bool TryGetSortedCSharpRanges(RazorCodeDocument codeDocument, LinePositionSpan razorRange, out ImmutableArray<LinePositionSpan> ranges)
    {
        using var _ = ArrayBuilderPool<LinePositionSpan>.GetPooledObject(out var csharpRanges);
        var csharpSourceText = codeDocument.GetCSharpSourceText();
        var sourceText = codeDocument.Source.Text;
        var textSpan = sourceText.GetTextSpan(razorRange);
        var csharpDoc = codeDocument.GetRequiredCSharpDocument();

        // We want to find the min and max C# source mapping that corresponds with our Razor range.
        foreach (var mapping in csharpDoc.SourceMappingsSortedByOriginal)
        {
            var mappedTextSpan = mapping.OriginalSpan.AsTextSpan();

            if (textSpan.OverlapsWith(mappedTextSpan))
            {
                var mappedRange = csharpSourceText.GetLinePositionSpan(mapping.GeneratedSpan);
                csharpRanges.Add(mappedRange);
            }
            else if (mappedTextSpan.Start > textSpan.End)
            {
                // This span (and all following) are after textSpan
                break;
            }
        }

        if (csharpRanges.Count == 0)
        {
            ranges = [];
            return false;
        }

        csharpRanges.Sort(CompareLinePositionSpans);
        ranges = csharpRanges.ToImmutableAndClear();
        return true;
    }

    private static int CompareLinePositionSpans(LinePositionSpan span1, LinePositionSpan span2)
    {
        var result = span1.Start.CompareTo(span2.Start);

        if (result == 0)
        {
            result = span1.End.CompareTo(span2.End);
        }

        return result;
    }

    private static SemanticRange CSharpDataToSemanticRange(
        int lineDelta,
        int charDelta,
        int length,
        int tokenType,
        int tokenModifiers,
        SemanticRange previousSemanticRange)
    {
        var startLine = previousSemanticRange.EndLine + lineDelta;
        var previousEndChar = lineDelta == 0 ? previousSemanticRange.StartCharacter : 0;
        var startCharacter = previousEndChar + charDelta;

        var endLine = startLine;
        var endCharacter = startCharacter + length;

        var semanticRange = new SemanticRange(tokenType, startLine, startCharacter, endLine, endCharacter, tokenModifiers, fromRazor: false);

        return semanticRange;
    }

    private static int[] ConvertSemanticRangesToSemanticTokensData(
        List<SemanticRange> semanticRanges,
        RazorCodeDocument razorCodeDocument)
    {
        var sourceText = razorCodeDocument.Source.Text;

        // We don't bother filtering out duplicate ranges (eg, where C# and Razor both have opinions), but instead take advantage of
        // our sort algorithm to be correct, so we can skip duplicates here. That means our final array may end up smaller than the
        // expected size.
        var tokens = new int[semanticRanges.Count * TokenSize];

        var isFirstRange = true;
        var index = 0;
        SemanticRange previousRange = default;
        var i = 0;
        foreach (var range in semanticRanges)
        {
            var nextRange = semanticRanges.Count > i + 1
                ? semanticRanges[i + 1]
                : default;

            if (TryWriteToken(range, previousRange, nextRange, isFirstRange, sourceText, tokens.AsSpan(index, TokenSize)))
            {
                index += TokenSize;
                previousRange = range;
            }

            isFirstRange = false;
            i++;
        }

        // The common case is that the ConvertIntoDataArray calls didn't find any overlap, and we can just directly use the
        // data array we allocated. If there was overlap, then we need to allocate a smaller array and copy the data over.
        if (index < tokens.Length)
        {
            Array.Resize(ref tokens, newSize: index);
        }

        return tokens;

        // We purposely capture and manipulate the destination array here to avoid allocation
        static bool TryWriteToken(
            SemanticRange currentRange,
            SemanticRange previousRange,
            SemanticRange nextRange,
            bool isFirstRange,
            SourceText sourceText,
            Span<int> destination)
        {
            Debug.Assert(destination.Length == TokenSize);

            // Due to the fact that Razor ranges can supersede C# ranges, we can end up with C# whitespace ranges we've
            // added, that we not don't want after further processing, so check for that, and skip emitting those ranges.
            if (currentRange.IsCSharpWhitespace)
            {
                // If the previous range is on the same line, and from Razor, then we don't want to emit this.
                // This happens when we have leftover whitespace from between two C# ranges, that were superseded by Razor ranges.
                if (previousRange.FromRazor &&
                    currentRange.StartLine == previousRange.EndLine)
                {
                    return false;
                }

                // If the next range is Razor, then it's leftover whitespace before C#, that was superseded by Razor, so don't emit.
                if (nextRange.FromRazor &&
                    currentRange.StartCharacter == 0)
                {
                    return false;
                }
            }

            /*
             * In short, each token takes 5 integers to represent, so a specific token `i` in the file consists of the following array indices:
             *  - at index `5*i`   - `deltaLine`: token line number, relative to the previous token
             *  - at index `5*i+1` - `deltaStart`: token start character, relative to the previous token (relative to 0 or the previous token's start if they are on the same line)
             *  - at index `5*i+2` - `length`: the length of the token. A token cannot be multiline.
             *  - at index `5*i+3` - `tokenType`: will be looked up in `SemanticTokensLegend.tokenTypes`
             *  - at index `5*i+4` - `tokenModifiers`: each set bit will be looked up in `SemanticTokensLegend.tokenModifiers`
            */

            // deltaLine
            var previousLineIndex = previousRange.StartLine;
            var deltaLine = currentRange.StartLine - previousLineIndex;

            int deltaStart;
            if (!isFirstRange && previousRange.StartLine == currentRange.StartLine)
            {
                // If the current range overlaps the previous one, we assume the sort order was
                // correct, and prefer the previous range. We have a check for the start character first
                // just in case there is a C# range that extends past a Razor range. There is no known case
                // for this, but we need to make sure we don't report bad data.
                if (previousRange.StartCharacter == currentRange.StartCharacter)
                {
                    return false;
                }

                // Razor ranges are allowed to extend past C# ranges though, so we need to check for that too.
                if (previousRange.StartCharacter <= currentRange.StartCharacter &&
                    (previousRange.EndCharacter >= currentRange.EndCharacter || previousRange.EndLine > currentRange.EndLine))
                {
                    return false;
                }

                deltaStart = currentRange.StartCharacter - previousRange.StartCharacter;

                Debug.Assert(deltaStart > 0, "There is no char delta which means this span overlaps the previous. This should have been filtered out!");
            }
            else
            {
                deltaStart = currentRange.StartCharacter;
            }

            destination[0] = deltaLine;
            destination[1] = deltaStart;

            // length

            if (!sourceText.TryGetAbsoluteIndex(currentRange.StartLine, currentRange.StartCharacter, out var startPosition) ||
                !sourceText.TryGetAbsoluteIndex(currentRange.EndLine, currentRange.EndCharacter, out var endPosition))
            {
                throw new ArgumentOutOfRangeException($"Range: All or part of {currentRange} was outside the bounds of the document.");
            }

            var length = endPosition - startPosition;
            Debug.Assert(length > 0);
            destination[2] = length;

            // tokenType
            destination[3] = currentRange.Kind;

            // tokenModifiers
            destination[4] = currentRange.Modifier;

            return true;
        }
    }
}
