using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal class SyntaxTriviaReplacer
    {
        internal static TRoot Replace<TRoot>(TRoot root, SyntaxTrivia oldTrivia, SyntaxTriviaList newTrivia)
            where TRoot : SyntaxNode
        {
            if (oldTrivia == newTrivia)
            {
                return root;
            }

            return (TRoot)new SingleTriviaReplacer(oldTrivia, newTrivia).Visit(root);
        }

        internal static SyntaxToken Replace(SyntaxToken token, SyntaxTrivia oldTrivia, SyntaxTriviaList newTrivia)
        {
            if (oldTrivia == newTrivia)
            {
                return token;
            }

            return new SingleTriviaReplacer(oldTrivia, newTrivia).VisitStartToken(token);
        }

        internal static TRoot Replace<TRoot>(TRoot root, IEnumerable<SyntaxTrivia> oldTrivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList> computeReplacementTrivia)
            where TRoot : SyntaxNode
        {
            var oldTriviaArray = oldTrivia.ToArray();
            if (oldTriviaArray.Length == 0)
            {
                return root;
            }

            return (TRoot)new MultipleTriviaReplacer(oldTriviaArray, computeReplacementTrivia).Visit(root);
        }

        internal static SyntaxToken Replace(SyntaxToken token, IEnumerable<SyntaxTrivia> oldTrivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList> computeReplacementTrivia)
        {
            var oldTriviaArray = oldTrivia.ToArray();
            if (oldTriviaArray.Length == 0)
            {
                return token;
            }

            return new MultipleTriviaReplacer(oldTriviaArray, computeReplacementTrivia).VisitStartToken(token);
        }

        private class SingleTriviaReplacer : SyntaxRewriter
        {
            private readonly SyntaxTrivia oldTrivia;
            private readonly SyntaxTriviaList newTrivia;
            private readonly TextSpan oldTriviaFullSpan;

            public SingleTriviaReplacer(SyntaxTrivia oldTrivia, SyntaxTriviaList newTrivia) :
                base(oldTrivia.IsPartOfStructuredTrivia())
            {
                this.oldTrivia = oldTrivia;
                this.newTrivia = newTrivia;
                this.oldTriviaFullSpan = oldTrivia.FullSpan;
            }

            public SyntaxToken VisitStartToken(SyntaxToken token)
            {
                return this.VisitToken(token);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (token.FullSpan.IntersectsWith(this.oldTriviaFullSpan))
                {
                    var leading = token.LeadingTrivia;
                    var trailing = token.TrailingTrivia;

                    if (this.oldTriviaFullSpan.Start < token.SpanStart)
                    {
                        token = token.WithLeadingTrivia(this.VisitList(leading));
                    }
                    else
                    {
                        token = token.WithTrailingTrivia(this.VisitList(trailing));
                    }
                }

                return token;
            }

            public override SyntaxTriviaList VisitListElement(SyntaxTrivia trivia)
            {
                if (trivia == this.oldTrivia)
                {
                    return this.newTrivia;
                }

                if (this.VisitIntoStructuredTrivia &&
                    trivia.FullSpan.IntersectsWith(this.oldTriviaFullSpan))
                {
                    return base.VisitTrivia(trivia);
                }

                return trivia;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node != null)
                {
                    if (node.FullSpan.IntersectsWith(this.oldTriviaFullSpan))
                    {
                        return base.Visit(node);
                    }
                }

                return node;
            }
        }

        private class MultipleTriviaReplacer : SyntaxRewriter
        {
            private readonly SyntaxTrivia[] trivia;
            private readonly HashSet<SyntaxTrivia> triviaSet;
            private readonly TextSpan totalSpan;
            private readonly Func<SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList> computeReplacementTrivia;

            public MultipleTriviaReplacer(SyntaxTrivia[] trivia, Func<SyntaxTrivia, SyntaxTrivia, SyntaxTriviaList> computeReplacementTrivia) :
                base(trivia.Any(t => t.IsPartOfStructuredTrivia()))
            {
                this.trivia = trivia;
                this.triviaSet = new HashSet<SyntaxTrivia>(this.trivia);
                this.totalSpan = ComputeTotalSpan(this.trivia);
                this.computeReplacementTrivia = computeReplacementTrivia;
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

            public SyntaxToken VisitStartToken(SyntaxToken token)
            {
                return this.VisitToken(token);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (this.ShouldVisit(token.FullSpan))
                {
                    return base.VisitToken(token);
                }

                return token;
            }

            public override SyntaxTriviaList VisitListElement(SyntaxTrivia trivia)
            {
                var result = trivia;

                if (this.VisitIntoStructuredTrivia && trivia.HasStructure && this.ShouldVisit(trivia.FullSpan))
                {
                    result = base.VisitTrivia(trivia);
                }

                if (this.triviaSet.Contains(trivia))
                {
                    return this.computeReplacementTrivia(trivia, result);
                }

                return result;
            }

            private static TextSpan ComputeTotalSpan(SyntaxTrivia[] trivia)
            {
                var span0 = trivia[0].FullSpan;
                int start = span0.Start;
                int end = span0.End;

                for (int i = 1; i < trivia.Length; i++)
                {
                    var span = trivia[i].FullSpan;
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

                foreach (var n in this.trivia)
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