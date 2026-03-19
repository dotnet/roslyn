// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class SyntaxTreeRootTests
    {
        [Fact]
        public void SyntaxTreeCreateAcceptsAnySyntaxNode()
        {
            var node = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("Blah"));
            var tree = SyntaxFactory.SyntaxTree(node);
            CheckTree(tree);
        }

        [Fact]
        public void SyntaxTreeCreateWithoutCloneAcceptsAnySyntaxNode()
        {
            var node = SyntaxFactory.CatchClause(SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName(typeof(InvalidOperationException).Name)), null, SyntaxFactory.Block());
            var tree = CSharpSyntaxTree.CreateWithoutClone(node, CSharpParseOptions.Default);
            CheckTree(tree);
        }

        [Fact]
        public void SyntaxTreeHasCompilationUnitRootReturnsTrueForFullDocument()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"class Program { static void Main() { System.Console.WriteLine(""Wah""); } }");
            Assert.True(tree.HasCompilationUnitRoot);
            Assert.Equal(typeof(CompilationUnitSyntax), tree.GetRoot().GetType());
        }

        [Fact]
        public void SyntaxTreeHasCompilationUnitRootReturnsFalseForArbitrarilyRootedTree()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.FromClause("Nay", SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(823))));
            SyntaxNode root;
            Assert.True(tree.TryGetRoot(out root));
            Assert.False(tree.HasCompilationUnitRoot);
            Assert.NotEqual(typeof(CompilationUnitSyntax), root.GetType());
        }

        [Fact]
        public void CompilationDoesNotAcceptArbitrarilyRootedTree()
        {
            var arbitraryTree = SyntaxFactory.SyntaxTree(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Wooh")));
            var parsedTree = SyntaxFactory.ParseSyntaxTree("");
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Grrr", syntaxTrees: new[] { arbitraryTree }));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("Wah").AddSyntaxTrees(arbitraryTree));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Bahh", syntaxTrees: new[] { parsedTree }).ReplaceSyntaxTree(parsedTree, arbitraryTree));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Woo").GetSemanticModel(arbitraryTree));
        }

        [Fact]
        public void SyntaxFactoryIsCompleteSubmissionShouldNotThrowForArbitrarilyRootedTree()
        {
            var tree = SyntaxFactory.SyntaxTree(
                SyntaxFactory.LetClause("Blah", SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(54))),
                options: TestOptions.Script);
            SyntaxFactory.IsCompleteSubmission(tree);
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeIsEmptyWhenCreatingUnboundNode()
        {
            var node = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3));
            var syntaxTreeField = typeof(CSharpSyntaxNode).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Single(f => f.FieldType == typeof(SyntaxTree));
            Assert.Null(syntaxTreeField.GetValue(node));
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeIsIdenticalOnSubsequentGets()
        {
            var node = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3));
            var tree = node.SyntaxTree;
            Assert.Equal(tree, node.SyntaxTree);
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeReturnsParentsSyntaxTree()
        {
            var node = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression,
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(3)));
            var childTree = node.Operand.SyntaxTree;
            var parentTree = node.SyntaxTree;
            // Don't inline these variables - order of evaluation and initialization would change
            Assert.Equal(parentTree, childTree);
        }

        [Fact]
        public void SyntaxNodeSyntaxTreeReturnsOriginalSyntaxTree()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class TheClass { }");
            Assert.Equal(tree, tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single().SyntaxTree);
        }

        private void CheckTree(SyntaxTree tree)
        {
            Assert.Throws<InvalidCastException>(() => { var _ = (CSharpSyntaxTree)(Object)tree.GetCompilationUnitRoot(); });
            Assert.Throws<ArgumentNullException>(() => { tree.GetDiagnostics((CSharpSyntaxNode)null); });
            Assert.Throws<InvalidOperationException>(() => { tree.GetDiagnostics(default(SyntaxToken)); });
            Assert.Throws<ArgumentNullException>(() => { tree.GetDiagnostics((SyntaxNode)null); });
            Assert.Throws<InvalidOperationException>(() => { tree.GetDiagnostics(default(SyntaxTrivia)); });
        }
    }
}
