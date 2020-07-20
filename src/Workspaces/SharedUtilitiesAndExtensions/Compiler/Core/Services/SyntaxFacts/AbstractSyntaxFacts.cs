// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Editing;
#else
using Microsoft.CodeAnalysis.Editing;
#endif

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSyntaxFacts
    {
        private static readonly ObjectPool<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>> s_stackPool
            = SharedPools.Default<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>>();

        public abstract ISyntaxKinds SyntaxKinds { get; }

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

        protected AbstractSyntaxFacts()
        {
            var whitespace = Matcher.Repeat(
                Matcher.Single<SyntaxTrivia>(IsWhitespaceTrivia, "\\b"));
            var endOfLine = Matcher.Single<SyntaxTrivia>(IsEndOfLineTrivia, "\\n");
            var singleBlankLine = Matcher.Sequence(whitespace, endOfLine);

            var shebangComment = Matcher.Single<SyntaxTrivia>(IsShebangDirectiveTrivia, "#!");
            var singleLineComment = Matcher.Single<SyntaxTrivia>(IsSingleLineCommentTrivia, "//");
            var multiLineComment = Matcher.Single<SyntaxTrivia>(IsMultiLineCommentTrivia, "/**/");
            var singleLineDocumentationComment = Matcher.Single<SyntaxTrivia>(IsSingleLineDocCommentTrivia, "///");
            var multiLineDocumentationComment = Matcher.Single<SyntaxTrivia>(IsMultiLineDocCommentTrivia, "/** */");
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

        private bool IsWhitespaceTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.WhitespaceTrivia == trivia.RawKind;

        private bool IsEndOfLineTrivia(SyntaxTrivia trivia)
            => SyntaxKinds.EndOfLineTrivia == trivia.RawKind;

        public abstract bool IsSingleLineCommentTrivia(SyntaxTrivia trivia);
        public abstract bool IsMultiLineCommentTrivia(SyntaxTrivia trivia);
        public abstract bool IsSingleLineDocCommentTrivia(SyntaxTrivia trivia);
        public abstract bool IsMultiLineDocCommentTrivia(SyntaxTrivia trivia);
        public abstract bool IsShebangDirectiveTrivia(SyntaxTrivia trivia);
        public abstract bool IsPreprocessorDirective(SyntaxTrivia trivia);

        public bool IsOnSingleLine(SyntaxNode node, bool fullSpan)
        {
            // Use an actual Stack so we can write out deeply recursive structures without overflowing.
            // Note: algorithm is taken from GreenNode.WriteTo.
            //
            // General approach is that we recurse down the nodes, using a real stack object to
            // keep track of what node we're on.  If full-span is true we'll examine all tokens
            // and all the trivia on each token.  If full-span is false we'll examine all tokens
            // but we'll ignore the leading trivia on the very first trivia and the trailing trivia
            // on the very last token.
            var stack = s_stackPool.Allocate();
            stack.Push((node, leading: fullSpan, trailing: fullSpan));

            var result = IsOnSingleLine(stack);

            s_stackPool.ClearAndFree(stack);

            return result;
        }

        private bool IsOnSingleLine(
            Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)> stack)
        {
            while (stack.Count > 0)
            {
                var (currentNodeOrToken, currentLeading, currentTrailing) = stack.Pop();
                if (currentNodeOrToken.IsToken)
                {
                    // If this token isn't on a single line, then the original node definitely
                    // isn't on a single line.
                    if (!IsOnSingleLine(currentNodeOrToken.AsToken(), currentLeading, currentTrailing))
                    {
                        return false;
                    }
                }
                else
                {
                    var currentNode = currentNodeOrToken.AsNode();

                    var childNodesAndTokens = currentNode.ChildNodesAndTokens();
                    var childCount = childNodesAndTokens.Count;

                    // Walk the children of this node in reverse, putting on the stack to process.
                    // This way we process the children in the actual child-order they are in for
                    // this node.
                    var index = 0;
                    foreach (var child in childNodesAndTokens.Reverse())
                    {
                        // Since we're walking the children in reverse, if we're on hte 0th item,
                        // that's the last child.
                        var last = index == 0;

                        // Once we get all the way to the end of the reversed list, we're actually
                        // on the first.
                        var first = index == childCount - 1;

                        // We want the leading trivia if we've asked for it, or if we're not the first
                        // token being processed.  We want the trailing trivia if we've asked for it,
                        // or if we're not the last token being processed.
                        stack.Push((child, currentLeading | !first, currentTrailing | !last));
                        index++;
                    }
                }
            }

            // All tokens were on a single line.  This node is on a single line.
            return true;
        }

        private bool IsOnSingleLine(SyntaxToken token, bool leading, bool trailing)
        {
            // If any of our trivia is not on a single line, then we're not on a single line.
            if (!IsOnSingleLine(token.LeadingTrivia, leading) ||
                !IsOnSingleLine(token.TrailingTrivia, trailing))
            {
                return false;
            }

            // Only string literals can span multiple lines.  Only need to check those.
            if (this.SyntaxKinds.StringLiteralToken == token.RawKind ||
                this.SyntaxKinds.InterpolatedStringTextToken == token.RawKind)
            {
                // This allocated.  But we only do it in the string case. For all other tokens
                // we don't need any allocations.
                if (!IsOnSingleLine(token.ToString()))
                {
                    return false;
                }
            }

            // Any other type of token is on a single line.
            return true;
        }

        private bool IsOnSingleLine(SyntaxTriviaList triviaList, bool checkTrivia)
        {
            if (checkTrivia)
            {
                foreach (var trivia in triviaList)
                {
                    if (trivia.HasStructure)
                    {
                        // For structured trivia, we recurse into the trivia to see if it
                        // is on a single line or not.  If it isn't, then we're definitely
                        // not on a single line.
                        if (!IsOnSingleLine(trivia.GetStructure(), fullSpan: true))
                        {
                            return false;
                        }
                    }
                    else if (IsEndOfLineTrivia(trivia))
                    {
                        // Contained an end-of-line trivia.  Definitely not on a single line.
                        return false;
                    }
                    else if (!IsWhitespaceTrivia(trivia))
                    {
                        // Was some other form of trivia (like a comment).  Easiest thing
                        // to do is just stringify this and count the number of newlines.
                        // these should be rare.  So the allocation here is ok.
                        if (!IsOnSingleLine(trivia.ToString()))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool IsOnSingleLine(string value)
            => value.GetNumberOfLineBreaks() == 0;

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

        public bool ContainsInterleavedDirective(
            ImmutableArray<SyntaxNode> nodes, CancellationToken cancellationToken)
        {
            if (nodes.Length > 0)
            {
                var span = TextSpan.FromBounds(nodes.First().Span.Start, nodes.Last().Span.End);

                foreach (var node in nodes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (ContainsInterleavedDirective(span, node, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool ContainsInterleavedDirective(SyntaxNode node, CancellationToken cancellationToken)
            => ContainsInterleavedDirective(node.Span, node, cancellationToken);

        public bool ContainsInterleavedDirective(
            TextSpan span, SyntaxNode node, CancellationToken cancellationToken)
        {
            foreach (var token in node.DescendantTokens())
            {
                if (ContainsInterleavedDirective(span, token, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        protected abstract bool ContainsInterleavedDirective(TextSpan span, SyntaxToken token, CancellationToken cancellationToken);

        public string GetBannerText(SyntaxNode documentationCommentTriviaSyntax, int bannerLength, CancellationToken cancellationToken)
            => DocumentationCommentService.GetBannerText(documentationCommentTriviaSyntax, bannerLength, cancellationToken);

        protected abstract IDocumentationCommentService DocumentationCommentService { get; }

        public bool SpansPreprocessorDirective(IEnumerable<SyntaxNode> nodes)
        {
            if (nodes == null || nodes.IsEmpty())
            {
                return false;
            }

            return SpansPreprocessorDirective(nodes.SelectMany(n => n.DescendantTokens()));
        }

        /// <summary>
        /// Determines if there is preprocessor trivia *between* any of the <paramref name="tokens"/>
        /// provided.  The <paramref name="tokens"/> will be deduped and then ordered by position.
        /// Specifically, the first token will not have it's leading trivia checked, and the last
        /// token will not have it's trailing trivia checked.  All other trivia will be checked to
        /// see if it contains a preprocessor directive.
        /// </summary>
        public bool SpansPreprocessorDirective(IEnumerable<SyntaxToken> tokens)
        {
            // we want to check all leading trivia of all tokens (except the 
            // first one), and all trailing trivia of all tokens (except the
            // last one).

            var first = true;
            var previousToken = default(SyntaxToken);

            // Allow duplicate nodes/tokens to be passed in.  Also, allow the nodes/tokens
            // to not be in any particular order when passed in.
            var orderedTokens = tokens.Distinct().OrderBy(t => t.SpanStart);

            foreach (var token in orderedTokens)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    // check the leading trivia of this token, and the trailing trivia
                    // of the previous token.
                    if (SpansPreprocessorDirective(token.LeadingTrivia) ||
                        SpansPreprocessorDirective(previousToken.TrailingTrivia))
                    {
                        return true;
                    }
                }

                previousToken = token;
            }

            return false;
        }

        private bool SpansPreprocessorDirective(SyntaxTriviaList list)
            => list.Any(t => IsPreprocessorDirective(t));

        public bool IsOnHeader(SyntaxNode root, int position, SyntaxNode ownerOfHeader, SyntaxNodeOrToken lastTokenOrNodeOfHeader)
            => IsOnHeader(root, position, ownerOfHeader, lastTokenOrNodeOfHeader, ImmutableArray<SyntaxNode>.Empty);

        public bool IsOnHeader<THoleSyntax>(
            SyntaxNode root,
            int position,
            SyntaxNode ownerOfHeader,
            SyntaxNodeOrToken lastTokenOrNodeOfHeader,
            ImmutableArray<THoleSyntax> holes)
            where THoleSyntax : SyntaxNode
        {
            Debug.Assert(ownerOfHeader.FullSpan.Contains(lastTokenOrNodeOfHeader.Span));

            var headerSpan = TextSpan.FromBounds(
                start: GetStartOfNodeExcludingAttributes(root, ownerOfHeader),
                end: lastTokenOrNodeOfHeader.FullSpan.End);

            // Is in header check is inclusive, being on the end edge of an header still counts
            if (!headerSpan.IntersectsWith(position))
            {
                return false;
            }

            // Holes are exclusive: 
            // To be consistent with other 'being on the edge' of Tokens/Nodes a position is 
            // in a hole (not in a header) only if it's inside _inside_ a hole, not only on the edge.
            if (holes.Any(h => h.Span.Contains(position) && position > h.Span.Start))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get an ancestor of a Token on current position or of Token directly to left:
        /// e.g.: tokenWithWantedAncestor[||]tokenWithoutWantedAncestor
        /// </summary>
        protected TNode TryGetAncestorForLocation<TNode>(SyntaxNode root, int position) where TNode : SyntaxNode
        {
            var tokenToRightOrIn = root.FindToken(position);
            var nodeToRightOrIn = tokenToRightOrIn.GetAncestor<TNode>();
            if (nodeToRightOrIn != null)
            {
                return nodeToRightOrIn;
            }

            // not at the beginning of a Token -> no (different) token to the left
            if (tokenToRightOrIn.FullSpan.Start != position && tokenToRightOrIn.RawKind != SyntaxKinds.EndOfFileToken)
            {
                return null;
            }

            return tokenToRightOrIn.GetPreviousToken().GetAncestor<TNode>();
        }

        protected int GetStartOfNodeExcludingAttributes(SyntaxNode root, SyntaxNode node)
        {
            var attributeList = GetAttributeLists(node);
            if (attributeList.Any())
            {
                var endOfAttributeLists = attributeList.Last().Span.End;
                var afterAttributesToken = root.FindTokenOnRightOfPosition(endOfAttributeLists);

                return Math.Min(afterAttributesToken.Span.Start, node.Span.End);
            }

            return node.SpanStart;
        }

        public abstract SyntaxList<SyntaxNode> GetAttributeLists(SyntaxNode node);

        public abstract bool IsParameterNameXmlElementSyntax(SyntaxNode node);

        public abstract SyntaxList<SyntaxNode> GetContentFromDocumentationCommentTriviaSyntax(SyntaxTrivia trivia);

        public bool HasIncompleteParentMember(SyntaxNode node)
            => node?.Parent?.RawKind == SyntaxKinds.IncompleteMember;

        public abstract bool CanHaveAccessibility(SyntaxNode declaration);

        public abstract Accessibility GetAccessibility(SyntaxNode declaration);

        public abstract void GetAccessibilityAndModifiers(SyntaxTokenList modifierList, out Accessibility accessibility, out DeclarationModifiers modifiers, out bool isDefault);

        public abstract SyntaxTokenList GetModifierTokens(SyntaxNode declaration);

        public abstract DeclarationKind GetDeclarationKind(SyntaxNode declaration);
    }
}
