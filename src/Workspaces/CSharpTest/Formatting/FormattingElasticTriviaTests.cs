// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingEngineElasticTriviaTests : CSharpFormattingTestBase
    {
        [Fact(Skip = "530167")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatElasticTrivia()
        {
            var expected = @"extern alias A1;

#line 99

[assembly: My]

class My : System.Attribute
{
}

class A
{
}

[My]
class B
{
}";
            var compilation = SyntaxFactory.CompilationUnit(
                externs: SyntaxFactory.SingletonList<ExternAliasDirectiveSyntax>(
                            SyntaxFactory.ExternAliasDirective("A1")),
                usings: default(SyntaxList<UsingDirectiveSyntax>),
                attributeLists: SyntaxFactory.SingletonList<AttributeListSyntax>(
                                SyntaxFactory.AttributeList(
                                    SyntaxFactory.Token(
                                        SyntaxFactory.TriviaList(
                                            SyntaxFactory.Trivia(
                                                SyntaxFactory.LineDirectiveTrivia(
                                                    SyntaxFactory.Literal("99", 99), false))),
                                        SyntaxKind.OpenBracketToken,
                                        SyntaxFactory.TriviaList()),
                                    SyntaxFactory.AttributeTargetSpecifier(
                                        SyntaxFactory.Identifier("assembly")),
                                    SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                        SyntaxFactory.Attribute(
                                            SyntaxFactory.ParseName("My"))),
                                    SyntaxFactory.Token(
                                        SyntaxKind.CloseBracketToken))),
                members: SyntaxFactory.List<MemberDeclarationSyntax>(
                new MemberDeclarationSyntax[]
                {
                    SyntaxFactory.ClassDeclaration(
                        default(SyntaxList<AttributeListSyntax>),
                        SyntaxFactory.TokenList(),
                        SyntaxFactory.Identifier("My"),
                        null,
                        SyntaxFactory.BaseList(
                            SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("System.Attribute")))),
                                default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                                default(SyntaxList<MemberDeclarationSyntax>)),
                    SyntaxFactory.ClassDeclaration("A"),
                    SyntaxFactory.ClassDeclaration(
                        attributeLists: SyntaxFactory.SingletonList<AttributeListSyntax>(
                            SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(
                                    SyntaxFactory.Attribute(
                                        SyntaxFactory.ParseName("My"))))),
                        modifiers: SyntaxFactory.TokenList(),
                        identifier: SyntaxFactory.Identifier("B"),
                        typeParameterList: null,
                        baseList: null,
                        constraintClauses: default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                        members: default(SyntaxList<MemberDeclarationSyntax>))
                }));

            Assert.NotNull(compilation);

            var newCompilation = Formatter.Format(compilation, new AdhocWorkspace());
            Assert.Equal(expected, newCompilation.ToFullString());
        }
    }
}
