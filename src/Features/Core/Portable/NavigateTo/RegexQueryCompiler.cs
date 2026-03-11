// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.NavigateTo;

/// <summary>
/// Compiles a regex pattern string into a <see cref="RegexQuery"/> tree that can be evaluated
/// against a document's indexed bigrams/trigrams for fast pre-filtering. The compilation walks
/// the AST produced by Roslyn's <see cref="RegexParser"/> and extracts literal text that must
/// appear in any matching symbol name.
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
            RegexWildcardNode => RegexQuery.None.Instance,

            // Grouping: recurse into the contained expression.
            RegexSimpleGroupingNode group => CompileNode(group.Expression),
            RegexNonCapturingGroupingNode group => CompileNode(group.Expression),
            RegexCaptureGroupingNode group => CompileNode(group.Expression),
            RegexNestedOptionsGroupingNode group => CompileNode(group.Expression),
            RegexAtomicGroupingNode group => CompileNode(group.Expression),

            // Quantifiers: only those requiring at least one match preserve the inner expression.
            RegexOneOrMoreQuantifierNode quantifier => CompileNode(quantifier.Expression),
            RegexZeroOrMoreQuantifierNode => RegexQuery.None.Instance,
            RegexZeroOrOneQuantifierNode => RegexQuery.None.Instance,
            RegexLazyQuantifierNode lazy => CompileNode(lazy.Quantifier),
            RegexExactNumericQuantifierNode exact => CompileNumericQuantifier(exact.Expression, exact.FirstNumberToken),
            RegexOpenNumericRangeQuantifierNode open => CompileNumericQuantifier(open.Expression, open.FirstNumberToken),
            RegexClosedNumericRangeQuantifierNode closed => CompileNumericQuantifier(closed.Expression, closed.FirstNumberToken),

            // Simple escape (e.g. \., \[, \n): treat the escaped character as a literal.
            RegexSimpleEscapeNode escape => CompileSimpleEscape(escape),

            // Anchors, character classes, and all other nodes cannot contribute literal text.
            _ => RegexQuery.None.Instance,
        };
    }

    private static RegexQuery CompileAlternation(RegexAlternationNode alternation)
    {
        if (alternation.SequenceList.Length == 1)
            return CompileNode(alternation.SequenceList[0]);

        using var _ = ArrayBuilder<RegexQuery>.GetInstance(out var children);
        for (var i = 0; i < alternation.SequenceList.Length; i++)
            children.Add(CompileNode(alternation.SequenceList[i]));

        return new RegexQuery.Any([.. children]);
    }

    private static RegexQuery CompileSequence(RegexSequenceNode sequence)
    {
        if (sequence.Children.Length == 1)
            return CompileNode(sequence.Children[0]);

        using var _ = ArrayBuilder<RegexQuery>.GetInstance(out var children);
        foreach (var child in sequence.Children)
            children.Add(CompileNode(child));

        return new RegexQuery.All([.. children]);
    }

    private static RegexQuery CompileText(RegexTextNode text)
    {
        var chars = text.TextToken.VirtualChars;
        if (chars.Length == 0)
            return RegexQuery.None.Instance;

        return new RegexQuery.Literal(chars.CreateString());
    }

    private static RegexQuery CompileSimpleEscape(RegexSimpleEscapeNode escape)
    {
        var chars = escape.TypeToken.VirtualChars;
        if (chars.Length == 0)
            return RegexQuery.None.Instance;

        return new RegexQuery.Literal(chars.CreateString());
    }

    private static RegexQuery CompileNumericQuantifier(RegexExpressionNode expression, EmbeddedSyntaxToken<RegexKind> firstNumberToken)
    {
        var numberText = firstNumberToken.VirtualChars.CreateString();
        if (int.TryParse(numberText, out var minCount) && minCount >= 1)
            return CompileNode(expression);

        return RegexQuery.None.Instance;
    }
}
