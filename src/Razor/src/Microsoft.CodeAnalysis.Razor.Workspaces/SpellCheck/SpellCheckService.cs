// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.SpellCheck;

internal class SpellCheckService(
    ICSharpSpellCheckRangeProvider csharpSpellCheckService,
    IDocumentMappingService documentMappingService) : ISpellCheckService
{
    private readonly ICSharpSpellCheckRangeProvider _csharpSpellCheckService = csharpSpellCheckService;
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;

    public async Task<int[]> GetSpellCheckRangeTriplesAsync(DocumentContext documentContext, CancellationToken cancellationToken)
    {
        using var builder = new PooledArrayBuilder<SpellCheckRange>();

        var syntaxTree = await documentContext.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

        AddRazorSpellCheckRanges(ref builder.AsRef(), syntaxTree);

        var csharpRanges = await _csharpSpellCheckService.GetCSharpSpellCheckRangesAsync(documentContext, cancellationToken).ConfigureAwait(false);

        if (csharpRanges.Length > 0)
        {
            var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);
            AddCSharpSpellCheckRanges(ref builder.AsRef(), csharpRanges, codeDocument);
        }

        // Important to sort first as we're calculating relative indexes
        var ranges = builder.ToImmutableOrderedBy(static r => r.AbsoluteStartIndex);

        return ConvertSpellCheckRangesToIntTriples(ranges);
    }

    private static void AddRazorSpellCheckRanges(ref PooledArrayBuilder<SpellCheckRange> ranges, RazorSyntaxTree syntaxTree)
    {
        // We don't want to report spelling errors in script or style tags, so we avoid descending into them at all, which
        // means we don't need complicated logic, and it performs a bit better. We assume any C# in them will still be reported
        // by Roslyn.
        // In an ideal world we wouldn't need this logic at all, as we would defer to the Html LSP server to provide spell checking
        // but it doesn't currently support it. When that support is added, we can remove all of this but the RazorCommentBlockSyntax
        // handling.
        foreach (var node in syntaxTree.Root.DescendantNodes(static n => n is not BaseMarkupElementSyntax element || !RazorSyntaxFacts.IsScriptOrStyleBlock(element)))
        {
            if (node is RazorCommentBlockSyntax commentBlockSyntax)
            {
                ranges.Add(new((int)VSInternalSpellCheckableRangeKind.Comment, commentBlockSyntax.Comment.SpanStart, commentBlockSyntax.Comment.Span.Length));
            }
            else if (node is MarkupTextLiteralSyntax textLiteralSyntax)
            {
                // Attribute names are text literals, but we don't want to spell check them because either C# will,
                // whether they're component attributes based on property names, or they come from tag helper attribute
                // parameters as strings, or they're Html attributes which are not necessarily expected to be real words.
                if (node.Parent.IsAnyAttributeSyntax())
                {
                    continue;
                }

                // Text literals appear everywhere in Razor to hold newlines and indentation, so its worth saving the tokens
                if (textLiteralSyntax.ContainsOnlyWhitespace())
                {
                    continue;
                }

                if (textLiteralSyntax.Span.Length == 0)
                {
                    continue;
                }

                ranges.Add(new((int)VSInternalSpellCheckableRangeKind.String, textLiteralSyntax.SpanStart, textLiteralSyntax.Span.Length));
            }
        }
    }

    private void AddCSharpSpellCheckRanges(ref PooledArrayBuilder<SpellCheckRange> ranges, ImmutableArray<SpellCheckRange> csharpRanges, RazorCodeDocument codeDocument)
    {
        var csharpDocument = codeDocument.GetRequiredCSharpDocument();

        foreach (var range in csharpRanges)
        {
            var absoluteCSharpStartIndex = range.AbsoluteStartIndex;
            var length = range.Length;

            // We need to map the start index to produce results, and we validate that we can map the end index so we don't have
            // squiggles that go from C# into Razor/Html.
            if (_documentMappingService.TryMapToRazorDocumentPosition(csharpDocument, absoluteCSharpStartIndex, out _, out var hostDocumentIndex) &&
                _documentMappingService.TryMapToRazorDocumentPosition(csharpDocument, absoluteCSharpStartIndex + length, out _, out _))
            {
                ranges.Add(range with { AbsoluteStartIndex = hostDocumentIndex });
            }
        }
    }

    private static int[] ConvertSpellCheckRangesToIntTriples(ImmutableArray<SpellCheckRange> ranges)
    {
        using var data = new PooledArrayBuilder<int>(ranges.Length * 3);

        var lastAbsoluteEndIndex = 0;
        foreach (var range in ranges)
        {
            if (range.Length == 0)
            {
                continue;
            }

            data.Add(range.Kind);
            data.Add(range.AbsoluteStartIndex - lastAbsoluteEndIndex);
            data.Add(range.Length);

            lastAbsoluteEndIndex = range.AbsoluteStartIndex + range.Length;
        }

        return data.ToArray();
    }
}
