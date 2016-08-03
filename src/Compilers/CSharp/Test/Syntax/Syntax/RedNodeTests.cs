// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class RedNodeTests
    {
        private class TokenDeleteRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                return SyntaxFactory.MissingToken(token.Kind());
            }
        }

        private class IdentityRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode DefaultVisit(SyntaxNode node)
            {
                return node;
            }
        }

        private class NonRecursiveTokenDeleteRewriter : CSharpNonRecursiveSyntaxRewriter
        {
            public override SyntaxToken VisitToken(SyntaxToken original, SyntaxToken rewritten)
            {
                return SyntaxFactory.MissingToken(rewritten.Kind());
            }
        }

        private class NonRecursiveIdentityRewriter : CSharpNonRecursiveSyntaxRewriter
        {
        }

        private class SkippedNonRecursiveRewriter : CSharpNonRecursiveSyntaxRewriter
        {
            protected override bool ShouldRewriteChildren(SyntaxNodeOrToken nodeOrToken, out SyntaxNodeOrToken rewritten)
            {
                rewritten = nodeOrToken;
                return ! (nodeOrToken.IsNode && nodeOrToken.AsNode() is LiteralExpressionSyntax);
            }

            public int VisitNodeCallsCount { get; private set; }

            public override SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
            {
                this.VisitNodeCallsCount++;
                return base.VisitNode(original, rewritten);
            }
        }

        private class SkippedAndTransformedNonRecursiveRewriter : CSharpNonRecursiveSyntaxRewriter
        {
            private SkipLiteralNonRecursiveRewriter _skipRewriter = new SkipLiteralNonRecursiveRewriter();

            protected override bool ShouldRewriteChildren(SyntaxNodeOrToken nodeOrToken, out SyntaxNodeOrToken rewritten)
            {
                if (nodeOrToken.IsNode)
                {
                    var value = _skipRewriter.Visit(nodeOrToken.AsNode());
                    rewritten = _skipRewriter.Rewriten;
                    return ! value;
                }

                rewritten = nodeOrToken;
                return true;
            }

            public int VisitNodeCallsCount { get; private set; }

            public override SyntaxNode VisitNode(SyntaxNode original, SyntaxNode rewritten)
            {
                this.VisitNodeCallsCount++;
                return base.VisitNode(original, rewritten);
            }

            private class SkipLiteralNonRecursiveRewriter : CSharpSyntaxVisitor<bool>
            {
                public SyntaxNode Rewriten { get; private set; }

                public override bool VisitLiteralExpression(LiteralExpressionSyntax node)
                {
                    Rewriten = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0));
                    return true;
                }
            }
        }

        internal class ToStringWalker : CSharpNonRecursiveSyntaxWalker
        {
            private StringBuilder _sb;

            public new string Visit(SyntaxNode node)
            {
                this._sb = new StringBuilder();
                base.Visit(node);
                return this._sb.ToString();
            }

            public override void VisitToken(SyntaxToken token)
            {
                _sb.Append(token.ToFullString());
            }
        }

        internal class CountingWalker : CSharpNonRecursiveSyntaxWalker
        {
            public int NodesCount { get; private set; }
            public int TokensCount { get; private set; }

            public override void VisitNode(SyntaxNode node)
            {
                NodesCount++;
                base.VisitNode(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                TokensCount++;
                base.VisitToken(token);
            }
        }

        internal class SkippedCountingWalker : CountingWalker
        {
            protected override bool ShouldVisitChildren(SyntaxNode node)
            {
                return ! (node is LiteralExpressionSyntax);
            }
        }

        [Fact]
        public void TestLongExpression()
        {
            var longExpression = Enumerable.Range(0, 1000000).Select(i => (ExpressionSyntax)SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i))).Aggregate((i, j) => SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, i, j));

            Assert.Throws(typeof(InsufficientExecutionStackException), () => new IdentityRewriter().Visit(longExpression));
        }

        [Fact]
        public void TestLongExpressionWithNonRecursiveRewriter()
        {
            var longExpression = Enumerable.Range(0, 1000000).Select(i => (ExpressionSyntax)SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i))).Aggregate((i, j) => SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, i, j));

            var exp = new NonRecursiveIdentityRewriter().Visit(longExpression);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 1000000; i++)
            {
                sb.Append(i);
                sb.Append("+");
            }
            sb.Length -= 1;
            Assert.Equal(sb.ToString(), exp.ToString());
        }

        [Fact]
        public void TestNonRecursiveWalker()
        {
            string code = "if (a)\r\n  b .  Foo();";
            StatementSyntax statement = SyntaxFactory.ParseStatement(code);
            Assert.Equal(code, new ToStringWalker().Visit(statement));
        }

        [Fact]
        public void TestNonRecursiveWalkerCount()
        {
            string code = "1 + 2 + 3";
            ExpressionSyntax expression = SyntaxFactory.ParseExpression(code);
            var countingWalker = new CountingWalker();
            countingWalker.Visit(expression);
            Assert.Equal(5, countingWalker.NodesCount);
            Assert.Equal(5, countingWalker.TokensCount);
        }

        [Fact]
        public void TestSkippedNonRecursiveWalkerCount()
        {
            string code = "1 + 2 + a";
            ExpressionSyntax expression = SyntaxFactory.ParseExpression(code);
            var countingWalker = new SkippedCountingWalker();
            countingWalker.Visit(expression);
            Assert.Equal(3, countingWalker.NodesCount);
            Assert.Equal(3, countingWalker.TokensCount);
        }

        [Fact]
        public void TestNonRecursiveRewriterSkip()
        {
            string code = "1 + 2 + 3";
            ExpressionSyntax expression = SyntaxFactory.ParseExpression(code);
            var skippedNonRecursiveRewriter = new SkippedNonRecursiveRewriter();
            var newExpression = skippedNonRecursiveRewriter.Visit(expression);
            Assert.Equal(2, skippedNonRecursiveRewriter.VisitNodeCallsCount);
            Assert.Equal(expression, newExpression);
        }

        [Fact]
        public void TestNonRecursiveRewriterSkipAndTransform()
        {
            string code = "1 + 2 + 3";
            ExpressionSyntax expression = SyntaxFactory.ParseExpression(code);
            var skippedAndTransformedNonRecursiveRewriter = new SkippedAndTransformedNonRecursiveRewriter();
            var transformedExpr = skippedAndTransformedNonRecursiveRewriter.Visit(expression);
            Assert.Equal(2, skippedAndTransformedNonRecursiveRewriter.VisitNodeCallsCount);
            Assert.Equal("0+ 0+ 0", transformedExpr.ToString());
        }
    }
}
