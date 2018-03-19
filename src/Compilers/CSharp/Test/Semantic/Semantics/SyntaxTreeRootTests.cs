// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class SyntaxTreeRootTests
    {
        [Fact]
        public void SyntaxTreeCreateAcceptsAnySyntaxNode()
        {
            UsingDirectiveSyntax node = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Blah"));
            SyntaxTree tree = SyntaxFactory.SyntaxTree(node);
            CheckTree(tree);
        }

        [Fact]
        public void SyntaxTreeCreateWithoutCloneAcceptsAnySyntaxNode()
        {
            CatchClauseSyntax node = SyntaxFactory.CatchClause(SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName(typeof(InvalidOperationException).Name)), null, SyntaxFactory.Block());
            SyntaxTree tree = CSharpSyntaxTree.CreateWithoutClone(node);
            CheckTree(tree);
        }

        [Fact]
        public void SyntaxTreeHasCompilationUnitRootReturnsTrueForFullDocument()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(@"class Program { static void Main() { System.Console.WriteLine(""Wah""); } }");
            Assert.Equal(true, tree.HasCompilationUnitRoot);
            Assert.Equal(typeof(CompilationUnitSyntax), tree.GetRoot().GetType());
        }

        [Fact]
        public void SyntaxTreeHasCompilationUnitRootReturnsFalseForArbitrarilyRootedTree()
        {
            SyntaxTree tree = SyntaxFactory.SyntaxTree(SyntaxFactory.FromClause("Nay", SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(823))));
            SyntaxNode root;
            Assert.Equal(true, tree.TryGetRoot(out root));
            Assert.Equal(false, tree.HasCompilationUnitRoot);
            Assert.NotEqual(typeof(CompilationUnitSyntax), root.GetType());
        }

        [Fact]
        public void CompilationDoesNotAcceptArbitrarilyRootedTree()
        {
            SyntaxTree arbitraryTree = SyntaxFactory.SyntaxTree(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Wooh")));
            SyntaxTree parsedTree = SyntaxFactory.ParseSyntaxTree("");
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Grrr", syntaxTrees: new[] { arbitraryTree }));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("Wah").AddSyntaxTrees(arbitraryTree));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Bahh", syntaxTrees: new[] { parsedTree }).ReplaceSyntaxTree(parsedTree, arbitraryTree));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Woo").GetSemanticModel(arbitraryTree));
        }

        [Fact]
        public void SyntaxFactoryIsCompleteSubmissionShouldNotThrowForArbitrarilyRootedTree()
        {
            SyntaxTree tree = SyntaxFactory.SyntaxTree(
                SyntaxFactory.LetClause("Blah", SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(54))),
                options: TestOptions.Script);
            SyntaxFactory.IsCompleteSubmission(tree);
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeIsEmptyWhenCreatingUnboundNode()
        {
            LiteralExpressionSyntax node = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3));
            FieldInfo syntaxTreeField = typeof(CSharpSyntaxNode).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Single(f => f.FieldType == typeof(SyntaxTree));
            Assert.Equal(null, syntaxTreeField.GetValue(node));
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeIsIdenticalOnSubsequentGets()
        {
            LiteralExpressionSyntax node = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3));
            SyntaxTree tree = node.SyntaxTree;
            Assert.Equal(tree, node.SyntaxTree);
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeReturnsParentsSyntaxTree()
        {
            PrefixUnaryExpressionSyntax node = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression,
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3)));
            SyntaxTree childTree = node.Operand.SyntaxTree;
            SyntaxTree parentTree = node.SyntaxTree;
            // Don't inline these variables - order of evaluation and initialization would change
            Assert.Equal(parentTree, childTree);
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeReturnsOriginalSyntaxTree()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree("class TheClass { }");
            Assert.Equal(tree, tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single().SyntaxTree);
        }

        private void CheckTree(SyntaxTree tree)
        {
            Assert.Throws<InvalidCastException>(() => { var _ = (CSharpSyntaxTree) (Object) tree.GetCompilationUnitRoot(); });
            Assert.Throws<ArgumentNullException>(() => { tree.GetDiagnostics((CSharpSyntaxNode) null); });
            Assert.Throws<InvalidOperationException>(() => { tree.GetDiagnostics(default(SyntaxToken) ); });
            Assert.Throws<ArgumentNullException>(() => { tree.GetDiagnostics((SyntaxNode) null); });
            Assert.Throws<InvalidOperationException>(() => { tree.GetDiagnostics(default(SyntaxTrivia) ); });
        }
    }
}
