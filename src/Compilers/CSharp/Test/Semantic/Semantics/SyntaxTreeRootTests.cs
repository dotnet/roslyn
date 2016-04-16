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
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics
{
    public class SyntaxTreeRootTests : SpeculativeSemanticModelTestsBase
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
            var tree = CSharpSyntaxTree.CreateWithoutClone(node);
            CheckTree(tree);
        }

        [Fact]
        public void SyntaxTreeHasCompilationUnitRootReturnsTrueForFullDocument()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"class Program { static void Main() { System.Console.WriteLine(""Wah""); } }");
            Assert.Equal(true, tree.HasCompilationUnitRoot);
            Assert.Equal(typeof(CompilationUnitSyntax), tree.GetRoot().GetType());
        }

        [Fact]
        public void SyntaxTreeHasCompilationUnitRootReturnsFalseForArbitrarilyRootedTree()
        {
            var tree = SyntaxFactory.SyntaxTree(SyntaxFactory.FromClause("Nay", SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(823))));
            SyntaxNode root;
            Assert.Equal(true, tree.TryGetRoot(out root));
            Assert.Equal(false, tree.HasCompilationUnitRoot);
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
            Assert.Equal(null, syntaxTreeField.GetValue(node));
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
#if false // https://github.com/dotnet/roslyn/issues/4453
            CheckAllMembers(
                tree,
                new Dictionary<Type, Func<object>> 
                { 
                    { typeof(CSharpSyntaxTree), () => tree },
                    { typeof(TextSpan), () => TextSpan.FromBounds(0, 0) },
                    { typeof(SourceText), () => new StringText("class { }") },
                    { typeof(SyntaxNodeOrToken), () => new SyntaxNodeOrToken(tree.GetRoot()) },
                    { typeof(SyntaxNodeOrToken), () => new SyntaxNodeOrToken(tree.GetRoot()) },
                },
                new Dictionary<MemberInfo, Type>
                {
                    { typeof(CSharpSyntaxTree).GetMethod("GetCompilationUnitRoot"), typeof(InvalidCastException) },
                    { typeof(CSharpSyntaxTree).GetMethod("GetDiagnostics", new[] { typeof(CSharpSyntaxNode) }), typeof(ArgumentNullException) },
                    { typeof(CSharpSyntaxTree).GetMethod("GetDiagnostics", new[] { typeof(SyntaxToken) }), typeof(InvalidOperationException) },
                    { typeof(CSharpSyntaxTree).GetMethod("GetDiagnostics", new[] { typeof(SyntaxTrivia) }), typeof(InvalidOperationException) },
                    { typeof(CSharpSyntaxTree).GetMethod("GetDiagnostics", new[] { typeof(SyntaxNode) }), typeof(ArgumentNullException) },
                    { typeof(CSharpSyntaxTree).GetMethod("GetDiagnostics", new[] { typeof(SyntaxToken) }), typeof(InvalidOperationException) },
                    { typeof(CSharpSyntaxTree).GetMethod("GetDiagnostics", new[] { typeof(SyntaxTrivia) }), typeof(InvalidOperationException) },
                });
#endif
        }
    }
}
