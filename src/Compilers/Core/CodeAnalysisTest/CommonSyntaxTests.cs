// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CommonSyntaxTests
    {
        [Fact]
        public void Kinds()
        {
            foreach (CS.SyntaxKind kind in Enum.GetValues(typeof(CS.SyntaxKind)))
            {
                Assert.True(CS.CSharpExtensions.IsCSharpKind((int)kind), kind + " should be C# kind");

                if (kind != CS.SyntaxKind.None && kind != CS.SyntaxKind.List)
                {
                    Assert.False(VB.VisualBasicExtensions.IsVisualBasicKind((int)kind), kind + " should not be VB kind");
                }
            }

            foreach (VB.SyntaxKind kind in Enum.GetValues(typeof(VB.SyntaxKind)))
            {
                Assert.True(VB.VisualBasicExtensions.IsVisualBasicKind((int)kind), kind + " should be VB kind");

                if (kind != VB.SyntaxKind.None && kind != VB.SyntaxKind.List)
                {
                    Assert.False(CS.CSharpExtensions.IsCSharpKind((int)kind), kind + " should not be C# kind");
                }
            }
        }

        [Fact]
        public void SyntaxNodeOrToken()
        {
            var d = default(SyntaxNodeOrToken);

            Assert.True(d.IsToken);
            Assert.False(d.IsNode);

            Assert.Equal(0, d.RawKind);
            Assert.Equal("", d.Language);
            Assert.Equal(default(TextSpan), d.FullSpan);
            Assert.Equal(default(TextSpan), d.Span);
            Assert.Equal("", d.ToString());
            Assert.Equal("", d.ToFullString());
            Assert.Equal("SyntaxNodeOrToken None ", d.GetDebuggerDisplay());
        }

        [Fact]
        public void SyntaxNodeOrToken1()
        {
            var d = (SyntaxNodeOrToken)((SyntaxNode)null);

            Assert.False(d.IsToken);
            Assert.True(d.IsNode);

            Assert.False(d.IsEquivalentTo(default(SyntaxNodeOrToken)));

            Assert.Equal(0, d.RawKind);
            Assert.Equal("", d.Language);
            Assert.Equal(default(TextSpan), d.FullSpan);
            Assert.Equal(default(TextSpan), d.Span);
            Assert.Equal("", d.ToString());
            Assert.Equal("", d.ToFullString());
            Assert.Equal("SyntaxNodeOrToken None ", d.GetDebuggerDisplay());
        }

        [Fact]
        public void CommonSyntaxToString_CSharp()
        {
            SyntaxNode node = CSharp.SyntaxFactory.IdentifierName("test");
            Assert.Equal("test", node.ToString());

            SyntaxNodeOrToken nodeOrToken = node;
            Assert.Equal("test", nodeOrToken.ToString());

            SyntaxToken token = node.DescendantTokens().Single();
            Assert.Equal("test", token.ToString());

            SyntaxTrivia trivia = CSharp.SyntaxFactory.Whitespace("test");
            Assert.Equal("test", trivia.ToString());
        }

        [Fact]
        public void CommonSyntaxToString_VisualBasic()
        {
            SyntaxNode node = VB.SyntaxFactory.IdentifierName("test");
            Assert.Equal("test", node.ToString());

            SyntaxNodeOrToken nodeOrToken = node;
            Assert.Equal("test", nodeOrToken.ToString());

            SyntaxToken token = node.DescendantTokens().Single();
            Assert.Equal("test", token.ToString());

            SyntaxTrivia trivia = VB.SyntaxFactory.Whitespace("test");
            Assert.Equal("test", trivia.ToString());
        }

        [Fact]
        public void CommonSyntaxTriviaSpan_CSharp()
        {
            var csharpToken = CSharp.SyntaxFactory.ParseExpression("1 + 123 /*hello*/").GetLastToken();
            var csharpTriviaList = csharpToken.TrailingTrivia;
            Assert.Equal(2, csharpTriviaList.Count);

            var csharpTrivia = csharpTriviaList.ElementAt(1);
            Assert.Equal(CSharp.SyntaxKind.MultiLineCommentTrivia, CSharp.CSharpExtensions.Kind(csharpTrivia));

            var correctSpan = csharpTrivia.Span;
            Assert.Equal(8, correctSpan.Start);
            Assert.Equal(17, correctSpan.End);

            var commonTrivia = (SyntaxTrivia)csharpTrivia; //direct conversion
            Assert.Equal(correctSpan, commonTrivia.Span);

            var commonTriviaList = (SyntaxTriviaList)csharpTriviaList;

            var commonTrivia2 = commonTriviaList[1]; //from converted list
            Assert.Equal(correctSpan, commonTrivia2.Span);

            var commonToken = (SyntaxToken)csharpToken;
            var commonTriviaList2 = commonToken.TrailingTrivia;

            var commonTrivia3 = commonTriviaList2[1]; //from converted token
            Assert.Equal(correctSpan, commonTrivia3.Span);

            var csharpTrivia2 = (SyntaxTrivia)commonTrivia; //direct conversion
            Assert.Equal(correctSpan, csharpTrivia2.Span);

            var csharpTriviaList2 = (SyntaxTriviaList)commonTriviaList;

            var csharpTrivia3 = csharpTriviaList2.ElementAt(1); //from converted list
            Assert.Equal(correctSpan, csharpTrivia3.Span);
        }

        [Fact]
        public void CommonSyntaxTriviaSpan_VisualBasic()
        {
            var vbToken = VB.SyntaxFactory.ParseExpression("1 + 123 'hello").GetLastToken();
            var vbTriviaList = (SyntaxTriviaList)vbToken.TrailingTrivia;
            Assert.Equal(2, vbTriviaList.Count);

            var vbTrivia = vbTriviaList.ElementAt(1);
            Assert.Equal(VB.SyntaxKind.CommentTrivia, VB.VisualBasicExtensions.Kind(vbTrivia));

            var correctSpan = vbTrivia.Span;
            Assert.Equal(8, correctSpan.Start);
            Assert.Equal(14, correctSpan.End);

            var commonTrivia = (SyntaxTrivia)vbTrivia; //direct conversion
            Assert.Equal(correctSpan, commonTrivia.Span);

            var commonTriviaList = (SyntaxTriviaList)vbTriviaList;

            var commonTrivia2 = commonTriviaList[1]; //from converted list
            Assert.Equal(correctSpan, commonTrivia2.Span);

            var commonToken = (SyntaxToken)vbToken;
            var commonTriviaList2 = commonToken.TrailingTrivia;

            var commonTrivia3 = commonTriviaList2[1]; //from converted token
            Assert.Equal(correctSpan, commonTrivia3.Span);

            var vbTrivia2 = (SyntaxTrivia)commonTrivia; //direct conversion
            Assert.Equal(correctSpan, vbTrivia2.Span);

            var vbTriviaList2 = (SyntaxTriviaList)commonTriviaList;

            var vbTrivia3 = vbTriviaList2.ElementAt(1); //from converted list
            Assert.Equal(correctSpan, vbTrivia3.Span);
        }

        [Fact, WorkItem(824695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824695")]
        public void CSharpSyntax_VisualBasicKind()
        {
            var node = CSharp.SyntaxFactory.Identifier("a");
            Assert.Equal(VB.SyntaxKind.None, VisualBasic.VisualBasicExtensions.Kind(node));
            var token = CSharp.SyntaxFactory.Token(CSharp.SyntaxKind.IfKeyword);
            Assert.Equal(VB.SyntaxKind.None, VisualBasic.VisualBasicExtensions.Kind(token));
            var trivia = CSharp.SyntaxFactory.Comment("c");
            Assert.Equal(VB.SyntaxKind.None, VisualBasic.VisualBasicExtensions.Kind(trivia));
        }

        [Fact, WorkItem(824695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/824695")]
        public void VisualBasicSyntax_CSharpKind()
        {
            var node = VisualBasic.SyntaxFactory.Identifier("a");
            Assert.Equal(CSharp.SyntaxKind.None, CSharp.CSharpExtensions.Kind(node));
            var token = VisualBasic.SyntaxFactory.Token(VisualBasic.SyntaxKind.IfKeyword);
            Assert.Equal(CSharp.SyntaxKind.None, CSharp.CSharpExtensions.Kind(token));
            var trivia = VisualBasic.SyntaxFactory.CommentTrivia("c");
            Assert.Equal(CSharp.SyntaxKind.None, CSharp.CSharpExtensions.Kind(trivia));
        }

        [Fact]
        public void TestTrackNodes()
        {
            var expr = CSharp.SyntaxFactory.ParseExpression("a + b + c + d");

            var exprB = expr.DescendantNodes().OfType<CSharp.Syntax.IdentifierNameSyntax>().First(n => n.Identifier.ToString() == "b");

            var trackedExpr = expr.TrackNodes(exprB);

            // replace each expression with a parenthesized expression
            trackedExpr = trackedExpr.ReplaceNodes(
                        nodes: trackedExpr.DescendantNodes().OfType<CSharp.Syntax.ExpressionSyntax>(),
                        computeReplacementNode: (node, rewritten) => CSharp.SyntaxFactory.ParenthesizedExpression(rewritten));

            trackedExpr = trackedExpr.NormalizeWhitespace();
            Assert.Equal("(((a) + (b)) + (c)) + (d)", trackedExpr.ToString());

            var trackedB = trackedExpr.GetCurrentNodes(exprB).First();
            Assert.Equal(CSharp.SyntaxKind.ParenthesizedExpression, CSharp.CSharpExtensions.Kind(trackedB.Parent));
        }

        [Fact]
        public void TestTrackNodesWithDuplicateIdAnnotations()
        {
            var expr = CSharp.SyntaxFactory.ParseExpression("a + b + c + d");

            var exprB = expr.DescendantNodes().OfType<CSharp.Syntax.IdentifierNameSyntax>().First(n => n.Identifier.ToString() == "b");

            var trackedExpr = expr.TrackNodes(exprB);
            var annotation = trackedExpr.GetAnnotatedNodes(SyntaxNodeExtensions.IdAnnotationKind).First()
                                        .GetAnnotations(SyntaxNodeExtensions.IdAnnotationKind).First();

            // replace each expression with a parenthesized expression
            trackedExpr = trackedExpr.ReplaceNodes(
                        nodes: trackedExpr.DescendantNodes().OfType<CSharp.Syntax.ExpressionSyntax>(),
                        computeReplacementNode: (node, rewritten) => CSharp.SyntaxFactory.ParenthesizedExpression(rewritten).WithAdditionalAnnotations(annotation));

            trackedExpr = trackedExpr.NormalizeWhitespace();
            Assert.Equal("(((a) + (b)) + (c)) + (d)", trackedExpr.ToString());

            var trackedB = trackedExpr.GetCurrentNodes(exprB).First();
            Assert.Equal(CSharp.SyntaxKind.ParenthesizedExpression, CSharp.CSharpExtensions.Kind(trackedB.Parent));
        }
    }
}
