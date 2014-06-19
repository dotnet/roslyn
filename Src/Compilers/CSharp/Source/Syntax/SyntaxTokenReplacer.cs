using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxTokenReplacer
    {
        internal static TRoot Replace<TRoot>(TRoot root, SyntaxToken oldToken, SyntaxToken newToken)
            where TRoot : SyntaxNode
        {
            if (oldToken == newToken)
            {
                return root;
            }

            return (TRoot)new SingleTokenReplacer(oldToken, newToken).Visit(root);
        }

        internal static TRoot Replace<TRoot>(TRoot root, IEnumerable<SyntaxToken> oldTokens, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken)
            where TRoot : SyntaxNode
        {
            var oldTokensArray = oldTokens.ToArray();
            if (oldTokensArray.Length == 0)
            {
                return root;
            }

            return (TRoot)new MultipleTokenReplacer(oldTokensArray, computeReplacementToken).Visit(root);
        }

        private class SingleTokenReplacer : SyntaxRewriter
        {
            private readonly SyntaxToken oldToken;
            private readonly SyntaxToken newToken;
            private readonly TextSpan oldTokenFullSpan;

            public SingleTokenReplacer(SyntaxToken oldToken, SyntaxToken newToken) :
                base(oldToken.IsPartOfStructuredTrivia())
            {
                this.oldToken = oldToken;
                this.newToken = newToken;
                this.oldTokenFullSpan = oldToken.FullSpan;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (token == this.oldToken)
                {
                    return this.newToken;
                }

                if (this.VisitIntoStructuredTrivia &&
                    token.HasStructuredTrivia && 
                    token.FullSpan.IntersectsWith(this.oldTokenFullSpan))
                {
                    return base.VisitToken(token);
                }

                return token;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node != null)
                {
                    if (node.FullSpan.IntersectsWith(this.oldTokenFullSpan))
                    {
                        return base.Visit(node);
                    }
                }

                return node;
            }
        }

        private class MultipleTokenReplacer : SyntaxRewriter
        {
            private readonly SyntaxToken[] tokens;
            private readonly HashSet<SyntaxToken> tokenSet;
            private readonly TextSpan totalSpan;
            private readonly Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken;

            public MultipleTokenReplacer(SyntaxToken[] tokens, Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken) :
                base(tokens.Any(t => t.IsPartOfStructuredTrivia()))
            {
                this.tokens = tokens;
                this.tokenSet = new HashSet<SyntaxToken>(this.tokens);
                this.totalSpan = ComputeTotalSpan(this.tokens);
                this.computeReplacementToken = computeReplacementToken;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node != null)
                {
                    if (this.ShouldVisit(node.FullSpan))
                    {
                        return base.Visit(node);
                    }
                }

                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var result = token;

                if (this.VisitIntoStructuredTrivia 
                    && token.HasStructuredTrivia
                    && this.ShouldVisit(token.FullSpan))
                {
                    result = base.VisitToken(token);
                }

                if (this.tokenSet.Contains(token))
                {
                    result = this.computeReplacementToken(token, result);
                }

                return result;
            }

            private static TextSpan ComputeTotalSpan(SyntaxToken[] tokens)
            {
                var span0 = tokens[0].FullSpan;
                int start = span0.Start;
                int end = span0.End;

                for (int i = 1; i < tokens.Length; i++)
                {
                    var span = tokens[i].FullSpan;
                    start = Math.Min(start, span.Start);
                    end = Math.Max(end, span.End);
                }

                return new TextSpan(start, end - start);
            }

            private bool ShouldVisit(TextSpan span)
            {
                // first do quick check against total span
                if (!span.IntersectsWith(this.totalSpan))
                {
                    // if the node is outside the total span of the nodes to be replaced
                    // then we won't find any nodes to replace below it.
                    return false;
                }

                foreach (var n in this.tokens)
                {
                    if (span.IntersectsWith(n.FullSpan))
                    {
                        // node's full span intersects with at least one node to be replaced
                        // so we need to visit node's children to find it.
                        return true;
                    }
                }

                return false;
            }
        }
    }
}