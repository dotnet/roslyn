// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace APISampleUnitTestsCS
{
    public class SyntaxTrees
    {
        [Fact]
        public void FindNodeUsingMembers()
        {
            string text = "class C { void M(int i) { } }";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var compilationUnit = (CompilationUnitSyntax)tree.GetRoot();
            var typeDeclaration = (TypeDeclarationSyntax)compilationUnit.Members[0];
            var methodDeclaration = (MethodDeclarationSyntax)typeDeclaration.Members[0];
            ParameterSyntax parameter = methodDeclaration.ParameterList.Parameters[0];
            SyntaxToken parameterName = parameter.Identifier;
            Assert.Equal("i", parameterName.ValueText);
        }

        [Fact]
        public void FindNodeUsingQuery()
        {
            string text = "class C { void M(int i) { } }";
            SyntaxNode root = SyntaxFactory.ParseCompilationUnit(text);
            var parameterDeclaration = root
                .DescendantNodes()
                .OfType<ParameterSyntax>()
                .First();
            Assert.Equal("i", parameterDeclaration.Identifier.ValueText);
        }

        [Fact]
        public void UpdateNode()
        {
            string text = "class C { void M() { } }";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            MethodDeclarationSyntax method = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();
            var newMethod = method.Update(
                method.AttributeLists,
                method.Modifiers,
                method.RefKeyword,
                method.ReturnType,
                method.ExplicitInterfaceSpecifier,
                SyntaxFactory.Identifier("NewMethodName"),
                method.TypeParameterList,
                method.ParameterList,
                method.ConstraintClauses,
                method.Body,
                method.SemicolonToken);

            root = root.ReplaceNode(method, newMethod);
            tree = tree.WithRootAndOptions(root, tree.Options);
            Assert.Equal("class C { void NewMethodName() { } }", tree.GetText().ToString());
        }

        [Fact]
        public void InsertNode()
        {
            string text = "class C { void M() { } }";
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var classNode = root.ChildNodes().First() as ClassDeclarationSyntax;
            
            var newMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("int"), SyntaxFactory.Identifier("NewMethod"))
                .WithBody(SyntaxFactory.Block());

            var newMembers = SyntaxFactory.List<MemberDeclarationSyntax>(classNode.Members.Concat(new[] { newMethod }));

            var newClass = SyntaxFactory.ClassDeclaration(
                classNode.AttributeLists,
                classNode.Modifiers,
                classNode.Keyword,
                classNode.Identifier,
                classNode.TypeParameterList,
                classNode.BaseList,
                classNode.ConstraintClauses,
                classNode.OpenBraceToken,
                newMembers,
                classNode.CloseBraceToken,
                classNode.SemicolonToken).NormalizeWhitespace(elasticTrivia: true);

            root = root.ReplaceNode(classNode, newClass);
            tree = tree.WithRootAndOptions(root, tree.Options);
            Assert.Equal(@"class C
{
    void M()
    {
    }

    int NewMethod()
    {
    }
}", tree.GetText().ToString());
        }

        [Fact]
        public void WalkTreeUsingSyntaxWalker()
        {
            string text = "class Class { void Method1() { } struct S { } void Method2() { } }";
            SyntaxNode node = SyntaxFactory.ParseCompilationUnit(text);
            FileContentsDumper visitor = new FileContentsDumper();
            visitor.Visit(node);
            Assert.Equal(@"class Class
  Method1
struct S
  Method2
", visitor.ToString());
        }

        [Fact]
        public void TransformTreeUsingSyntaxRewriter()
        {
            string text = "class C { void M() { } int field; }";
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxNode newRoot = new RemoveMethodsRewriter().Visit(tree.GetRoot());
            Assert.Equal("class C { int field; }", newRoot.ToFullString());
        }

        private class RemoveMethodsRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                return null;
            }
        }

        private class FileContentsDumper : CSharpSyntaxWalker
        {
            private readonly StringBuilder sb = new StringBuilder();

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                sb.AppendLine(node.Keyword.ValueText + " " + node.Identifier.ValueText);
                base.VisitClassDeclaration(node);
            }

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
            {
                sb.AppendLine(node.Keyword.ValueText + " " + node.Identifier.ValueText);
                base.VisitStructDeclaration(node);
            }

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
            {
                sb.AppendLine(node.Keyword.ValueText + " " + node.Identifier.ValueText);
                base.VisitInterfaceDeclaration(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                sb.AppendLine("  " + node.Identifier.ToString());
                base.VisitMethodDeclaration(node);
            }

            public override string ToString()
            {
                return sb.ToString();
            }
        }
    }
}
