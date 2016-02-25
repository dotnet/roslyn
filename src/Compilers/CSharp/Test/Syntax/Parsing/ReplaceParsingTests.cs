// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ReplaceParsingTests
    {
        [Fact]
        public void TestClassReplace()
        {
            var root = SyntaxFactory.ParseCompilationUnit("abstract replace override class C { }");
            root.Errors().Verify();
            var type = (TypeDeclarationSyntax)root.Members[0];
            Assert.Equal(
                type.Modifiers.ToDeclarationModifiers(),
                DeclarationModifiers.Abstract | DeclarationModifiers.Override | DeclarationModifiers.Replace);
        }

        [Fact]
        public void TestMethodReplace()
        {
            var root = SyntaxFactory.ParseCompilationUnit("class C { abstract replace override void M(); }");
            root.Errors().Verify();
            var type = (TypeDeclarationSyntax)root.Members[0];
            Assert.Equal(
                type.Modifiers.ToDeclarationModifiers(),
                DeclarationModifiers.None);
            var method = (MethodDeclarationSyntax)type.Members[0];
            Assert.Equal(
                method.Modifiers.ToDeclarationModifiers(),
                DeclarationModifiers.Abstract | DeclarationModifiers.Override | DeclarationModifiers.Replace);
        }

        [Fact]
        public void OriginalExpression()
        {
            var expr = SyntaxFactory.ParseExpression("original");
            expr.Errors().Verify();
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            Assert.Equal(SyntaxKind.OriginalKeyword, ((IdentifierNameSyntax)expr).Identifier.ContextualKind());

            var opKind = SyntaxFacts.GetInstanceExpression(SyntaxKind.OriginalKeyword);
            Assert.Equal(SyntaxKind.None, opKind);
        }

        [Fact]
        public void OriginalInReplaceIndexer()
        {
            const string text =
@"class C
{
        replace object this[int index]
        {
            get { return original[index]; }
            set { original[index] = value; }
        }
}";
            var root = SyntaxFactory.ParseCompilationUnit(text);
            root.Errors().Verify();
            Assert.Equal(SyntaxKind.IdentifierName, root.Kind());
        }

        private void OriginalInMethod(string text, bool isContextualKeyword)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var root = tree.GetCompilationUnitRoot();
            root.Errors().Verify();
            Assert.Equal(SyntaxKind.IdentifierName, root.Kind());
        }
    }
}
