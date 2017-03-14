using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSyntaxFactsService
    {
        private readonly static ObjectPool<List<Dictionary<string, string>>> s_aliasMapListPool =
            new ObjectPool<List<Dictionary<string, string>>>(() => new List<Dictionary<string, string>>());

        // Note: these names are stored case insensitively.  That way the alias mapping works 
        // properly for VB.  It will mean that our inheritance maps may store more links in them
        // for C#.  However, that's ok.  It will be rare in practice, and all it means is that
        // we'll end up examining slightly more types (likely 0) when doing operations like 
        // Find all references.
        private readonly static ObjectPool<Dictionary<string, string>> s_aliasMapPool =
            new ObjectPool<Dictionary<string, string>>(() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

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

        protected AbstractSyntaxFactsService()
        {
            var whitespace = Matcher.Repeat(
                Matcher.Single<SyntaxTrivia>(IsWhitespaceTrivia, "\\b"));
            var endOfLine = Matcher.Single<SyntaxTrivia>(IsEndOfLineTrivia, "\\n");
            var singleBlankLine = Matcher.Sequence(whitespace, endOfLine);

            var shebangComment = Matcher.Single<SyntaxTrivia>(IsShebangDirectiveTrivia, "#!");
            var singleLineComment = Matcher.Single<SyntaxTrivia>(IsSingleLineCommentTrivia, "//");
            var multiLineComment = Matcher.Single<SyntaxTrivia>(IsMultiLineCommentTrivia, "/**/");
            var anyCommentMatcher = Matcher.Choice(shebangComment, singleLineComment, multiLineComment);

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

        public abstract bool IsWhitespaceTrivia(SyntaxTrivia trivia);
        public abstract bool IsEndOfLineTrivia(SyntaxTrivia trivia);
        public abstract bool IsSingleLineCommentTrivia(SyntaxTrivia trivia);
        public abstract bool IsMultiLineCommentTrivia(SyntaxTrivia trivia);
        public abstract bool IsShebangDirectiveTrivia(SyntaxTrivia trivia);
        public abstract bool IsPreprocessorDirective(SyntaxTrivia trivia);

        protected static List<Dictionary<string, string>> AllocateAliasMapList()
        {
            return s_aliasMapListPool.Allocate();
        }

        protected static void FreeAliasMapList(List<Dictionary<string, string>> list)
        {
            if (list != null)
            {
                foreach (var aliasMap in list)
                {
                    FreeAliasMap(aliasMap);
                }

                s_aliasMapListPool.ClearAndFree(list);
            }
        }

        protected static void FreeAliasMap(Dictionary<string, string> aliasMap)
        {
            if (aliasMap != null)
            {
                s_aliasMapPool.ClearAndFree(aliasMap);
            }
        }

        protected static Dictionary<string, string> AllocateAliasMap()
        {
            return s_aliasMapPool.Allocate();
        }

        public ImmutableArray<SyntaxTrivia> GetLeadingBlankLines<TSyntaxNode>(TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            GetNodeWithoutLeadingBlankLines(node, out var blankLines);
            return blankLines;
        }

        public TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            return GetNodeWithoutLeadingBlankLines(node, out var blankLines);
        }

        public TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(
            TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia)
            where TSyntaxNode : SyntaxNode
        {
            var leadingTriviaToKeep = new List<SyntaxTrivia>(node.GetLeadingTrivia());

            var index = 0;
            _fileBannerMatcher.TryMatch(leadingTriviaToKeep, ref index);

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
            return GetNodeWithoutLeadingBannerAndPreprocessorDirectives(node, out var strippedTrivia);
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
                if (this.IsPreprocessorDirective(leadingTrivia[i]))
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
                leadingTriviaToStrip = new List<SyntaxTrivia>();
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

            strippedTrivia = leadingTriviaToStrip.ToImmutableArray();
            return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
        }

        public ImmutableArray<SyntaxTrivia> GetFileBanner(SyntaxNode root)
        {
            Debug.Assert(root.FullSpan.Start == 0);

            var leadingTrivia = root.GetLeadingTrivia();
            var index = 0;
            _fileBannerMatcher.TryMatch(leadingTrivia.ToList(), ref index);

            return ImmutableArray.CreateRange(leadingTrivia.Take(index));
        }
    }
}