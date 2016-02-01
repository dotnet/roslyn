// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Test SyntaxRewriter and InternalSyntax.SyntaxRewriter.
    /// </summary>
    public class SyntaxRewriterTests
    {
        #region Green Tree / SeparatedSyntaxList

        [Fact]
        public void TestGreenSeparatedDeleteNone()
        {
            //the type argument list is a SeparateSyntaxList
            var input = "A<B,C,D>"; //NB: no whitespace, since it would be deleted
            var output = input;

            var rewriter = new GreenRewriter();

            TestGreen(input, output, rewriter, isExpr: true);
        }

        #endregion Green Tree / SeparatedSyntaxList

        #region Green Tree / SyntaxList

        [Fact]
        public void TestGreenDeleteNone()
        {
            //statements in block constitute a SyntaxList
            var input = "{A();B();C();}"; //NB: no whitespace, since it would be deleted
            var output = input;

            var rewriter = new GreenRewriter();

            TestGreen(input, output, rewriter, isExpr: false);
        }


        #endregion Green Tree / SyntaxList

        #region Red Tree / SeparatedSyntaxList

        [Fact]
        public void TestRedSeparatedDeleteNone()
        {
            //the type argument list is a SeparateSyntaxList
            var input = "A<B,C,D>"; //NB: no whitespace, since it would be deleted
            var output = input;

            var rewriter = new RedRewriter();

            TestRed(input, output, rewriter, isExpr: true);
        }

        [Fact]
        public void TestRedSeparatedDeleteSome()
        {
            //the type argument list is a SeparateSyntaxList
            var input = "A<B,C,D>"; //NB: no whitespace, since it would be deleted

            //delete the middle type argument (should clear the following comma)
            var rewriter = new RedRewriter(rewriteNode: node =>
                (node.IsKind(SyntaxKind.IdentifierName) && node.ToString() == "C") ? null : node);

            Exception caught = null;
            try
            {
                TestRed(input, "", rewriter, isExpr: true);
            }
            catch (Exception e)
            {
                caught = e;
            }

            Assert.NotNull(caught);
            Assert.Equal(true, caught is InvalidOperationException);
        }

        [Fact]
        public void TestRedSeparatedDeleteAll()
        {
            //the type argument list is a SeparateSyntaxList
            var input = "A<B,C,D>"; //NB: no whitespace, since it would be deleted

            //delete all type arguments, should clear the intervening commas
            var rewriter = new RedRewriter(rewriteNode: node =>
                (node.IsKind(SyntaxKind.IdentifierName) && node.ToString() != "A") ? null : node);

            Exception caught = null;
            try
            {
                TestRed(input, "", rewriter, isExpr: true);
            }
            catch (Exception e)
            {
                caught = e;
            }

            Assert.NotNull(caught);
            Assert.Equal(true, caught is InvalidOperationException);
        }

        #endregion Red Tree / SeparatedSyntaxList

        #region Red Tree / SyntaxTokenList

        [Fact]
        public void TestRedTokenDeleteNone()
        {
            //commas in an implicit array creation constitute a SyntaxTokenList
            var input = "x=new[,,]{{{}}};"; //NB: no whitespace, since it would be deleted
            var output = input;

            var rewriter = new RedRewriter();

            TestRed(input, output, rewriter, isExpr: false);
        }

        [Fact]
        public void TestRedTokenDeleteSome()
        {
            //commas in an implicit array creation constitute a SyntaxTokenList
            var input = "x=new[,,]{{{}}};"; //NB: no whitespace, since it would be deleted
            var output = "x=new[,]{{{}}};";

            //delete one comma
            bool first = true;
            var rewriter = new RedRewriter(rewriteToken: token =>
            {
                if (token.Kind() == SyntaxKind.CommaToken && first)
                {
                    first = false;
                    return default(SyntaxToken);
                }
                return token;
            });

            TestRed(input, output, rewriter, isExpr: false);
        }

        [Fact]
        public void TestRedTokenDeleteAll()
        {
            //commas in an implicit array creation constitute a SyntaxTokenList
            var input = "x=new[,,]{{{}}};"; //NB: no whitespace, since it would be deleted
            var output = "x=new[]{{{}}};";

            //delete all commas
            var rewriter = new RedRewriter(rewriteToken: token =>
                (token.Kind() == SyntaxKind.CommaToken) ? default(SyntaxToken) : token);

            TestRed(input, output, rewriter, isExpr: false);
        }

        #endregion Red Tree / SyntaxTokenList

        #region Red Tree / SyntaxNodeOrTokenList

        //These only in the syntax tree inside SeparatedSyntaxLists, so they are not visitable.
        //We can't call this directly due to its protection level.

        #endregion Red Tree / SyntaxNodeOrTokenList

        #region Red Tree / SyntaxTriviaList

        [Fact]
        public void TestRedTriviaDeleteNone()
        {
            //whitespace and comments constitute a SyntaxTriviaList
            var input = " a(); //comment"; //NB: no whitespace, since it would be deleted
            var output = input;

            var rewriter = new RedRewriter();

            TestRed(input, output, rewriter, isExpr: false);
        }

        [Fact]
        public void TestRedTriviaDeleteSome()
        {
            //whitespace and comments constitute a SyntaxTriviaList
            var input = " a(); //comment"; //NB: no whitespace, since it would be deleted
            var output = "a();//comment";

            //delete all whitespace trivia (leave comments)
            var rewriter = new RedRewriter(rewriteTrivia: trivia =>
                trivia.Kind() == SyntaxKind.WhitespaceTrivia ? default(SyntaxTrivia) : trivia);

            TestRed(input, output, rewriter, isExpr: false);
        }

        [Fact]
        public void TestRedTriviaDeleteAll()
        {
            //whitespace and comments constitute a SyntaxTriviaList
            var input = " a(); //comment"; //NB: no whitespace, since it would be deleted
            var output = "a();";

            //delete all trivia
            var rewriter = new RedRewriter(rewriteTrivia: trivia => default(SyntaxTrivia));

            TestRed(input, output, rewriter, isExpr: false);
        }

        #endregion Red Tree / SyntaxTriviaList

        #region Red Tree / SyntaxList

        [Fact]
        public void TestRedDeleteNone()
        {
            //statements in block constitute a SyntaxList
            var input = "{A();B();C();}"; //NB: no whitespace, since it would be deleted
            var output = input;

            var rewriter = new RedRewriter();

            TestRed(input, output, rewriter, isExpr: false);
        }

        [Fact]
        public void TestRedDeleteSome()
        {
            //statements in block constitute a SyntaxList
            var input = "{A();B();C();}"; //NB: no whitespace, since it would be deleted
            var output = "{A();C();}";

            //delete the middle statement
            var rewriter = new RedRewriter(rewriteNode: node =>
                (node.IsKind(SyntaxKind.ExpressionStatement) && node.ToString().Contains("B")) ? null : node);

            TestRed(input, output, rewriter, isExpr: false);
        }

        [Fact]
        public void TestRedDeleteAll()
        {
            //statements in block constitute a SyntaxList
            var input = "{A();B();C();}"; //NB: no whitespace, since it would be deleted
            var output = "{}";

            //delete all statements
            var rewriter = new RedRewriter(rewriteNode: node =>
                (node.IsKind(SyntaxKind.ExpressionStatement)) ? null : node);

            TestRed(input, output, rewriter, isExpr: false);
        }

        #endregion Red Tree / SyntaxList

        #region Misc

        [Fact]
        public void TestRedConsecutiveSeparators()
        {
            //there are omitted array size expressions between the commas
            var input = "int[,,]a;"; //NB: no whitespace, since it would be deleted

            bool first = true;
            var rewriter = new RedRewriter(rewriteNode: node =>
            {
                if (node != null && node.IsKind(SyntaxKind.OmittedArraySizeExpression) && first)
                {
                    first = false;
                    return null;
                }
                return node;
            });

            Exception caught = null;
            try
            {
                TestRed(input, "", rewriter, isExpr: false);
            }
            catch (Exception e)
            {
                caught = e;
            }

            Assert.NotNull(caught);
            Assert.Equal(true, caught is InvalidOperationException);
        }

        [Fact]
        public void TestRedSeparatedDeleteLast()
        {
            //the type argument list is a SeparateSyntaxList
            var input = "A<B,C,D>"; //NB: no whitespace, since it would be deleted
            var output = "A<B,C,>";

            //delete the last type argument (should clear the *preceding* comma)
            var rewriter = new RedRewriter(rewriteNode: node =>
                (node.IsKind(SyntaxKind.IdentifierName) && node.ToString() == "D") ? null : node);

            TestRed(input, output, rewriter, isExpr: true);
        }

        [Fact]
        public void TestSyntaxTreeForFactoryGenerated()
        {
            var node = SyntaxFactory.ClassDeclaration("Class1");
            Assert.NotNull(node.SyntaxTree);
            Assert.False(node.SyntaxTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?");
            Assert.Same(node, node.SyntaxTree.GetRoot());
        }

        [Fact]
        public void TestSyntaxTreeForParsedSyntaxNode()
        {
            var node1 = SyntaxFactory.ParseCompilationUnit("class Class1<T> { }");
            Assert.NotNull(node1.SyntaxTree);
            Assert.True(node1.SyntaxTree.HasCompilationUnitRoot, "how did we get a non-CompilationUnit root?");
            Assert.Same(node1, node1.SyntaxTree.GetRoot());
            var node2 = SyntaxFactory.ParseExpression("2 + 2");
            Assert.NotNull(node2.SyntaxTree);
            Assert.False(node2.SyntaxTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?");
            Assert.Same(node2, node2.SyntaxTree.GetRoot());
        }

        [Fact]
        public void TestSyntaxTreeForSyntaxTreeWithReplacedToken()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class Class1<T> { }");
            var tokenT = tree.GetCompilationUnitRoot().DescendantTokens().Where(t => t.ToString() == "T").Single();
            Assert.Same(tree, tree.GetCompilationUnitRoot().ReplaceToken(tokenT, tokenT).SyntaxTree);
            var newRoot = tree.GetCompilationUnitRoot().ReplaceToken(tokenT, SyntaxFactory.Identifier("U"));
            Assert.NotNull(newRoot.SyntaxTree);
            Assert.True(newRoot.SyntaxTree.HasCompilationUnitRoot, "how did we get a non-CompilationUnit root?");
            Assert.Same(newRoot, newRoot.SyntaxTree.GetRoot());
        }

        [Fact]
        public void TestSyntaxTreeForSyntaxTreeWithReplacedNode()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class Class1 : Class2<T> { }");
            var typeName = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.GenericName)).Single();
            Assert.Same(tree, tree.GetCompilationUnitRoot().ReplaceNode(typeName, typeName).SyntaxTree);
            var newRoot = tree.GetCompilationUnitRoot().ReplaceNode(typeName, SyntaxFactory.ParseTypeName("Class2<U>"));
            Assert.NotNull(newRoot.SyntaxTree);
            Assert.True(newRoot.SyntaxTree.HasCompilationUnitRoot, "how did we get a non-CompilationUnit root?");
            Assert.Same(newRoot, newRoot.SyntaxTree.GetRoot());
        }

        [Fact]
        public void TestSyntaxTreeForRewrittenRoot()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class Class1<T> { }");
            Assert.NotNull(tree.GetCompilationUnitRoot().SyntaxTree);
            var rewriter = new BadRewriter();
            var rewrittenRoot = rewriter.Visit(tree.GetCompilationUnitRoot());
            Assert.NotNull(rewrittenRoot.SyntaxTree);
            Assert.True(((SyntaxTree)rewrittenRoot.SyntaxTree).HasCompilationUnitRoot, "how did we get a non-CompilationUnit root?");
            Assert.Same(rewrittenRoot, rewrittenRoot.SyntaxTree.GetRoot());
        }

        [WorkItem(545049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545049")]
        [WorkItem(896538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/896538")]
        [Fact]
        public void RewriteMissingIdentifierInExpressionStatement_ImplicitlyCreatedSyntaxTree()
        {
            var ifStmt1 = (IfStatementSyntax)SyntaxFactory.ParseStatement("if (true)");
            var exprStmt1 = (ExpressionStatementSyntax)ifStmt1.Statement;
            var expr1 = (IdentifierNameSyntax)exprStmt1.Expression;
            var token1 = expr1.Identifier;

            Assert.NotNull(expr1.SyntaxTree);
            Assert.False(expr1.SyntaxTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?");
            Assert.Same(ifStmt1, expr1.SyntaxTree.GetRoot());
            Assert.True(expr1.IsMissing);
            Assert.True(expr1.ContainsDiagnostics);

            Assert.True(token1.IsMissing);
            Assert.False(token1.ContainsDiagnostics);

            var trivia = SyntaxFactory.ParseTrailingTrivia(" ");
            var rewriter = new RedRewriter(rewriteToken: tok => tok.Kind() == SyntaxKind.IdentifierToken ? tok.WithLeadingTrivia(trivia) : tok);

            var ifStmt2 = (IfStatementSyntax)rewriter.Visit(ifStmt1);
            var exprStmt2 = (ExpressionStatementSyntax)ifStmt2.Statement;
            var expr2 = (IdentifierNameSyntax)exprStmt2.Expression;
            var token2 = expr2.Identifier;

            Assert.NotEqual(expr1, expr2);
            Assert.NotNull(expr2.SyntaxTree);
            Assert.False(expr2.SyntaxTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?");
            Assert.Same(ifStmt2, expr2.SyntaxTree.GetRoot());
            Assert.True(expr2.IsMissing);
            Assert.False(expr2.ContainsDiagnostics); //gone after rewrite

            Assert.NotEqual(token1, token2);
            Assert.True(token2.IsMissing);
            Assert.False(token2.ContainsDiagnostics);

            Assert.True(IsStatementExpression(expr1));
            Assert.True(IsStatementExpression(expr2));
        }

        internal static bool IsStatementExpression(CSharpSyntaxNode expression)
        {
            return SyntaxFacts.IsStatementExpression(expression);
        }

        [WorkItem(545049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545049")]
        [WorkItem(896538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/896538")]
        [Fact]
        public void RewriteMissingIdentifierInExpressionStatement_WithSyntaxTree()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { static void Main() { if (true) } }");
            var ifStmt1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType<IfStatementSyntax>().Single();
            var exprStmt1 = (ExpressionStatementSyntax)ifStmt1.Statement;
            var expr1 = (IdentifierNameSyntax)exprStmt1.Expression;
            var token1 = expr1.Identifier;

            Assert.Equal(tree1, expr1.SyntaxTree);
            Assert.True(expr1.IsMissing);
            Assert.True(expr1.ContainsDiagnostics);

            Assert.True(token1.IsMissing);
            Assert.False(token1.ContainsDiagnostics);

            var trivia = SyntaxFactory.ParseTrailingTrivia(" ");
            var rewriter = new RedRewriter(rewriteToken: tok => tok.Kind() == SyntaxKind.IdentifierToken ? tok.WithLeadingTrivia(trivia) : tok);

            var ifStmt2 = (IfStatementSyntax)rewriter.Visit(ifStmt1);
            var exprStmt2 = (ExpressionStatementSyntax)ifStmt2.Statement;
            var expr2 = (IdentifierNameSyntax)exprStmt2.Expression;
            var token2 = expr2.Identifier;

            Assert.NotEqual(expr1, expr2);
            Assert.NotNull(expr2.SyntaxTree);
            Assert.False(expr2.SyntaxTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?");
            Assert.Same(ifStmt2, expr2.SyntaxTree.GetRoot());
            Assert.True(expr2.IsMissing);
            Assert.False(expr2.ContainsDiagnostics); //gone after rewrite

            Assert.NotEqual(token1, token2);
            Assert.True(token2.IsMissing);
            Assert.False(token2.ContainsDiagnostics);

            Assert.True(IsStatementExpression(expr1));
            Assert.True(IsStatementExpression(expr2));
        }

        [Fact]
        public void RemoveDocCommentNode()
        {
            var oldSource = @"
/// <see cref='C'/>
class C { }
";

            var expectedNewSource = @"
/// 
class C { }
";

            var oldTree = CSharpTestBase.Parse(oldSource, options: TestOptions.RegularWithDocumentationComments);
            var oldRoot = oldTree.GetRoot();
            var xmlNode = oldRoot.DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().Single();
            var newRoot = oldRoot.RemoveNode(xmlNode, SyntaxRemoveOptions.KeepDirectives);

            Assert.Equal(expectedNewSource, newRoot.ToFullString());
        }

        [WorkItem(991474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/991474")]
        [Fact]
        public void ReturnNullFromStructuredTriviaRoot_Succeeds()
        {
            var text =
@"#region
class C { }
#endregion";

            var expectedText =
@"class C { }
#endregion";

            var root = SyntaxFactory.ParseCompilationUnit(text);
            var newRoot = new RemoveRegionRewriter().Visit(root);

            Assert.Equal(expectedText, newRoot.ToFullString());
        }

        private class RemoveRegionRewriter : CSharpSyntaxRewriter
        {
            public RemoveRegionRewriter()
                : base(visitIntoStructuredTrivia: true)
            {
            }

            public override SyntaxNode VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
            {
                return null;
            }
        }

        #endregion Misc

        #region Helper Methods

        private static void TestGreen(string input, string output, GreenRewriter rewriter, bool isExpr)
        {
            var red = isExpr ? (CSharpSyntaxNode)SyntaxFactory.ParseExpression(input) : SyntaxFactory.ParseStatement(input);
            var green = red.CsGreen;

            Assert.False(green.ContainsDiagnostics);

            var result = rewriter.Visit(green);

            Assert.Equal(input == output, ReferenceEquals(green, result));
            Assert.Equal(output, result.ToFullString());
        }

        private static void TestRed(string input, string output, RedRewriter rewriter, bool isExpr)
        {
            var red = isExpr ? (CSharpSyntaxNode)SyntaxFactory.ParseExpression(input) : SyntaxFactory.ParseStatement(input);

            Assert.False(red.ContainsDiagnostics);

            var result = rewriter.Visit(red);

            Assert.Equal(input == output, ReferenceEquals(red, result));
            Assert.Equal(output, result.ToFullString());
        }

        #endregion Helper Methods

        #region Helper Types

        /// <summary>
        /// This Rewriter exposes delegates for the methods that would normally be overridden.
        /// </summary>
        internal class GreenRewriter : InternalSyntax.CSharpSyntaxRewriter
        {
            private readonly Func<InternalSyntax.CSharpSyntaxNode, InternalSyntax.CSharpSyntaxNode> _rewriteNode;
            private readonly Func<InternalSyntax.SyntaxToken, InternalSyntax.SyntaxToken> _rewriteToken;

            internal GreenRewriter(
                Func<InternalSyntax.CSharpSyntaxNode, InternalSyntax.CSharpSyntaxNode> rewriteNode = null,
                Func<InternalSyntax.SyntaxToken, InternalSyntax.SyntaxToken> rewriteToken = null)
            {
                _rewriteNode = rewriteNode;
                _rewriteToken = rewriteToken;
            }

            public override InternalSyntax.CSharpSyntaxNode Visit(InternalSyntax.CSharpSyntaxNode node)
            {
                var visited = base.Visit(node);
                return _rewriteNode == null ? visited : _rewriteNode(visited);
            }

            public override InternalSyntax.CSharpSyntaxNode VisitToken(InternalSyntax.SyntaxToken token)
            {
                var visited = (InternalSyntax.SyntaxToken)base.VisitToken(token);
                return _rewriteToken == null ? visited : _rewriteToken(visited);
            }
        }

        /// <summary>
        /// This Rewriter exposes delegates for the methods that would normally be overridden.
        /// </summary>
        internal class RedRewriter : CSharpSyntaxRewriter
        {
            private readonly Func<SyntaxNode, SyntaxNode> _rewriteNode;
            private readonly Func<SyntaxToken, SyntaxToken> _rewriteToken;
            private readonly Func<SyntaxTrivia, SyntaxTrivia> _rewriteTrivia;

            internal RedRewriter(
                Func<SyntaxNode, SyntaxNode> rewriteNode = null,
                Func<SyntaxToken, SyntaxToken> rewriteToken = null,
                Func<SyntaxTrivia, SyntaxTrivia> rewriteTrivia = null)
            {
                _rewriteNode = rewriteNode;
                _rewriteToken = rewriteToken;
                _rewriteTrivia = rewriteTrivia;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                var visited = base.Visit(node);
                return _rewriteNode == null ? visited : _rewriteNode(visited);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var visited = base.VisitToken(token);
                return _rewriteToken == null ? visited : _rewriteToken(token);
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                var visited = base.VisitTrivia(trivia);
                return _rewriteTrivia == null ? visited : _rewriteTrivia(trivia);
            }
        }

        internal class BadRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                // This is garbage...the identifier can't be "class"...
                return SyntaxFactory.ClassDeclaration(SyntaxFactory.Identifier("class"));
            }
        }

        #endregion Helper Types
    }
}
