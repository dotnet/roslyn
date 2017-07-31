﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractDeclaredSymbolInfoFactoryService : IDeclaredSymbolInfoFactoryService
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

        protected static List<Dictionary<string, string>> AllocateAliasMapList()
            => s_aliasMapListPool.Allocate();

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
            => s_aliasMapPool.Allocate();

        protected static void AppendTokens(SyntaxNode node, StringBuilder builder)
        {
            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsToken)
                {
                    builder.Append(child.AsToken().Text);
                }
                else
                {
                    AppendTokens(child.AsNode(), builder);
                }
            }
        }

        protected static void Intern(StringTable stringTable, ArrayBuilder<string> builder)
        {
            for (int i = 0, n = builder.Count; i < n; i++)
            {
                builder[i] = stringTable.Add(builder[i]);
            }
        }

        public abstract bool TryGetDeclaredSymbolInfo(StringTable stringTable, SyntaxNode node, out DeclaredSymbolInfo declaredSymbolInfo);
    }

    internal abstract class AbstractSyntaxFactsService
    {
        private readonly static ObjectPool<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>> s_stackPool =
            new ObjectPool<Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>>(() => new Stack<(SyntaxNodeOrToken nodeOrToken, bool leading, bool trailing)>());

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
                var current = stack.Pop();
                var currentNodeOrToken = current.nodeOrToken;
                var currentLeading = current.leading;
                var currentTrailing = current.trailing;

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
                        var first = index == 0;
                        var last = index == childCount - 1;

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
            if (IsStringLiteral(token) ||
                IsInterpolatedStringTextToken(token))
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

        private bool IsOnSingleLine(string value)
            => value.GetNumberOfLineBreaks() == 0;

        public abstract bool IsStringLiteral(SyntaxToken token);
        public abstract bool IsInterpolatedStringTextToken(SyntaxToken token);

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
            return GetNodeWithoutLeadingBlankLines(node, out var blankLines);
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

        private bool ContainsInterleavedDirective(
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

        public string GetBannerText(SyntaxNode documentationCommentTriviaSyntax, CancellationToken cancellationToken)
            => DocumentationCommentService.GetBannerText(documentationCommentTriviaSyntax, cancellationToken);

        protected abstract IDocumentationCommentService DocumentationCommentService { get; }
    }
}
