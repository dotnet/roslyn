// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ParsingErrorRecoveryTests : CSharpTestBase
    {
        private CompilationUnitSyntax ParseTree(string text, CSharpParseOptions options = null)
        {
            return SyntaxFactory.ParseCompilationUnit(text, options: options);
        }

        [Fact]
        public void TestGlobalAttributeGarbageAfterLocation()
        {
            var text = "[assembly: $";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeUsingAfterLocation()
        {
            var text = "[assembly: using n;";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_UsingAfterElements, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeExternAfterLocation()
        {
            var text = "[assembly: extern alias a;";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_ExternAfterElements, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeNamespaceAfterLocation()
        {
            var text = "[assembly: namespace n { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeClassAfterLocation()
        {
            var text = "[assembly: class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeAttributeAfterLocation()
        {
            var text = "[assembly: [assembly: attr]";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeEOFAfterLocation()
        {
            var text = "[assembly: ";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeGarbageAfterAttribute()
        {
            var text = "[assembly: a $";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeGarbageAfterParameterStart()
        {
            var text = "[assembly: a( $";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeGarbageAfterParameter()
        {
            var text = "[assembly: a(b $";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeMissingCommaBetweenParameters()
        {
            var text = "[assembly: a(b c)";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeWithGarbageBetweenParameters()
        {
            var text = "[assembly: a(b $ c)";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeWithGarbageBetweenAttributes()
        {
            var text = "[assembly: a $ b";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeWithUsingAfterParameterStart()
        {
            var text = "[assembly: a( using n;";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_UsingAfterElements, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeWithExternAfterParameterStart()
        {
            var text = "[assembly: a( extern alias n;";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(0, file.Members.Count);
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_ExternAfterElements, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGlobalAttributeWithNamespaceAfterParameterStart()
        {
            var text = "[assembly: a( namespace n { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGlobalAttributeWithClassAfterParameterStart()
        {
            var text = "[assembly: a( class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageBeforeNamespace()
        {
            var text = "$ namespace n { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterNamespace()
        {
            var text = "namespace n { } $";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void MultipleSubsequentMisplacedCharactersSingleError1()
        {
            var text = "namespace n { } ,,,,,,,,";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_EOFExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void MultipleSubsequentMisplacedCharactersSingleError2()
        {
            var text = ",,,, namespace n { } ,,,,";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_EOFExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_EOFExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageInsideNamespace()
        {
            var text = "namespace n { $ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestIncompleteGlobalMembers()
        {
            var text = @"
asas]
extern alias A;
asas
using System;
sadasdasd]

[assembly: foo]

class C
{
}


[a]fod;
[b";
            var file = this.ParseTree(text);
            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Externs.Count);
            Assert.Equal(1, file.Usings.Count);
            Assert.Equal(1, file.AttributeLists.Count);
            Assert.Equal(3, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.IncompleteMember, file.Members[1].Kind());
            Assert.Equal(SyntaxKind.IncompleteMember, file.Members[2].Kind());
        }

        [Fact]
        public void TestAttributeWithGarbageAfterStart()
        {
            var text = "[ $";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.IncompleteMember, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
        }

        [Fact]
        public void TestAttributeWithGarbageAfterName()
        {
            var text = "[a $";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.IncompleteMember, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestAttributeWithClassAfterBracket()
        {
            var text = "[ class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestAttributeWithClassAfterName()
        {
            var text = "[a class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestAttributeWithClassAfterParameterStart()
        {
            var text = "[a( class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestAttributeWithClassAfterParameter()
        {
            var text = "[a(b class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestAttributeWithClassAfterParameterAndComma()
        {
            var text = "[a(b, class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
        }

        [Fact]
        public void TestAttributeWithCommaAfterParameterStart()
        {
            var text = "[a(, class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[3].Code);
        }

        [Fact]
        public void TestAttributeWithCommasAfterParameterStart()
        {
            var text = "[a(,, class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[4].Code);
        }

        [Fact]
        public void TestAttributeWithMissingFirstParameter()
        {
            var text = "[a(, b class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
        }

        [Fact]
        public void TestNamespaceWithGarbage()
        {
            var text = "namespace n { $ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestNamespaceWithUnexpectedKeyword()
        {
            var text = "namespace n { int }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_NamespaceUnexpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestNamespaceWithUnexpectedBracing()
        {
            var text = "namespace n { { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_EOFExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGlobalNamespaceWithUnexpectedBracingAtEnd()
        {
            var text = "namespace n { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_EOFExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGlobalNamespaceWithUnexpectedBracingAtStart()
        {
            var text = "} namespace n { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_EOFExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGlobalNamespaceWithOpenBraceBeforeNamespace()
        {
            var text = "{ namespace n { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_EOFExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestPartialNamespace()
        {
            var text = "partial namespace n { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.NamespaceDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_BadModifiersOnNamespace, file.Errors()[0].Code);
        }

        [Fact]
        public void TestClassAfterStartOfBaseTypeList()
        {
            var text = "class c : class b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestClassAfterBaseType()
        {
            var text = "class c : t class b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestClassAfterBaseTypeAndComma()
        {
            var text = "class c : t, class b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestClassAfterBaseTypesWithMissingComma()
        {
            var text = "class c : x y class b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGarbageAfterStartOfBaseTypeList()
        {
            var text = "class c : $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterBaseType()
        {
            var text = "class c : t $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterBaseTypeAndComma()
        {
            var text = "class c : t, $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterBaseTypesWithMissingComma()
        {
            var text = "class c : x y $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestConstraintAfterStartOfBaseTypeList()
        {
            var text = "class c<t> : where t : b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestConstraintAfterBaseType()
        {
            var text = "class c<t> : x where t : b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestConstraintAfterBaseTypeComma()
        {
            var text = "class c<t> : x, where t : b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestConstraintAfterBaseTypes()
        {
            var text = "class c<t> : x, y where t : b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestConstraintAfterBaseTypesWithMissingComma()
        {
            var text = "class c<t> : x y where t : b { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterStartOfBaseTypeList()
        {
            var text = "class c<t> : { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterBaseType()
        {
            var text = "class c<t> : x { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestOpenBraceAfterBaseTypeComma()
        {
            var text = "class c<t> : x, { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterBaseTypes()
        {
            var text = "class c<t> : x, y { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestBaseTypesWithMissingComma()
        {
            var text = "class c<t> : x y { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterConstraintStart()
        {
            var text = "class c<t> where { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestOpenBraceAfterConstraintName()
        {
            var text = "class c<t> where t { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestOpenBraceAfterConstraintNameAndColon()
        {
            var text = "class c<t> where t : { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterConstraintNameAndTypeAndComma()
        {
            var text = "class c<t> where t : x, { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestConstraintAfterConstraintStart()
        {
            var text = "class c<t> where where t : a { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestConstraintAfterConstraintName()
        {
            var text = "class c<t> where t where t : a { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestConstraintAfterConstraintNameAndColon()
        {
            var text = "class c<t> where t : where t : a { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestConstraintAfterConstraintNameColonTypeAndComma()
        {
            var text = "class c<t> where t : a, where t : a { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterConstraintStart()
        {
            var text = "class c<t> where $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[3].Code);
        }

        [Fact]
        public void TestGarbageAfterConstraintName()
        {
            var text = "class c<t> where t $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGarbageAfterConstraintNameAndColon()
        {
            var text = "class c<t> where t : $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterConstraintNameColonAndType()
        {
            var text = "class c<t> where t : x $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterConstraintNameColonTypeAndComma()
        {
            var text = "class c<t> where t : x, $ { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterGenericClassNameStart()
        {
            var text = "class c<$> { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterGenericClassNameType()
        {
            var text = "class c<t $> { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterGenericClassNameTypeAndComma()
        {
            var text = "class c<t, $> { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestOpenBraceAfterGenericClassNameStart()
        {
            var text = "class c< { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestOpenBraceAfterGenericClassNameAndType()
        {
            var text = "class c<t { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestClassAfterGenericClassNameStart()
        {
            var text = "class c< class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestClassAfterGenericClassNameAndType()
        {
            var text = "class c<t class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestClassAfterGenericClassNameTypeAndComma()
        {
            var text = "class c<t, class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestBaseTypeAfterGenericClassNameStart()
        {
            var text = "class c< : x { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestBaseTypeAfterGenericClassNameAndType()
        {
            var text = "class c<t : x { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestBaseTypeAfterGenericClassNameTypeAndComma()
        {
            var text = "class c<t, : x { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestConstraintAfterGenericClassNameStart()
        {
            var text = "class c< where t : x { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestConstraintAfterGenericClassNameAndType()
        {
            var text = "class c<t where t : x { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestConstraintAfterGenericClassNameTypeAndComma()
        {
            var text = "class c<t, where t : x { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestFieldAfterFieldStart()
        {
            var text = "class c { int int y; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IncompleteMember, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidMemberDecl, file.Errors()[0].Code);
        }

        [Fact]
        public void TestFieldAfterFieldTypeAndName()
        {
            var text = "class c { int x int y; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestFieldAfterFieldTypeNameAndComma()
        {
            var text = "class c { int x, int y; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterFieldStart()
        {
            var text = "class c { int $ int y; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IncompleteMember, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidMemberDecl, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidMemberDecl, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGarbageAfterFieldTypeAndName()
        {
            var text = "class c { int x $ int y; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterFieldTypeNameAndComma()
        {
            var text = "class c { int x, $ int y; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestEndBraceAfterFieldStart()
        {
            var text = "class c { int }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IncompleteMember, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidMemberDecl, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEndBraceAfterFieldName()
        {
            var text = "class c { int x }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEndBraceAfterFieldNameAndComma()
        {
            var text = "class c { int x, }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestEndBraceAfterMethodParameterStart()
        {
            var text = "class c { int m( }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestEndBraceAfterMethodParameterType()
        {
            var text = "class c { int m(x }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestEndBraceAfterMethodParameterName()
        {
            var text = "class c { int m(x y}";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestEndBraceAfterMethodParameterTypeNameAndComma()
        {
            var text = "class c { int m(x y, }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestEndBraceAfterMethodParameters()
        {
            var text = "class c { int m() }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterMethodParameterStart()
        {
            var text = "class c { int m( $ ); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterMethodParameterType()
        {
            var text = "class c { int m( x $ ); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterMethodParameterTypeAndName()
        {
            var text = "class c { int m( x y $ ); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterMethodParameterTypeNameAndComma()
        {
            var text = "class c { int m( x y, $ ); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[2].Code);
        }

        [Fact]
        public void TestMethodAfterMethodParameterStart()
        {
            var text = "class c { int m( public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestMethodAfterMethodParameterType()
        {
            var text = "class c { int m(x public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestMethodAfterMethodParameterTypeAndName()
        {
            var text = "class c { int m(x y public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestMethodAfterMethodParameterTypeNameAndComma()
        {
            var text = "class c { int m(x y, public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestMethodAfterMethodParameterList()
        {
            var text = "class c { int m(x y) public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestMethodBodyAfterMethodParameterListStart()
        {
            var text = "class c { int m( { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterMethodParameterListStart()
        {
            var text = "class c { int m( ; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestConstructorBodyAfterConstructorParameterListStart()
        {
            var text = "class c { c( { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.ConstructorDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterDelegateParameterListStart()
        {
            var text = "delegate void d( ;";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            var agg = (DelegateDeclarationSyntax)file.Members[0];
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEndBraceAfterIndexerParameterStart()
        {
            var text = "class c { int this[ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IndexerNeedsParam, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestEndBraceAfterIndexerParameterType()
        {
            var text = "class c { int this[x }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestEndBraceAfterIndexerParameterName()
        {
            var text = "class c { int this[x y }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestEndBraceAfterIndexerParameterTypeNameAndComma()
        {
            var text = "class c { int this[x y, }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[4].Code);
        }

        [Fact]
        public void TestEndBraceAfterIndexerParameters()
        {
            var text = "class c { int this[x y] }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterIndexerParameterStart()
        {
            var text = "class c { int this[ $ ] { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IndexerNeedsParam, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterIndexerParameterType()
        {
            var text = "class c { int this[ x $ ] { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterIndexerParameterTypeAndName()
        {
            var text = "class c { int this[ x y $ ] { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterIndexerParameterTypeNameAndComma()
        {
            var text = "class c { int this[ x y, $ ] { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[2].Code);
        }

        [Fact]
        public void TestMethodAfterIndexerParameterStart()
        {
            var text = "class c { int this[ public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IndexerNeedsParam, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestMethodAfterIndexerParameterType()
        {
            var text = "class c { int this[x public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestMethodAfterIndexerParameterTypeAndName()
        {
            var text = "class c { int this[x y public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestMethodAfterIndexerParameterTypeNameAndComma()
        {
            var text = "class c { int this[x y, public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[4].Code);
        }

        [Fact]
        public void TestMethodAfterIndexerParameterList()
        {
            var text = "class c { int this[x y] public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IndexerDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateStart()
        {
            var text = "delegate";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[4].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateType()
        {
            var text = "delegate d";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateName()
        {
            var text = "delegate void d";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateParameterStart()
        {
            var text = "delegate void d(";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateParameterType()
        {
            var text = "delegate void d(t";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateParameterTypeName()
        {
            var text = "delegate void d(t n";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateParameterList()
        {
            var text = "delegate void d(t n)";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEOFAfterDelegateParameterTypeNameAndComma()
        {
            var text = "delegate void d(t n, ";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestClassAfterDelegateStart()
        {
            var text = "delegate class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[4].Code);
        }

        [Fact]
        public void TestClassAfterDelegateType()
        {
            var text = "delegate d class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestClassAfterDelegateName()
        {
            var text = "delegate void d class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestClassAfterDelegateParameterStart()
        {
            var text = "delegate void d( class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestClassAfterDelegateParameterType()
        {
            var text = "delegate void d(t class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestClassAfterDelegateParameterTypeName()
        {
            var text = "delegate void d(t n class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestClassAfterDelegateParameterList()
        {
            var text = "delegate void d(t n) class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestClassAfterDelegateParameterTypeNameAndComma()
        {
            var text = "delegate void d(t n, class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestGarbageAfterDelegateParameterStart()
        {
            var text = "delegate void d($);";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterDelegateParameterType()
        {
            var text = "delegate void d(t $);";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterDelegateParameterTypeAndName()
        {
            var text = "delegate void d(t n $);";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterDelegateParameterTypeNameAndComma()
        {
            var text = "delegate void d(t n, $);";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.DelegateDeclaration, file.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGarbageAfterEnumStart()
        {
            var text = "enum e { $ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterEnumName()
        {
            var text = "enum e { n $ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeEnumName()
        {
            var text = "enum e { $ n }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAferEnumNameAndComma()
        {
            var text = "enum e { n, $ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAferEnumNameCommaAndName()
        {
            var text = "enum e { n, n $ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBetweenEnumNames()
        {
            var text = "enum e { n, $ n }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBetweenEnumNamesWithMissingComma()
        {
            var text = "enum e { n $ n }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAferEnumNameAndEquals()
        {
            var text = "enum e { n = $ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestEOFAfterEnumStart()
        {
            var text = "enum e { ";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEOFAfterEnumName()
        {
            var text = "enum e { n ";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEOFAfterEnumNameAndComma()
        {
            var text = "enum e { n, ";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestClassAfterEnumStart()
        {
            var text = "enum e { class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestClassAfterEnumName()
        {
            var text = "enum e { n class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestClassAfterEnumNameAndComma()
        {
            var text = "enum e { n, class c { }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Members.Count);
            Assert.Equal(SyntaxKind.EnumDeclaration, file.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterFixedFieldRankStart()
        {
            var text = "class c { fixed int x[$]; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_ValueExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageBeforeFixedFieldRankSize()
        {
            var text = "class c { fixed int x[$ 10]; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterFixedFieldRankSize()
        {
            var text = "class c { fixed int x[10 $]; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal(ErrorCode.ERR_SyntaxError, (ErrorCode)file.Errors()[0].Code); //expected comma
            Assert.Equal(ErrorCode.ERR_UnexpectedCharacter, (ErrorCode)file.Errors()[1].Code); //didn't expect '$'
            Assert.Equal(ErrorCode.ERR_ValueExpected, (ErrorCode)file.Errors()[2].Code); //expected value after (missing) comma
        }

        [Fact]
        public void TestGarbageAfterFieldTypeRankStart()
        {
            var text = "class c { int[$] x; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterFieldTypeRankComma()
        {
            var text = "class c { int[,$] x; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeFieldTypeRankComma()
        {
            var text = "class c { int[$,] x; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEndBraceAfterFieldRankStart()
        {
            var text = "class c { int[ }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.IncompleteMember, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestEndBraceAfterFieldRankComma()
        {
            var text = "class c { int[, }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.IncompleteMember, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestMethodAfterFieldRankStart()
        {
            var text = "class c { int[ public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IncompleteMember, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestMethodAfterFieldRankComma()
        {
            var text = "class c { int[, public void m() { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.IncompleteMember, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestStatementAfterLocalDeclarationStart()
        {
            var text = "class c { void m() { int if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterLocalRankStart()
        {
            var text = "class c { void m() { int [ if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestStatementAfterLocalRankComma()
        {
            var text = "class c { void m() { int [, if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestStatementAfterLocalDeclarationWithMissingSemicolon()
        {
            var text = "class c { void m() { int a if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestStatementAfterLocalDeclarationWithCommaAndMissingSemicolon()
        {
            var text = "class c { void m() { int a, if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterLocalDeclarationEquals()
        {
            var text = "class c { void m() { int a = if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterLocalDeclarationArrayInitializerStart()
        {
            var text = "class c { void m() { int a = { if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterLocalDeclarationArrayInitializerExpression()
        {
            var text = "class c { void m() { int a = { e if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterLocalDeclarationArrayInitializerExpressionAndComma()
        {
            var text = "class c { void m() { int a = { e, if (x) y(); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterLocalDeclarationArrayInitializerStart()
        {
            var text = "class c { void m() { int a = { $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterLocalDeclarationArrayInitializerExpression()
        {
            var text = "class c { void m() { int a = { e $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeLocalDeclarationArrayInitializerExpression()
        {
            var text = "class c { void m() { int a = { $ e }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterLocalDeclarationArrayInitializerExpressionAndComma()
        {
            var text = "class c { void m() { int a = { e, $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterLocalDeclarationArrayInitializerExpressions()
        {
            var text = "class c { void m() { int a = { e, e $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBetweenLocalDeclarationArrayInitializerExpressions()
        {
            var text = "class c { void m() { int a = { e, $ e }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBetweenLocalDeclarationArrayInitializerExpressionsWithMissingComma()
        {
            var text = "class c { void m() { int a = { e $ e }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterMethodCallStart()
        {
            var text = "class c { void m() { m($); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterMethodArgument()
        {
            var text = "class c { void m() { m(a $); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeMethodArgument()
        {
            var text = "class c { void m() { m($ a); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeMethodArgumentAndComma()
        {
            var text = "class c { void m() { m(a, $); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemiColonAfterMethodCallStart()
        {
            var text = "class c { void m() { m(; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemiColonAfterMethodCallArgument()
        {
            var text = "class c { void m() { m(a; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemiColonAfterMethodCallArgumentAndComma()
        {
            var text = "class c { void m() { m(a,; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestClosingBraceAfterMethodCallArgumentAndCommaWithWhitespace()
        {
            var text = "class c { void m() { m(a,\t\t\n\t\t\t} }";
            var file = this.ParseTree(text);

            var md = (file.Members[0] as TypeDeclarationSyntax).Members[0] as MethodDeclarationSyntax;
            var ie = (md.Body.Statements[0] as ExpressionStatementSyntax).Expression as InvocationExpressionSyntax;

            // whitespace trivia is part of the following '}', not the invocation expression
            Assert.Equal("", ie.ArgumentList.CloseParenToken.ToFullString());
            Assert.Equal("\t\t\t} ", md.Body.CloseBraceToken.ToFullString());

            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestStatementAfterMethodCallStart()
        {
            var text = "class c { void m() { m( if(e) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterMethodCallArgument()
        {
            var text = "class c { void m() { m(a if(e) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterMethodCallArgumentAndComma()
        {
            var text = "class c { void m() { m(a, if(e) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestCloseBraceAfterMethodCallStart()
        {
            var text = "class c { void m() { m( } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestCloseBraceAfterMethodCallArgument()
        {
            var text = "class c { void m() { m(a } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestCloseBraceAfterMethodCallArgumentAndComma()
        {
            var text = "class c { void m() { m(a, } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.InvocationExpression, es.Expression.Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGarbageAfterIndexerStart()
        {
            var text = "class c { void m() { ++a[$]; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterIndexerArgument()
        {
            var text = "class c { void m() { ++a[e $]; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeIndexerArgument()
        {
            var text = "class c { void m() { ++a[$ e]; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeIndexerArgumentAndComma()
        {
            var text = "class c { void m() { ++a[e, $]; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemiColonAfterIndexerStart()
        {
            var text = "class c { void m() { ++a[; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemiColonAfterIndexerArgument()
        {
            var text = "class c { void m() { ++a[e; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemiColonAfterIndexerArgumentAndComma()
        {
            var text = "class c { void m() { ++a[e,; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterIndexerStart()
        {
            var text = "class c { void m() { ++a[ if(e) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterIndexerArgument()
        {
            var text = "class c { void m() { ++a[e if(e) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterIndexerArgumentAndComma()
        {
            var text = "class c { void m() { ++a[e, if(e) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.IfStatement, ms.Body.Statements[1].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestCloseBraceAfterIndexerStart()
        {
            var text = "class c { void m() { ++a[ } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestCloseBraceAfterIndexerArgument()
        {
            var text = "class c { void m() { ++a[e } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestCloseBraceAfterIndexerArgumentAndComma()
        {
            var text = "class c { void m() { ++a[e, } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ExpressionStatement, ms.Body.Statements[0].Kind());
            var es = (ExpressionStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(SyntaxKind.PreIncrementExpression, es.Expression.Kind());
            Assert.Equal(SyntaxKind.ElementAccessExpression, ((PrefixUnaryExpressionSyntax)es.Expression).Operand.Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestOpenBraceAfterFixedStatementStart()
        {
            var text = "class c { void m() { fixed(t v { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.FixedStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemiColonAfterFixedStatementStart()
        {
            var text = "class c { void m() { fixed(t v; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.FixedStatement, ms.Body.Statements[0].Kind());
            var diags = file.ErrorsAndWarnings();
            Assert.Equal(2, diags.Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, diags[0].Code);
            Assert.Equal((int)ErrorCode.WRN_PossibleMistakenNullStatement, diags[1].Code);
        }

        [Fact]
        public void TestSemiColonAfterFixedStatementType()
        {
            var text = "class c { void m() { fixed(t ) { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.FixedStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestCatchAfterTryBlockStart()
        {
            var text = "class c { void m() { try { catch { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestFinallyAfterTryBlockStart()
        {
            var text = "class c { void m() { try { finally { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestFinallyAfterCatchStart()
        {
            var text = "class c { void m() { try { } catch finally { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestCatchAfterCatchStart()
        {
            var text = "class c { void m() { try { } catch catch { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_TooManyCatches, file.Errors()[2].Code);
        }

        [Fact]
        public void TestFinallyAfterCatchParameterStart()
        {
            var text = "class c { void m() { try { } catch (t finally { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestCatchAfterCatchParameterStart()
        {
            var text = "class c { void m() { try { } catch (t catch { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestCloseBraceAfterCatchStart()
        {
            var text = "class c { void m() { try { } catch } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestCloseBraceAfterCatchParameterStart()
        {
            var text = "class c { void m() { try { } catch(t } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.TryStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemiColonAfterDoWhileExpressionIndexer()
        {
            // this shows that ';' is an exit condition for the expression
            var text = "class c { void m() { do { } while(e[; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.DoStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestCloseParenAfterDoWhileExpressionIndexerStart()
        {
            // this shows that ')' is an exit condition for the expression
            var text = "class c { void m() { do { } while(e[); } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.DoStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestCloseParenAfterForStatementInitializerStart()
        {
            // this shows that ';' is an exit condition for the initializer expression
            var text = "class c { void m() { for (a[;;) { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterForStatementInitializerStart()
        {
            // this shows that '{' is an exit condition for the initializer expression
            var text = "class c { void m() { for (a[ { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[4].Code);
        }

        [Fact]
        public void TestCloseBraceAfterForStatementInitializerStart()
        {
            // this shows that '}' is an exit condition for the initializer expression
            var text = "class c { void m() { for (a[ } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(7, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[4].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[5].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[6].Code);
        }

        [Fact]
        public void TestCloseParenAfterForStatementConditionStart()
        {
            var text = "class c { void m() { for (;a[;) { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterForStatementConditionStart()
        {
            var text = "class c { void m() { for (;a[ { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestCloseBraceAfterForStatementConditionStart()
        {
            var text = "class c { void m() { for (;a[ } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[4].Code);
        }

        [Fact]
        public void TestCloseParenAfterForStatementIncrementerStart()
        {
            var text = "class c { void m() { for (;;++a[) { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
        }

        [Fact]
        public void TestOpenBraceAfterForStatementIncrementerStart()
        {
            var text = "class c { void m() { for (;;++a[ { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestCloseBraceAfterForStatementIncrementerStart()
        {
            var text = "class c { void m() { for (;;++a[ } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.ForStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestCloseBraceAfterAnonymousTypeStart()
        {
            // empty anonymous type is perfectly legal
            var text = "class c { void m() { var x = new {}; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestSemicolonAfterAnonymousTypeStart()
        {
            var text = "class c { void m() { var x = new {; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.EqualsToken);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterAnonymousTypeMemberStart()
        {
            var text = "class c { void m() { var x = new {a; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterAnonymousTypeMemberEquals()
        {
            var text = "class c { void m() { var x = new {a =; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemicolonAfterAnonymousTypeMember()
        {
            var text = "class c { void m() { var x = new {a = b; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterAnonymousTypeMemberComma()
        {
            var text = "class c { void m() { var x = new {a = b, ; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestStatementAfterAnonymousTypeStart()
        {
            var text = "class c { void m() { var x = new { while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterAnonymousTypeMemberStart()
        {
            var text = "class c { void m() { var x = new { a while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterAnonymousTypeMemberEquals()
        {
            var text = "class c { void m() { var x = new { a = while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestStatementAfterAnonymousTypeMember()
        {
            var text = "class c { void m() { var x = new { a = b while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterAnonymousTypeMemberComma()
        {
            var text = "class c { void m() { var x = new { a = b, while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterAnonymousTypeStart()
        {
            var text = "class c { void m() { var x = new { $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeAnonymousTypeMemberStart()
        {
            var text = "class c { void m() { var x = new { $ a }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterAnonymousTypeMemberStart()
        {
            var text = "class c { void m() { var x = new { a $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterAnonymousTypeMemberEquals()
        {
            var text = "class c { void m() { var x = new { a = $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterAnonymousTypeMember()
        {
            var text = "class c { void m() { var x = new { a = b $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterAnonymousTypeMemberComma()
        {
            var text = "class c { void m() { var x = new { a = b, $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestCloseBraceAfterObjectInitializerStart()
        {
            // empty object initializer is perfectly legal
            var text = "class c { void m() { var x = new C {}; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestSemicolonAfterObjectInitializerStart()
        {
            var text = "class c { void m() { var x = new C {; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterObjectInitializerMemberStart()
        {
            var text = "class c { void m() { var x = new C { a; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterObjectInitializerMemberEquals()
        {
            var text = "class c { void m() { var x = new C { a =; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemicolonAfterObjectInitializerMember()
        {
            var text = "class c { void m() { var x = new C { a = b; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterObjectInitializerMemberComma()
        {
            var text = "class c { void m() { var x = new C { a = b, ; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterObjectInitializerStart()
        {
            var text = "class c { void m() { var x = new C { while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterObjectInitializerMemberStart()
        {
            var text = "class c { void m() { var x = new C { a while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterObjectInitializerMemberEquals()
        {
            var text = "class c { void m() { var x = new C { a = while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestStatementAfterObjectInitializerMember()
        {
            var text = "class c { void m() { var x = new C { a = b while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestStatementAfterObjectInitializerMemberComma()
        {
            var text = "class c { void m() { var x = new C { a = b, while (x) {} } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestGarbageAfterObjectInitializerStart()
        {
            var text = "class c { void m() { var x = new C { $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageBeforeObjectInitializerMemberStart()
        {
            var text = "class c { void m() { var x = new C { $ a }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterObjectInitializerMemberStart()
        {
            var text = "class c { void m() { var x = new C { a $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterObjectInitializerMemberEquals()
        {
            var text = "class c { void m() { var x = new C { a = $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestGarbageAfterObjectInitializerMember()
        {
            var text = "class c { void m() { var x = new C { a = b $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[0].Code);
        }

        [Fact]
        public void TestGarbageAfterObjectInitializerMemberComma()
        {
            var text = "class c { void m() { var x = new C { a = b, $ }; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_UnexpectedCharacter, file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemicolonAfterLambdaParameter()
        {
            var text = "class c { void m() { var x = (Y y, ; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[4].Code);
        }

        [Fact]
        public void TestSemicolonAfterUntypedLambdaParameter()
        {
            var text = "class c { void m() { var x = (y, ; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(1, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[3].Code);
        }

        [Fact]
        public void TestStatementAfterLambdaParameter()
        {
            var text = "class c { void m() { var x = (Y y, while (c) { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(6, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[4].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[5].Code);
        }

        [Fact]
        public void TestStatementAfterUntypedLambdaParameter()
        {
            var text = "class c { void m() { var x = (y, while (c) { } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var ms = (MethodDeclarationSyntax)agg.Members[0];
            Assert.NotNull(ms.Body);
            Assert.Equal(2, ms.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, ms.Body.Statements[0].Kind());
            Assert.Equal(SyntaxKind.WhileStatement, ms.Body.Statements[1].Kind());
            var ds = (LocalDeclarationStatementSyntax)ms.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotEqual(SyntaxKind.None, ds.Declaration.Variables[0].Initializer.EqualsToken.Kind());
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            Assert.Equal(5, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_CloseParenExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[3].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[4].Code);
        }

        [Fact]
        public void TestPropertyWithNoAccessors()
        {
            // this is syntactically valid (even though it will produce a binding error)
            var text = "class c { int p { } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.PropertyDeclaration, agg.Members[0].Kind());
            var pd = (PropertyDeclarationSyntax)agg.Members[0];
            Assert.NotNull(pd.AccessorList);
            Assert.NotNull(pd.AccessorList.OpenBraceToken);
            Assert.False(pd.AccessorList.OpenBraceToken.IsMissing);
            Assert.NotNull(pd.AccessorList.CloseBraceToken);
            Assert.False(pd.AccessorList.CloseBraceToken.IsMissing);
            Assert.Equal(0, pd.AccessorList.Accessors.Count);
            Assert.Equal(0, file.Errors().Length);
        }

        [Fact]
        public void TestMethodAfterPropertyStart()
        {
            // this is syntactically valid (even though it will produce a binding error)
            var text = "class c { int p { int M() {} }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.PropertyDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            var pd = (PropertyDeclarationSyntax)agg.Members[0];
            Assert.NotNull(pd.AccessorList);
            Assert.NotNull(pd.AccessorList.OpenBraceToken);
            Assert.False(pd.AccessorList.OpenBraceToken.IsMissing);
            Assert.NotNull(pd.AccessorList.CloseBraceToken);
            Assert.True(pd.AccessorList.CloseBraceToken.IsMissing);
            Assert.Equal(0, pd.AccessorList.Accessors.Count);
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestMethodAfterPropertyGet()
        {
            var text = "class c { int p { get int M() {} }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.PropertyDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[1].Kind());
            var pd = (PropertyDeclarationSyntax)agg.Members[0];
            Assert.NotNull(pd.AccessorList);
            Assert.NotNull(pd.AccessorList.OpenBraceToken);
            Assert.False(pd.AccessorList.OpenBraceToken.IsMissing);
            Assert.NotNull(pd.AccessorList.CloseBraceToken);
            Assert.True(pd.AccessorList.CloseBraceToken.IsMissing);
            Assert.Equal(1, pd.AccessorList.Accessors.Count);
            var acc = pd.AccessorList.Accessors[0];
            Assert.Equal(SyntaxKind.GetAccessorDeclaration, acc.Kind());
            Assert.NotNull(acc.Keyword);
            Assert.False(acc.Keyword.IsMissing);
            Assert.Equal(SyntaxKind.GetKeyword, acc.Keyword.Kind());
            Assert.Null(acc.Body);
            Assert.NotNull(acc.SemicolonToken);
            Assert.True(acc.SemicolonToken.IsMissing);

            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_SemiOrLBraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestClassAfterPropertyGetBrace()
        {
            var text = "class c { int p { get { class d {} }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.PropertyDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, agg.Members[1].Kind());
            var pd = (PropertyDeclarationSyntax)agg.Members[0];
            Assert.NotNull(pd.AccessorList);
            Assert.NotNull(pd.AccessorList.OpenBraceToken);
            Assert.False(pd.AccessorList.OpenBraceToken.IsMissing);
            Assert.NotNull(pd.AccessorList.CloseBraceToken);
            Assert.True(pd.AccessorList.CloseBraceToken.IsMissing);
            Assert.Equal(1, pd.AccessorList.Accessors.Count);
            var acc = pd.AccessorList.Accessors[0];
            Assert.Equal(SyntaxKind.GetAccessorDeclaration, acc.Kind());
            Assert.NotNull(acc.Keyword);
            Assert.False(acc.Keyword.IsMissing);
            Assert.Equal(SyntaxKind.GetKeyword, acc.Keyword.Kind());
            Assert.NotNull(acc.Body);
            Assert.NotNull(acc.Body.OpenBraceToken);
            Assert.False(acc.Body.OpenBraceToken.IsMissing);
            Assert.Equal(0, acc.Body.Statements.Count);
            Assert.NotNull(acc.Body.CloseBraceToken);
            Assert.True(acc.Body.CloseBraceToken.IsMissing);
            Assert.Equal(SyntaxKind.None, acc.SemicolonToken.Kind());

            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestModifiedMemberAfterPropertyGetBrace()
        {
            var text = "class c { int p { get { public class d {} }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.PropertyDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.ClassDeclaration, agg.Members[1].Kind());
            var pd = (PropertyDeclarationSyntax)agg.Members[0];
            Assert.NotNull(pd.AccessorList);
            Assert.NotNull(pd.AccessorList.OpenBraceToken);
            Assert.False(pd.AccessorList.OpenBraceToken.IsMissing);
            Assert.NotNull(pd.AccessorList.CloseBraceToken);
            Assert.True(pd.AccessorList.CloseBraceToken.IsMissing);
            Assert.Equal(1, pd.AccessorList.Accessors.Count);
            var acc = pd.AccessorList.Accessors[0];
            Assert.Equal(SyntaxKind.GetAccessorDeclaration, acc.Kind());
            Assert.NotNull(acc.Keyword);
            Assert.False(acc.Keyword.IsMissing);
            Assert.Equal(SyntaxKind.GetKeyword, acc.Keyword.Kind());
            Assert.NotNull(acc.Body);
            Assert.NotNull(acc.Body.OpenBraceToken);
            Assert.False(acc.Body.OpenBraceToken.IsMissing);
            Assert.Equal(0, acc.Body.Statements.Count);
            Assert.NotNull(acc.Body.CloseBraceToken);
            Assert.True(acc.Body.CloseBraceToken.IsMissing);
            Assert.Equal(SyntaxKind.None, acc.SemicolonToken.Kind());

            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestPropertyAccessorMissingOpenBrace()
        {
            var text = "class c { int p { get return 0; } } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);

            var classDecl = (TypeDeclarationSyntax)file.Members[0];
            var propertyDecl = (PropertyDeclarationSyntax)classDecl.Members[0];

            var accessorDecls = propertyDecl.AccessorList.Accessors;
            Assert.Equal(1, accessorDecls.Count);

            var getDecl = accessorDecls[0];
            Assert.Equal(SyntaxKind.GetKeyword, getDecl.Keyword.Kind());

            var getBodyDecl = getDecl.Body;
            Assert.NotNull(getBodyDecl);
            Assert.True(getBodyDecl.OpenBraceToken.IsMissing);

            var getBodyStmts = getBodyDecl.Statements;
            Assert.Equal(1, getBodyStmts.Count);
            Assert.Equal(SyntaxKind.ReturnKeyword, getBodyStmts[0].GetFirstToken().Kind());
            Assert.False(getBodyStmts[0].ContainsDiagnostics);

            Assert.Equal(1, file.Errors().Length);
            Assert.Equal(ErrorCode.ERR_SemiOrLBraceExpected, (ErrorCode)file.Errors()[0].Code);
        }

        [Fact]
        public void TestPropertyAccessorsWithoutBodiesOrSemicolons()
        {
            var text = "class c { int p { get set } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);

            var classDecl = (TypeDeclarationSyntax)file.Members[0];
            var propertyDecl = (PropertyDeclarationSyntax)classDecl.Members[0];

            var accessorDecls = propertyDecl.AccessorList.Accessors;
            Assert.Equal(2, accessorDecls.Count);

            var getDecl = accessorDecls[0];
            Assert.Equal(SyntaxKind.GetKeyword, getDecl.Keyword.Kind());
            Assert.Null(getDecl.Body);
            Assert.True(getDecl.SemicolonToken.IsMissing);

            var setDecl = accessorDecls[1];
            Assert.Equal(SyntaxKind.SetKeyword, setDecl.Keyword.Kind());
            Assert.Null(setDecl.Body);
            Assert.True(setDecl.SemicolonToken.IsMissing);

            Assert.Equal(2, file.Errors().Length);
            Assert.Equal(ErrorCode.ERR_SemiOrLBraceExpected, (ErrorCode)file.Errors()[0].Code);
            Assert.Equal(ErrorCode.ERR_SemiOrLBraceExpected, (ErrorCode)file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemicolonAfterOrderingStart()
        {
            var text = "class c { void m() { var q = from x in y orderby; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var md = (MethodDeclarationSyntax)agg.Members[0];

            Assert.NotNull(md.Body);
            Assert.NotNull(md.Body.OpenBraceToken);
            Assert.False(md.Body.OpenBraceToken.IsMissing);
            Assert.NotNull(md.Body.CloseBraceToken);
            Assert.False(md.Body.CloseBraceToken.IsMissing);
            Assert.Equal(1, md.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, md.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)md.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.QueryExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            var qx = (QueryExpressionSyntax)ds.Declaration.Variables[0].Initializer.Value;
            Assert.Equal(1, qx.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qx.FromClause.Kind());
            Assert.Equal(SyntaxKind.OrderByClause, qx.Body.Clauses[0].Kind());
            var oc = (OrderByClauseSyntax)qx.Body.Clauses[0];
            Assert.NotNull(oc.OrderByKeyword);
            Assert.False(oc.OrderByKeyword.IsMissing);
            Assert.Equal(1, oc.Orderings.Count);
            Assert.NotNull(oc.Orderings[0].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            var nm = (IdentifierNameSyntax)oc.Orderings[0].Expression;
            Assert.True(nm.IsMissing);

            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_ExpectedSelectOrGroup, file.Errors()[1].Code);
        }

        [Fact]
        public void TestSemicolonAfterOrderingExpression()
        {
            var text = "class c { void m() { var q = from x in y orderby e; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var md = (MethodDeclarationSyntax)agg.Members[0];

            Assert.NotNull(md.Body);
            Assert.NotNull(md.Body.OpenBraceToken);
            Assert.False(md.Body.OpenBraceToken.IsMissing);
            Assert.NotNull(md.Body.CloseBraceToken);
            Assert.False(md.Body.CloseBraceToken.IsMissing);
            Assert.Equal(1, md.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, md.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)md.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.QueryExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            var qx = (QueryExpressionSyntax)ds.Declaration.Variables[0].Initializer.Value;
            Assert.Equal(1, qx.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qx.FromClause.Kind());
            Assert.Equal(SyntaxKind.OrderByClause, qx.Body.Clauses[0].Kind());
            var oc = (OrderByClauseSyntax)qx.Body.Clauses[0];
            Assert.NotNull(oc.OrderByKeyword);
            Assert.False(oc.OrderByKeyword.IsMissing);
            Assert.Equal(1, oc.Orderings.Count);
            Assert.NotNull(oc.Orderings[0].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            var nm = (IdentifierNameSyntax)oc.Orderings[0].Expression;
            Assert.False(nm.IsMissing);

            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_ExpectedSelectOrGroup, file.Errors()[0].Code);
        }

        [Fact]
        public void TestSemicolonAfterOrderingExpressionAndComma()
        {
            var text = "class c { void m() { var q = from x in y orderby e, ; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(1, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            var md = (MethodDeclarationSyntax)agg.Members[0];

            Assert.NotNull(md.Body);
            Assert.NotNull(md.Body.OpenBraceToken);
            Assert.False(md.Body.OpenBraceToken.IsMissing);
            Assert.NotNull(md.Body.CloseBraceToken);
            Assert.False(md.Body.CloseBraceToken.IsMissing);
            Assert.Equal(1, md.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, md.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)md.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.QueryExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            var qx = (QueryExpressionSyntax)ds.Declaration.Variables[0].Initializer.Value;
            Assert.Equal(1, qx.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qx.FromClause.Kind());
            Assert.Equal(SyntaxKind.OrderByClause, qx.Body.Clauses[0].Kind());
            var oc = (OrderByClauseSyntax)qx.Body.Clauses[0];
            Assert.NotNull(oc.OrderByKeyword);
            Assert.False(oc.OrderByKeyword.IsMissing);
            Assert.Equal(2, oc.Orderings.Count);
            Assert.NotNull(oc.Orderings[0].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            var nm = (IdentifierNameSyntax)oc.Orderings[0].Expression;
            Assert.False(nm.IsMissing);
            Assert.NotNull(oc.Orderings[1].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            nm = (IdentifierNameSyntax)oc.Orderings[1].Expression;
            Assert.True(nm.IsMissing);

            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_ExpectedSelectOrGroup, file.Errors()[1].Code);
        }

        [Fact]
        public void TestMemberAfterOrderingStart()
        {
            var text = "class c { void m() { var q = from x in y orderby public int Foo; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            var md = (MethodDeclarationSyntax)agg.Members[0];

            Assert.NotNull(md.Body);
            Assert.NotNull(md.Body.OpenBraceToken);
            Assert.False(md.Body.OpenBraceToken.IsMissing);
            Assert.NotNull(md.Body.CloseBraceToken);
            Assert.True(md.Body.CloseBraceToken.IsMissing);
            Assert.Equal(1, md.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, md.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)md.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.QueryExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            var qx = (QueryExpressionSyntax)ds.Declaration.Variables[0].Initializer.Value;
            Assert.Equal(1, qx.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qx.FromClause.Kind());
            Assert.Equal(SyntaxKind.OrderByClause, qx.Body.Clauses[0].Kind());
            var oc = (OrderByClauseSyntax)qx.Body.Clauses[0];
            Assert.NotNull(oc.OrderByKeyword);
            Assert.False(oc.OrderByKeyword.IsMissing);
            Assert.Equal(1, oc.Orderings.Count);
            Assert.NotNull(oc.Orderings[0].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            var nm = (IdentifierNameSyntax)oc.Orderings[0].Expression;
            Assert.True(nm.IsMissing);

            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_ExpectedSelectOrGroup, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void TestMemberAfterOrderingExpression()
        {
            var text = "class c { void m() { var q = from x in y orderby e public int Foo; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            var md = (MethodDeclarationSyntax)agg.Members[0];

            Assert.NotNull(md.Body);
            Assert.NotNull(md.Body.OpenBraceToken);
            Assert.False(md.Body.OpenBraceToken.IsMissing);
            Assert.NotNull(md.Body.CloseBraceToken);
            Assert.True(md.Body.CloseBraceToken.IsMissing);
            Assert.Equal(1, md.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, md.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)md.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.QueryExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            var qx = (QueryExpressionSyntax)ds.Declaration.Variables[0].Initializer.Value;
            Assert.Equal(1, qx.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qx.FromClause.Kind());
            Assert.Equal(SyntaxKind.OrderByClause, qx.Body.Clauses[0].Kind());
            var oc = (OrderByClauseSyntax)qx.Body.Clauses[0];
            Assert.NotNull(oc.OrderByKeyword);
            Assert.False(oc.OrderByKeyword.IsMissing);
            Assert.Equal(1, oc.Orderings.Count);
            Assert.NotNull(oc.Orderings[0].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            var nm = (IdentifierNameSyntax)oc.Orderings[0].Expression;
            Assert.False(nm.IsMissing);

            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_ExpectedSelectOrGroup, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);
        }

        [Fact]
        public void TestMemberAfterOrderingExpressionAndComma()
        {
            var text = "class c { void m() { var q = from x in y orderby e, public int Foo; }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(2, agg.Members.Count);
            Assert.Equal(SyntaxKind.MethodDeclaration, agg.Members[0].Kind());
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[1].Kind());
            var md = (MethodDeclarationSyntax)agg.Members[0];

            Assert.NotNull(md.Body);
            Assert.NotNull(md.Body.OpenBraceToken);
            Assert.False(md.Body.OpenBraceToken.IsMissing);
            Assert.NotNull(md.Body.CloseBraceToken);
            Assert.True(md.Body.CloseBraceToken.IsMissing);
            Assert.Equal(1, md.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, md.Body.Statements[0].Kind());
            var ds = (LocalDeclarationStatementSyntax)md.Body.Statements[0];
            Assert.Equal(1, ds.Declaration.Variables.Count);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer);
            Assert.NotNull(ds.Declaration.Variables[0].Initializer.Value);
            Assert.Equal(SyntaxKind.QueryExpression, ds.Declaration.Variables[0].Initializer.Value.Kind());
            var qx = (QueryExpressionSyntax)ds.Declaration.Variables[0].Initializer.Value;
            Assert.Equal(1, qx.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qx.FromClause.Kind());
            Assert.Equal(SyntaxKind.OrderByClause, qx.Body.Clauses[0].Kind());
            var oc = (OrderByClauseSyntax)qx.Body.Clauses[0];
            Assert.NotNull(oc.OrderByKeyword);
            Assert.False(oc.OrderByKeyword.IsMissing);
            Assert.Equal(2, oc.Orderings.Count);
            Assert.NotNull(oc.Orderings[0].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            var nm = (IdentifierNameSyntax)oc.Orderings[0].Expression;
            Assert.False(nm.IsMissing);
            Assert.NotNull(oc.Orderings[1].Expression);
            Assert.Equal(SyntaxKind.IdentifierName, oc.Orderings[0].Expression.Kind());
            nm = (IdentifierNameSyntax)oc.Orderings[1].Expression;
            Assert.True(nm.IsMissing);

            Assert.Equal(4, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_ExpectedSelectOrGroup, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[3].Code);
        }

        [Fact]
        public void PartialInVariableDecl()
        {
            var text = "class C1 { void M1() { int x = 1, partial class y = 2; } }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());

            var item1 = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal("C1", item1.Identifier.ToString());
            Assert.False(item1.OpenBraceToken.IsMissing);
            Assert.Equal(2, item1.Members.Count);
            Assert.False(item1.CloseBraceToken.IsMissing);

            var subitem1 = (MethodDeclarationSyntax)item1.Members[0];
            Assert.Equal(SyntaxKind.MethodDeclaration, subitem1.Kind());
            Assert.NotNull(subitem1.Body);
            Assert.False(subitem1.Body.OpenBraceToken.IsMissing);
            Assert.True(subitem1.Body.CloseBraceToken.IsMissing);
            Assert.Equal(1, subitem1.Body.Statements.Count);
            Assert.Equal(SyntaxKind.LocalDeclarationStatement, subitem1.Body.Statements[0].Kind());
            var decl = (LocalDeclarationStatementSyntax)subitem1.Body.Statements[0];
            Assert.True(decl.SemicolonToken.IsMissing);
            Assert.Equal(2, decl.Declaration.Variables.Count);
            Assert.Equal("x", decl.Declaration.Variables[0].Identifier.ToString());
            Assert.True(decl.Declaration.Variables[1].Identifier.IsMissing);
            Assert.Equal(3, subitem1.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, subitem1.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, subitem1.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, subitem1.Errors()[2].Code);

            var subitem2 = (TypeDeclarationSyntax)item1.Members[1];
            Assert.Equal(SyntaxKind.ClassDeclaration, item1.Members[1].Kind());
            Assert.Equal("y", subitem2.Identifier.ToString());
            Assert.Equal(SyntaxKind.PartialKeyword, subitem2.Modifiers[0].ContextualKind());
            Assert.True(subitem2.OpenBraceToken.IsMissing);
            Assert.True(subitem2.CloseBraceToken.IsMissing);
            Assert.Equal(3, subitem2.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, subitem2.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, subitem2.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidMemberDecl, subitem2.Errors()[2].Code);
        }

        [WorkItem(905394, "DevDiv/Personal")]
        [Fact]
        public void TestThisKeywordInIncompleteLambdaArgumentList()
        {
            var text = @"public class Test
                         {
                             public void Foo()
                             {
                                 var x = ((x, this
                             }
                         }";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.True(file.ContainsDiagnostics);
        }

        [WorkItem(906986, "DevDiv/Personal")]
        [Fact]
        public void TestIncompleteAttribute()
        {
            var text = @"    [type: F";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.True(file.ContainsDiagnostics);
        }

        [WorkItem(908952, "DevDiv/Personal")]
        [Fact]
        public void TestNegAttributeOnTypeParameter()
        {
            var text = @"    
                            public class B
                            {
                                void M()
                                {
                                    I<[Test] int> I1=new I<[Test] int>();
                                }
                            } 
                        ";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.True(file.ContainsDiagnostics);
        }

        [WorkItem(918947, "DevDiv/Personal")]
        [Fact]
        public void TestAtKeywordAsLocalOrParameter()
        {
            var text = @"
class A
{
  public void M()
  {
    int @int = 0;
    if (@int == 1)
    {
      @int = 0;
    }
    MM(@int);
  }
  public void MM(int n) { }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.False(file.ContainsDiagnostics);
        }

        [WorkItem(918947, "DevDiv/Personal")]
        [Fact]
        public void TestAtKeywordAsTypeNames()
        {
            var text = @"namespace @namespace
{
    class C1 { }
    class @class : C1 { }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.False(file.ContainsDiagnostics);
        }

        [WorkItem(919418, "DevDiv/Personal")]
        [Fact]
        public void TestNegDefaultAsLambdaParameter()
        {
            var text = @"class C
{
    delegate T Func<T>();
    delegate T Func<A0, T>(A0 a0);
    delegate T Func<A0, A1, T>(A0 a0, A1 a1);
    delegate T Func<A0, A1, A2, A3, T>(A0 a0, A1 a1, A2 a2, A3 a3);

    static void X()
    {
        // Func<int,int> f1      = (int @in) => 1;              // ok: @Keyword as parameter name
        Func<int,int> f2      = (int where, int from) => 1;  // ok: contextual keyword as parameter name
        Func<int,int> f3      = (int default) => 1;          // err: Keyword as parameter name
    }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.True(file.ContainsDiagnostics);
        }

        [Fact]
        public void TestEmptyUsingDirective()
        {
            var text = @"using;";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);

            var usings = file.Usings;
            Assert.Equal(1, usings.Count);
            Assert.True(usings[0].Name.IsMissing);
        }

        [Fact]
        public void TestNumericLiteralInUsingDirective()
        {
            var text = @"using 10;";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);

            var usings = file.Usings;
            Assert.Equal(1, usings.Count);
            Assert.True(usings[0].Name.IsMissing);
        }

        [Fact]
        public void TestNamespaceDeclarationInUsingDirective()
        {
            var text = @"using namespace Foo";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpectedKW, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_LbraceExpected, file.Errors()[1].Code);
            Assert.Equal((int)ErrorCode.ERR_RbraceExpected, file.Errors()[2].Code);

            var usings = file.Usings;
            Assert.Equal(1, usings.Count);
            Assert.True(usings[0].Name.IsMissing);

            var members = file.Members;
            Assert.Equal(1, members.Count);

            var namespaceDeclaration = members[0];
            Assert.Equal(SyntaxKind.NamespaceDeclaration, namespaceDeclaration.Kind());
            Assert.False(((NamespaceDeclarationSyntax)namespaceDeclaration).Name.IsMissing);
        }

        [Fact]
        public void TestContextualKeywordAsFromVariable()
        {
            var text = @"
class C 
{ 
    int x = from equals in new[] { 1, 2, 3 } select 1;
}";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code);
        }

        [WorkItem(537210, "DevDiv")]
        [Fact]
        public void RegressException4UseValueInAccessor()
        {
            var text = @"public class MyClass
{
    public int MyProp
    {
        set { int value = 0; } // CS0136
    }
    D x;
    int this[int n]
    {
        get { return 0; }
        set { x = (value) => { value++; }; }  // CS0136
    }

    public delegate void D(int n);
    public event D MyEvent
    {
        add { object value = null; } // CS0136
        remove { }
    }
}";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            // Assert.True(file.ContainsDiagnostics); // CS0136 is not parser error
        }

        [WorkItem(931315, "DevDiv/Personal")]
        [Fact]
        public void RegressException4InvalidOperator()
        {
            var text = @"class A 
{
  public static int operator &&(A a) // CS1019
  {    return 0;   }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.True(file.ContainsDiagnostics);
        }

        [WorkItem(931316, "DevDiv/Personal")]
        [Fact]
        public void RegressNoError4NoOperator()
        {
            var text = @"class A 
{
  public static A operator (A a) // CS1019
  {    return a;   }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.True(file.ContainsDiagnostics);
        }

        [WorkItem(537214, "DevDiv")]
        [Fact]
        public void RegressWarning4UseContextKeyword()
        {
            var text = @"class TestClass
{
    int partial { get; set; }
    static int Main()
    {
        TestClass tc = new TestClass();
        tc.partial = 0;
        return 0;
    }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.False(file.ContainsDiagnostics);
        }

        [WorkItem(537150, "DevDiv")]
        [Fact]
        public void ParseStartOfAccessor()
        {
            var text = @"class Program
{
  int this[string s]
  {
    g
  }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_GetOrSetExpected, file.Errors()[0].Code);
        }

        [WorkItem(536050, "DevDiv")]
        [Fact]
        public void ParseMethodWithConstructorInitializer()
        {
            //someone has a typo in the name of their ctor - parse it as a method, but accept the initializer 
            var text = @"
class C
{
  CTypo() : base() {
     //body
  }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_MemberNeedsType, file.Errors()[0].Code); //for the missing 'void'
            Assert.Equal((int)ErrorCode.ERR_UnexpectedToken, file.Errors()[1].Code); //colon is unexpected

            // CONSIDER: Dev10 actually gives 'CS1002: ; expected', because it thinks you were trying to
            // specify a method without a body.  This is a little silly, since we already know the method
            // isn't abstract.  It might be reasonable to say that an open brace was expected though.

            var classDecl = file.ChildNodesAndTokens()[0];
            Assert.Equal(SyntaxKind.ClassDeclaration, classDecl.Kind());

            var methodDecl = classDecl.ChildNodesAndTokens()[3];
            Assert.Equal(SyntaxKind.MethodDeclaration, methodDecl.Kind()); //not ConstructorDeclaration
            Assert.True(methodDecl.ContainsDiagnostics);

            var methodBody = methodDecl.ChildNodesAndTokens()[3];
            Assert.Equal(SyntaxKind.Block, methodBody.Kind());
            Assert.False(methodBody.ContainsDiagnostics);
        }

        [WorkItem(537157, "DevDiv")]
        [Fact]
        public void MissingInternalNode()
        {
            var text = @"[1]";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());

            var incompleteMemberDecl = file.ChildNodesAndTokens()[0];
            Assert.Equal(incompleteMemberDecl.Kind(), SyntaxKind.IncompleteMember);
            Assert.False(incompleteMemberDecl.IsMissing);

            var attributeDecl = incompleteMemberDecl.ChildNodesAndTokens()[0];
            Assert.Equal(attributeDecl.Kind(), SyntaxKind.AttributeList);
            Assert.False(attributeDecl.IsMissing);

            var openBracketToken = attributeDecl.ChildNodesAndTokens()[0];
            Assert.Equal(openBracketToken.Kind(), SyntaxKind.OpenBracketToken);
            Assert.False(openBracketToken.IsMissing);

            var attribute = attributeDecl.ChildNodesAndTokens()[1];
            Assert.Equal(attribute.Kind(), SyntaxKind.Attribute);
            Assert.True(attribute.IsMissing);

            var identifierName = attribute.ChildNodesAndTokens()[0];
            Assert.Equal(identifierName.Kind(), SyntaxKind.IdentifierName);
            Assert.True(identifierName.IsMissing);

            var identifierToken = identifierName.ChildNodesAndTokens()[0];
            Assert.Equal(identifierToken.Kind(), SyntaxKind.IdentifierToken);
            Assert.True(identifierToken.IsMissing);
        }

        [WorkItem(538469, "DevDiv")]
        [Fact]
        public void FromKeyword()
        {
            var text = @"
using System.Collections.Generic;
using System.Linq;
public class QueryExpressionTest
{
    public static int Main()
    {
        int[] expr1 = new int[] { 1, 2, 3, };
        IEnumerable<int> query01 = from value in expr1 select value;
        IEnumerable<int> query02 = from yield in expr1 select yield;
        IEnumerable<int> query03 = from select in expr1 select select;
        return 0;
    }
}";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());

            Assert.Equal(3, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_IdentifierExpected, file.Errors()[0].Code); //expecting item name - found "select" keyword
            Assert.Equal((int)ErrorCode.ERR_InvalidExprTerm, file.Errors()[1].Code); //expecting expression - found "select" keyword
            Assert.Equal((int)ErrorCode.ERR_SemicolonExpected, file.Errors()[2].Code); //we inserted a missing semicolon in a place we didn't expect
        }

        [WorkItem(538971, "DevDiv")]
        [Fact]
        public void UnclosedGenericInExplicitInterfaceName()
        {
            var text = @"
interface I<T>
{
    void Foo();
}
 
class C : I<int>
{
    void I<.Foo() { }
}
";
            var file = this.ParseTree(text);

            Assert.Equal(text, file.ToFullString());

            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code); //expecting a type (argument)
            Assert.Equal((int)ErrorCode.ERR_SyntaxError, file.Errors()[1].Code); //expecting close angle bracket
        }

        [WorkItem(540788, "DevDiv")]
        [Fact]
        public void IncompleteForEachStatement()
        {
            var text = @"
public class Test
{
    public static void Main(string[] args)
    {
        foreach";

            var srcTree = this.ParseTree(text);

            Assert.Equal(text, srcTree.ToFullString());
            Assert.Equal("foreach", srcTree.GetLastToken().ToString());

            // Get the Foreach Node
            var foreachNode = srcTree.GetLastToken().Parent;

            // Verify 3 empty nodes are created by the parser for error recovery.
            Assert.Equal(3, foreachNode.ChildNodes().ToList().Count);
        }

        [WorkItem(542236, "DevDiv")]
        [ClrOnlyFact]
        public void InsertOpenBraceBeforeCodes()
        {
            var text = @"{
        this.I = i;
    };
}";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            Assert.Equal("{\r\n", syntaxTree.GetCompilationUnitRoot().GetLeadingTrivia().Node.ToFullString());

            // The issue (9391) was exhibited while enumerating the diagnostics
            Assert.True(syntaxTree.GetDiagnostics().Select(d => ((IFormattable)d).ToString(null, EnsureEnglishUICulture.PreferredOrNull)).SequenceEqual(new[]
            {
                "(1,2): error CS1031: Type expected",
                "(1,1): error CS1022: Type or namespace definition, or end-of-file expected",
                "(2,13): error CS1003: Syntax error, '[' expected",
                "(2,13): error CS1001: Identifier expected",
                "(2,16): error CS1001: Identifier expected",
                "(2,19): error CS1003: Syntax error, ',' expected",
                "(2,20): error CS1003: Syntax error, ']' expected",
                "(2,20): error CS1514: { expected",
                "(3,6): error CS1597: Semicolon after method or accessor block is not valid",
                "(4,1): error CS1022: Type or namespace definition, or end-of-file expected",
            }));
        }

        [WorkItem(542352, "DevDiv")]
        [Fact]
        public void IncompleteTopLevelOperator()
        {
            var text = @"
fg implicit//
class C { }
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            // 9553: Several of the locations were incorrect and one was negative
            Assert.True(syntaxTree.GetDiagnostics().Select(d => ((IFormattable)d).ToString(null, EnsureEnglishUICulture.PreferredOrNull)).SequenceEqual(new[]
            {
                // Error on the return type, because in C# syntax it goes after the operator and implicit/explicit keywords
                "(2,1): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead",
                // Error on "implicit" because there should be an operator keyword
                "(2,4): error CS1003: Syntax error, 'operator' expected",
                // Error on "implicit" because there should be an operator symbol
                "(2,4): error CS1037: Overloadable operator expected",
                // Missing parameter list and body
                "(2,12): error CS1003: Syntax error, '(' expected",
                "(2,12): error CS1026: ) expected",
                "(2,12): error CS1002: ; expected",
            }));
        }

        [WorkItem(545647, "DevDiv")]
        [Fact]
        public void IncompleteVariableDeclarationAboveDotMemberAccess()
        {
            var text = @"
class C
{
    void Main()
    {
        C
        Console.WriteLine();
    }
}
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            Assert.True(syntaxTree.GetDiagnostics().Select(d => ((IFormattable)d).ToString(null, EnsureEnglishUICulture.PreferredOrNull)).SequenceEqual(new[]
            {
                "(6,10): error CS1001: Identifier expected",
                "(6,10): error CS1002: ; expected",
            }));
        }

        [WorkItem(545647, "DevDiv")]
        [Fact]
        public void IncompleteVariableDeclarationAbovePointerMemberAccess()
        {
            var text = @"
class C
{
    void Main()
    {
        C
        Console->WriteLine();
    }
}
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            Assert.True(syntaxTree.GetDiagnostics().Select(d => ((IFormattable)d).ToString(null, EnsureEnglishUICulture.PreferredOrNull)).SequenceEqual(new[]
            {
                "(6,10): error CS1001: Identifier expected",
                "(6,10): error CS1002: ; expected",
            }));
        }

        [WorkItem(545647, "DevDiv")]
        [Fact]
        public void IncompleteVariableDeclarationAboveBinaryExpression()
        {
            var text = @"
class C
{
    void Main()
    {
        C
        A + B;
    }
}
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            Assert.True(syntaxTree.GetDiagnostics().Select(d => ((IFormattable)d).ToString(null, EnsureEnglishUICulture.PreferredOrNull)).SequenceEqual(new[]
            {
                "(6,10): error CS1001: Identifier expected",
                "(6,10): error CS1002: ; expected",
            }));
        }

        [WorkItem(545647, "DevDiv")]
        [Fact]
        public void IncompleteVariableDeclarationAboveMemberAccess_MultiLine()
        {
            var text = @"
class C
{
    void Main()
    {
        C

        Console.WriteLine();
    }
}
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            Assert.True(syntaxTree.GetDiagnostics().Select(d => ((IFormattable)d).ToString(null, EnsureEnglishUICulture.PreferredOrNull)).SequenceEqual(new[]
            {
                "(6,10): error CS1001: Identifier expected",
                "(6,10): error CS1002: ; expected",
            }));
        }

        [WorkItem(545647, "DevDiv")]
        [Fact]
        public void IncompleteVariableDeclarationBeforeMemberAccessOnSameLine()
        {
            var text = @"
class C
{
    void Main()
    {
        C Console.WriteLine();
    }
}
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            Assert.True(syntaxTree.GetDiagnostics().Select(d => ((IFormattable)d).ToString(null, EnsureEnglishUICulture.PreferredOrNull)).SequenceEqual(new[]
            {
                "(6,18): error CS1003: Syntax error, ',' expected",
                "(6,19): error CS1002: ; expected",
            }));
        }

        [WorkItem(545647, "DevDiv")]
        [Fact]
        public void EqualsIsNotAmbiguous()
        {
            var text = @"
class C
{
    void Main()
    {
        C
        A = B;
    }
}
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            Assert.Empty(syntaxTree.GetDiagnostics());
        }

        [WorkItem(547120, "DevDiv")]
        [Fact]
        public void ColonColonInExplicitInterfaceMember()
        {
            var text = @"
_ _::this
";

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text);
            Assert.Equal(text, syntaxTree.GetCompilationUnitRoot().ToFullString());

            syntaxTree.GetDiagnostics().Verify(
                // (2,4): error CS1003: Syntax error, '.' expected
                // _ _::this
                Diagnostic(ErrorCode.ERR_SyntaxError, "::").WithArguments(".", "::"),
                // (3,1): error CS1551: Indexers must have at least one parameter
                // 
                Diagnostic(ErrorCode.ERR_IndexerNeedsParam, ""),
                // (2,10): error CS1003: Syntax error, '[' expected
                // _ _::this
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("[", ""),
                // (2,10): error CS1003: Syntax error, ']' expected
                // _ _::this
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("]", ""),
                // (2,10): error CS1514: { expected
                // _ _::this
                Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
                // (2,10): error CS1513: } expected
                // _ _::this
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [WorkItem(649806, "DevDiv")]
        [Fact]
        public void Repro649806()
        {
            var source = "a b:: /**/\r\n";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var diags = tree.GetDiagnostics();
            diags.ToArray();
            Assert.Equal(1, diags.Count(d => d.Code == (int)ErrorCode.ERR_AliasQualAsExpression));
        }

        [WorkItem(674564, "DevDiv")]
        [Fact]
        public void Repro674564()
        {
            var source = @"
class C
{
    int P { set . } }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var diags = tree.GetDiagnostics();
            diags.ToArray();
            diags.Verify(
                // We see this diagnostic because the accessor has no open brace.

                // (4,17): error CS1043: { or ; expected
                //     int P { set . } }
                Diagnostic(ErrorCode.ERR_SemiOrLBraceExpected, "."),

                // We see this diagnostic because we're trying to skip bad tokens in the block and 
                // the "expected" token (i.e. the one we report when we see something that's not a
                // statement) is close brace.
                // CONSIDER: This diagnostic isn't great.

                // (4,17): error CS1513: } expected
                //     int P { set . } }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "."));
        }

        [WorkItem(680733, "DevDiv")]
        [Fact]
        public void Repro680733a()
        {
            var source = @"
class Test
{
    public async Task<in{> Bar()
    {
        return 1;
    }
}
";
            AssertEqualRoundtrip(source);
        }

        [WorkItem(680733, "DevDiv")]
        [Fact]
        public void Repro680733b()
        {
            var source = @"
using System;

class Test
{
    public async Task<[Obsolete]in{> Bar()
    {
        return 1;
    }
}
";
            AssertEqualRoundtrip(source);
        }

        [WorkItem(680739, "DevDiv")]
        [Fact]
        public void Repro680739()
        {
            var source = @"a b<c..<using.d";
            AssertEqualRoundtrip(source);
        }

        [WorkItem(675600, "DevDiv")]
        [Fact]
        public void TestBracesToOperatorDoubleGreaterThan()
        {
            AssertEqualRoundtrip(
@"/// <see cref=""operator}}""/>
class C {}");

            AssertEqualRoundtrip(
@"/// <see cref=""operator{{""/>
class C {}");

            AssertEqualRoundtrip(
@"/// <see cref=""operator}=""/>
class C {}");

            AssertEqualRoundtrip(
@"/// <see cref=""operator}}=""/>
class C {}");
        }

        private void AssertEqualRoundtrip(string source)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var toString = tree.GetRoot().ToFullString();
            Assert.Equal(source, toString);
        }

        [WorkItem(684816, "DevDiv")]
        [Fact]
        public void GenericPropertyWithMissingIdentifier()
        {
            var source = @"
class C : I
{
    int I./*missing*/< {
";

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var toString = tree.GetRoot().ToFullString();
            Assert.Equal(source, toString);
            tree.GetDiagnostics().Verify(
                // (4,22): error CS1001: Identifier expected
                //     int I./*missing*/< {
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<"),
                // (4,22): error CS7002: Unexpected use of a generic name
                //     int I./*missing*/< {
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "<"),
                // (4,24): error CS1003: Syntax error, '>' expected
                //     int I./*missing*/< {
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(">", "{"),
                // (4,25): error CS1513: } expected
                //     int I./*missing*/< {
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (4,25): error CS1513: } expected
                //     int I./*missing*/< {
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [WorkItem(684816, "DevDiv")]
        [Fact]
        public void GenericEventWithMissingIdentifier()
        {
            var source = @"
class C : I
{
    event D I./*missing*/< {
";

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var toString = tree.GetRoot().ToFullString();
            Assert.Equal(source, toString);
            tree.GetDiagnostics().Verify(
                // (4,26): error CS1001: Identifier expected
                //     event D I./*missing*/< {
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<"),
                // (4,26): error CS1001: Identifier expected
                //     event D I./*missing*/< {
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "<"),
                // (4,28): error CS1003: Syntax error, '>' expected
                //     event D I./*missing*/< {
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(">", "{"),
                // (4,26): error CS7002: Unexpected use of a generic name
                //     event D I./*missing*/< {
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "<"),
                // (4,29): error CS1513: } expected
                //     event D I./*missing*/< {
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (4,29): error CS1513: } expected
                //     event D I./*missing*/< {
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [WorkItem(684816, "DevDiv")]
        [Fact]
        public void ExplicitImplementationEventWithColonColon()
        {
            var source = @"
class C : I
{
    event D I::
";

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var toString = tree.GetRoot().ToFullString();
            Assert.Equal(source, toString);
            tree.GetDiagnostics().Verify(
                // (4,14): error CS0071: An explicit interface implementation of an event must use event accessor syntax
                //     event D I::
                Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, "::"),
                // (4,14): error CS0687: The namespace alias qualifier '::' always resolves to a type or namespace so is illegal here. Consider using '.' instead.
                //     event D I::
                Diagnostic(ErrorCode.ERR_AliasQualAsExpression, "::"),
                // (4,16): error CS1513: } expected
                //     event D I::
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [WorkItem(684816, "DevDiv")]
        [Fact]
        public void EventNamedThis()
        {
            var source = @"
class C
{
    event System.Action this
";

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var toString = tree.GetRoot().ToFullString();
            Assert.Equal(source, toString);
            tree.GetDiagnostics().Verify(
                // (4,25): error CS1001: Identifier expected
                //     event System.Action this
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "this"),
                // (4,29): error CS1514: { expected
                //     event System.Action this
                Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
                // (4,29): error CS1513: } expected
                //     event System.Action this
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
                // (4,29): error CS1513: } expected
                //     event System.Action this
                Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [WorkItem(697022, "DevDiv")]
        [Fact]
        public void GenericEnumWithMissingIdentifiers()
        {
            var source = @"enum
<//aaaa
enum
";

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var toString = tree.GetRoot().ToFullString();
            Assert.Equal(source, toString);
            tree.GetDiagnostics().ToArray();
        }

        [WorkItem(703809, "DevDiv")]
        [Fact]
        public void ReplaceOmittedArrayRankWithMissingIdentifier()
        {
            var source = @"fixed a,b {//aaaa
static
";

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var toString = tree.GetRoot().ToFullString();
            Assert.Equal(source, toString);
            tree.GetDiagnostics().ToArray();
        }

        [WorkItem(716245, "DevDiv")]
        [Fact]
        public void ManySkippedTokens()
        {
            const int numTokens = 500000; // Prohibitively slow without fix.
            var source = new string(',', numTokens);
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var eofToken = ((CompilationUnitSyntax)tree.GetRoot()).EndOfFileToken;
            Assert.Equal(numTokens, eofToken.FullWidth);
            Assert.Equal(numTokens, eofToken.LeadingTrivia.Count); // Confirm that we built a list.
        }


        [WorkItem(947819, "DevDiv")]
        [ClrOnlyFact]
        public void MissingOpenBraceForClass()
        {
            var source = @"namespace n
{
    class c
}
";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();

            Assert.Equal(source, root.ToFullString());
            var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            Assert.Equal(new Text.TextSpan(20, 9), classDecl.Span);
            Assert.Equal(new Text.TextSpan(16, 13), classDecl.FullSpan);
        }

        [WorkItem(947819, "DevDiv")]
        [ClrOnlyFact]
        public void MissingOpenBraceForStruct()
        {
            var source = @"namespace n
{
    struct c : I
}
";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();

            Assert.Equal(source, root.ToFullString());
            var structDecl = root.DescendantNodes().OfType<StructDeclarationSyntax>().Single();
            Assert.Equal(new Text.TextSpan(20, 14), structDecl.Span);
            Assert.Equal(new Text.TextSpan(16, 18), structDecl.FullSpan);
        }

        [WorkItem(947819, "DevDiv")]
        [ClrOnlyFact]
        public void MissingNameForStruct()
        {
            var source = @"namespace n
{
    struct : I
    {
    }
}
";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();

            Assert.Equal(source, root.ToFullString());
            var structDecl = root.DescendantNodes().OfType<StructDeclarationSyntax>().Single();
            Assert.Equal(new Text.TextSpan(20, 24), structDecl.Span);
            Assert.Equal(new Text.TextSpan(16, 30), structDecl.FullSpan);
        }

        [WorkItem(947819, "DevDiv")]
        [ClrOnlyFact]
        public void MissingNameForClass()
        {
            var source = @"namespace n
{
    class
    {
    }
}
";
            var root = SyntaxFactory.ParseSyntaxTree(source).GetRoot();

            Assert.Equal(source, root.ToFullString());
            var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
            Assert.Equal(new Text.TextSpan(20, 19), classDecl.Span);
            Assert.Equal(new Text.TextSpan(16, 25), classDecl.FullSpan);
        }

        [Fact]
        public void TestInvalidTypeArgInGenericExpression()
        {
            var text = "class c { Type t = typeof(Action<0>); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestInvalidTypeArgWithKeywordInGenericExpression()
        {
            var text = "class c { Type t = typeof(Action<static>); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestInvalidTypeArgInGenericExpressionMultipleTypes()
        {
            var text = "class c { Type t = typeof(Func<0,1>); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(2, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[1].Code);
        }

        [Fact]
        public void TestInvalidTypeArgInGenericExpressionMultipleTypesPartiallyValid()
        {
            var text = "class c { Type t = typeof(Func<0,bool>); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }

        [Fact]
        public void TestInvalidTypeArgInNewGenericExpression()
        {
            var text = "class c { var s = new Action<0>(); }";
            var file = this.ParseTree(text);

            Assert.NotNull(file);
            Assert.Equal(text, file.ToFullString());
            Assert.Equal(1, file.Members.Count);
            Assert.Equal(SyntaxKind.ClassDeclaration, file.Members[0].Kind());
            var agg = (TypeDeclarationSyntax)file.Members[0];
            Assert.Equal(SyntaxKind.FieldDeclaration, agg.Members[0].Kind());
            Assert.Equal(1, file.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_TypeExpected, file.Errors()[0].Code);
        }
    }
}
