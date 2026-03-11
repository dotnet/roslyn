// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PatternMatching;

namespace Microsoft.CodeAnalysis.NavigateTo;

/// <summary>
/// Compiles a regex pattern string into a <see cref="RegexQuery"/> tree that can be evaluated
/// against a document's indexed bigrams/trigrams for fast pre-filtering.
/// <para/>
/// The key insight is that certain regex constructs guarantee literal text in any match. For
/// example, <c>(Read|Write)Line</c> guarantees that "Line" appears in every match. By extracting
/// these guarantees into a boolean query tree, we can reject documents that lack the required
/// bigrams/trigrams without ever running the expensive full regex match.
/// <para/>
/// Constructs that cannot guarantee literal text (wildcards, character classes, zero-count
/// quantifiers) produce <see cref="RegexQuery.None"/>, which means "I can't tell — don't reject
/// on my account." If the entire compiled tree is <see cref="RegexQuery.None"/> (e.g. for <c>.*</c>),
/// <see cref="RegexQuery.HasLiterals"/> is <see langword="false"/> and the caller skips pre-filtering
/// entirely — every document is a candidate and must be checked with the full regex.
/// </summary>
internal static class RegexQueryCompiler
{
    /// <summary>
    /// Parses <paramref name="pattern"/> as a .NET regex and compiles it into an optimized
    /// <see cref="RegexQuery"/> tree. Returns <see langword="null"/> if the pattern is not
    /// a valid regex.
    /// </summary>
    public static RegexQuery? Compile(string pattern)
    {
        // We use Roslyn's own regex parser (not System.Text.RegularExpressions) because it gives
        // us a full AST we can walk to extract structural information. The System.Text parser
        // only gives us a compiled matcher, not the tree.
        var sequence = VirtualCharSequence.Create(0, pattern);
        var tree = RegexParser.TryParse(sequence, RegexOptions.None);
        if (tree is null || tree.Diagnostics.Length > 0)
            return null;

        var raw = CompileNode(tree.Root.Expression);
        return RegexQuery.Optimize(raw);
    }

    private static RegexQuery CompileNode(RegexNode node)
    {
        return node switch
        {
            RegexAlternationNode alternation => CompileAlternation(alternation),
            RegexSequenceNode seq => CompileSequence(seq),
            RegexTextNode text => CompileText(text),

            // A bare wildcard (`.`) matches any character — no literal requirement can be extracted.
            RegexWildcardNode => RegexQuery.None.Instance,

            // Grouping nodes are transparent for pre-filtering purposes — only the inner expression
            // matters. Capture semantics don't affect which literals must appear in a match.
            RegexSimpleGroupingNode group => CompileNode(group.Expression),
            RegexNonCapturingGroupingNode group => CompileNode(group.Expression),
            RegexCaptureGroupingNode group => CompileNode(group.Expression),
            RegexNestedOptionsGroupingNode group => CompileNode(group.Expression),
            RegexAtomicGroupingNode group => CompileNode(group.Expression),

            // `+` requires at least one occurrence, so the inner expression's literals must appear.
            // `*` and `?` allow zero occurrences, so we can't require anything from them.
            RegexOneOrMoreQuantifierNode quantifier => CompileNode(quantifier.Expression),
            RegexZeroOrMoreQuantifierNode => RegexQuery.None.Instance,
            RegexZeroOrOneQuantifierNode => RegexQuery.None.Instance,

            // Lazy quantifiers (e.g. `X+?`) don't change the minimum occurrence count,
            // so we delegate to the underlying quantifier node for the same reasoning.
            RegexLazyQuantifierNode lazy => CompileNode(lazy.Quantifier),

            // Numeric quantifiers: `{n}`, `{n,}`, `{n,m}`. If the minimum count (first number)
            // is >= 1, the inner expression must appear at least once.
            RegexExactNumericQuantifierNode exact => CompileNumericQuantifier(exact.Expression, exact.FirstNumberToken),
            RegexOpenNumericRangeQuantifierNode open => CompileNumericQuantifier(open.Expression, open.FirstNumberToken),
            RegexClosedNumericRangeQuantifierNode closed => CompileNumericQuantifier(closed.Expression, closed.FirstNumberToken),

            // Simple escape (e.g. `\.`, `\[`, `\n`): the escaped character is a required literal.
            RegexSimpleEscapeNode escape => CompileSimpleEscape(escape),

            // Everything else (anchors `^`/`$`, character classes `[a-z]`, `\d`, `\w`, backreferences,
            // lookahead/lookbehind, etc.) cannot contribute literal text we can require. Return None
            // so the pre-filter doesn't over-reject.
            _ => RegexQuery.None.Instance,
        };
    }

    private static RegexQuery CompileAlternation(RegexAlternationNode alternation)
    {
        if (alternation.SequenceList.Length == 1)
            return CompileNode(alternation.SequenceList[0]);

        var children = new FixedSizeArrayBuilder<RegexQuery>(alternation.SequenceList.Length);
        for (var i = 0; i < alternation.SequenceList.Length; i++)
            children.Add(CompileNode(alternation.SequenceList[i]));

        return new RegexQuery.Any(children.MoveToImmutable());
    }

    private static RegexQuery CompileSequence(RegexSequenceNode sequence)
    {
        if (sequence.Children.Length == 1)
            return CompileNode(sequence.Children[0]);

        var children = new FixedSizeArrayBuilder<RegexQuery>(sequence.Children.Length);
        foreach (var child in sequence.Children)
            children.Add(CompileNode(child));

        return new RegexQuery.All(children.MoveToImmutable());
    }

    private static RegexQuery CompileText(RegexTextNode text)
    {
        var chars = text.TextToken.VirtualChars;
        if (chars.Length == 0)
            return RegexQuery.None.Instance;

        // Strip whitespace from text nodes because symbol names never contain spaces.
        // This allows users to write readable patterns like `( Read | Write ) Line` —
        // the spaces around alternation branches are purely for readability.
        var literal = chars.CreateString().Replace(" ", "");
        if (literal.Length == 0)
            return RegexQuery.None.Instance;

        return new RegexQuery.Literal(literal);
    }

    private static RegexQuery CompileSimpleEscape(RegexSimpleEscapeNode escape)
    {
        // The escaped character itself is a concrete character that must appear in any match.
        // For example, `\.` requires a literal dot, `\[` requires a literal bracket.
        var chars = escape.TypeToken.VirtualChars;
        if (chars.Length == 0)
            return RegexQuery.None.Instance;

        return new RegexQuery.Literal(chars.CreateString());
    }

    private static RegexQuery CompileNumericQuantifier(RegexExpressionNode expression, EmbeddedSyntaxToken<RegexKind> firstNumberToken)
    {
        // Only require the inner expression's literals when the minimum repetition count is >= 1.
        // `{0}` or `{0,5}` allow zero matches, meaning the inner text need not appear at all.
        var numberText = firstNumberToken.VirtualChars.CreateString();
        if (int.TryParse(numberText, out var minCount) && minCount >= 1)
            return CompileNode(expression);

        return RegexQuery.None.Instance;
    }
}
