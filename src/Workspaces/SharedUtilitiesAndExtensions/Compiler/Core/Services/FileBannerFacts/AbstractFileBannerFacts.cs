// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.LanguageService;

internal abstract class AbstractFileBannerFacts : IFileBannerFacts
{
    // Matches the following:
    //
    // (whitespace* newline)+ 
    private readonly Matcher<SyntaxTrivia> _oneOrMoreBlankLines;

    // Matches the following:
    // 
    // (whitespace* (single-comment|multi-comment) whitespace* newline)+ OneOrMoreBlankLines
    private readonly Matcher<SyntaxTrivia> _bannerMatcher;

    // Used to match the following:
    //
    // <start-of-file> (whitespace* (single-comment|multi-comment) whitespace* newline)+ blankLine*
    private readonly Matcher<SyntaxTrivia> _fileBannerMatcher;

    protected AbstractFileBannerFacts()
    {
        var whitespace = Matcher.Repeat(
            Matcher.Single<SyntaxTrivia>(this.SyntaxFacts.IsWhitespaceTrivia, "\\b"));
        var endOfLine = Matcher.Single<SyntaxTrivia>(this.SyntaxFacts.IsEndOfLineTrivia, "\\n");
        var singleBlankLine = Matcher.Sequence(whitespace, endOfLine);

        var shebangComment = Matcher.Single<SyntaxTrivia>(this.SyntaxFacts.IsShebangDirectiveTrivia, "#!");
        var singleLineComment = Matcher.Single<SyntaxTrivia>(this.SyntaxFacts.IsSingleLineCommentTrivia, "//");
        var multiLineComment = Matcher.Single<SyntaxTrivia>(this.SyntaxFacts.IsMultiLineCommentTrivia, "/**/");
        var singleLineDocumentationComment = Matcher.Single<SyntaxTrivia>(this.SyntaxFacts.IsSingleLineDocCommentTrivia, "///");
        var multiLineDocumentationComment = Matcher.Single<SyntaxTrivia>(this.SyntaxFacts.IsMultiLineDocCommentTrivia, "/** */");
        var anyCommentMatcher = Matcher.Choice(shebangComment, singleLineComment, multiLineComment, singleLineDocumentationComment, multiLineDocumentationComment);

        var commentLine = Matcher.Sequence(whitespace, anyCommentMatcher, whitespace, endOfLine);

        _oneOrMoreBlankLines = Matcher.OneOrMore(singleBlankLine);
        _bannerMatcher =
            Matcher.Sequence(
                Matcher.OneOrMore(commentLine),
                _oneOrMoreBlankLines);
        _fileBannerMatcher =
            Matcher.Sequence(
                Matcher.OneOrMore(commentLine),
                Matcher.Repeat(singleBlankLine));
    }

    protected abstract ISyntaxFacts SyntaxFacts { get; }
    protected abstract IDocumentationCommentService DocumentationCommentService { get; }

    public string GetBannerText(SyntaxNode? documentationCommentTriviaSyntax, int bannerLength, CancellationToken cancellationToken)
        => DocumentationCommentService.GetBannerText(documentationCommentTriviaSyntax, bannerLength, cancellationToken);

    public ImmutableArray<SyntaxTrivia> GetLeadingBlankLines(SyntaxNode node)
        => GetLeadingBlankLines<SyntaxNode>(node);

    public ImmutableArray<SyntaxTrivia> GetLeadingBlankLines<TSyntaxNode>(TSyntaxNode node)
        where TSyntaxNode : SyntaxNode
    {
        GetNodeWithoutLeadingBlankLines(node, out var blankLines);
        return blankLines;
    }

    public TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(TSyntaxNode node)
        where TSyntaxNode : SyntaxNode
    {
        return GetNodeWithoutLeadingBlankLines(node, out _);
    }

    public TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(
        TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia)
        where TSyntaxNode : SyntaxNode
    {
        var leadingTriviaToKeep = new List<SyntaxTrivia>(node.GetLeadingTrivia());

        var index = 0;
        _oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, ref index);

        strippedTrivia = leadingTriviaToKeep.Take(index).ToImmutableArray();

        return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
    }

    public ImmutableArray<SyntaxTrivia> GetLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(TSyntaxNode node)
        where TSyntaxNode : SyntaxNode
    {
        GetNodeWithoutLeadingBannerAndPreprocessorDirectives(node, out var leadingTrivia);
        return leadingTrivia;
    }

    public TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
        TSyntaxNode node)
        where TSyntaxNode : SyntaxNode
    {
        return GetNodeWithoutLeadingBannerAndPreprocessorDirectives(node, out _);
    }

    public TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
        TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia)
        where TSyntaxNode : SyntaxNode
    {
        var leadingTrivia = node.GetLeadingTrivia();

        // Rules for stripping trivia: 
        // 1) If there is a pp directive, then it (and all preceding trivia) *must* be stripped.
        //    This rule supersedes all other rules.
        // 2) If there is a doc comment, it cannot be stripped.  Even if there is a doc comment,
        //    followed by 5 new lines, then the doc comment still must stay with the node.  This
        //    rule does *not* supersede rule 1.
        // 3) Single line comments in a group (i.e. with no blank lines between them) belong to
        //    the node *iff* there is no blank line between it and the following trivia.

        List<SyntaxTrivia> leadingTriviaToStrip, leadingTriviaToKeep;

        var ppIndex = -1;
        for (var i = leadingTrivia.Count - 1; i >= 0; i--)
        {
            if (this.SyntaxFacts.IsPreprocessorDirective(leadingTrivia[i]))
            {
                ppIndex = i;
                break;
            }
        }

        if (ppIndex != -1)
        {
            // We have a pp directive.  it (and all previous trivia) must be stripped.
            leadingTriviaToStrip = new List<SyntaxTrivia>(leadingTrivia.Take(ppIndex + 1));
            leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia.Skip(ppIndex + 1));
        }
        else
        {
            leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia);
            leadingTriviaToStrip = [];
        }

        // Now, consume as many banners as we can.  s_fileBannerMatcher will only be matched at
        // the start of the file.
        var index = 0;

        while (
            _oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, ref index) ||
            _bannerMatcher.TryMatch(leadingTriviaToKeep, ref index) ||
            (node.FullSpan.Start == 0 && _fileBannerMatcher.TryMatch(leadingTriviaToKeep, ref index)))
        {
        }

        leadingTriviaToStrip.AddRange(leadingTriviaToKeep.Take(index));

        strippedTrivia = [.. leadingTriviaToStrip];
        return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
    }

    public ImmutableArray<SyntaxTrivia> GetFileBanner(SyntaxNode root)
    {
        Debug.Assert(root.FullSpan.Start == 0);
        return GetFileBanner(root.GetFirstToken(includeZeroWidth: true));
    }

    public ImmutableArray<SyntaxTrivia> GetFileBanner(SyntaxToken firstToken)
    {
        Debug.Assert(firstToken.FullSpan.Start == 0);

        var leadingTrivia = firstToken.LeadingTrivia;
        var index = 0;
        _fileBannerMatcher.TryMatch(leadingTrivia.ToList(), ref index);

        return ImmutableArray.CreateRange(leadingTrivia.Take(index));
    }
}
