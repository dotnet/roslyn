// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class FormattingEngineElasticTriviaTests : CSharpFormattingTestBase
    {
        [Fact(Skip = "530167")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatElasticTrivia()
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

            var newCompilation = await Formatter.FormatAsync(compilation, new AdhocWorkspace());
            Assert.Equal(expected, newCompilation.ToFullString());
        }

        [WorkItem(1947, "https://github.com/dotnet/roslyn/issues/1947")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task ElasticLineBreaksBetweenMembers()
        {
            var text = @"
public class C
{
    public string f1;

    // example comment
    public string f2;
}

public class SomeAttribute : System.Attribute { }
";

            var ws = new AdhocWorkspace();
            var generator = SyntaxGenerator.GetGenerator(ws, LanguageNames.CSharp);
            var root = SyntaxFactory.ParseCompilationUnit(text);
            var decl = generator.GetDeclaration(root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First(vd => vd.Identifier.Text == "f2"));
            var newDecl = generator.AddAttributes(decl, generator.Attribute("Some")).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(decl, newDecl);

            var expected = @"
public class C
{
    public string f1;

    // example comment
    [Some]
    public string f2;
}

public class SomeAttribute : System.Attribute { }
";

            var formatted = (await Formatter.FormatAsync(newRoot, ws)).ToFullString();
            Assert.Equal(expected, formatted);

            var elasticOnlyFormatted = (await Formatter.FormatAsync(newRoot, SyntaxAnnotation.ElasticAnnotation, ws)).ToFullString();
            Assert.Equal(expected, elasticOnlyFormatted);

            var annotationFormatted = (await Formatter.FormatAsync(newRoot, Formatter.Annotation, ws)).ToFullString();
            Assert.Equal(expected, annotationFormatted);
        }

        [WorkItem(408, "https://roslyn.codeplex.com/workitem/408")]
        [Fact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatElasticTriviaBetweenPropertiesWithoutAccessors()
        {
            var expected = @"class PropertyTest
{
    string MyProperty => ""42"";

    string MyProperty => ""42"";
}";
            var property = SyntaxFactory.PropertyDeclaration(
                attributeLists: default(SyntaxList<AttributeListSyntax>),
                modifiers: SyntaxFactory.TokenList(),
                type: SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(
                        SyntaxKind.StringKeyword)),
                explicitInterfaceSpecifier: null,
                identifier: SyntaxFactory.Identifier("MyProperty"),
                accessorList: null,
                expressionBody:
                    SyntaxFactory.ArrowExpressionClause(
                        SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal("42"))),
                initializer: null,
                semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var compilation = SyntaxFactory.CompilationUnit(
                externs: default(SyntaxList<ExternAliasDirectiveSyntax>),
                usings: default(SyntaxList<UsingDirectiveSyntax>),
                attributeLists: default(SyntaxList<AttributeListSyntax>),
                members: SyntaxFactory.List(
                new MemberDeclarationSyntax[]
                {
                    SyntaxFactory.ClassDeclaration(
                        attributeLists: default(SyntaxList<AttributeListSyntax>),
                        modifiers: SyntaxFactory.TokenList(),
                        identifier: SyntaxFactory.Identifier("PropertyTest"),
                        typeParameterList: null,
                        baseList: null,
                        constraintClauses: default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                        members: SyntaxFactory.List(
                            new MemberDeclarationSyntax[]
                            {
                                property,
                                property
                            }))
                }));

            Assert.NotNull(compilation);

            var newCompilation = await Formatter.FormatAsync(compilation, new AdhocWorkspace());
            Assert.Equal(expected, newCompilation.ToFullString());
        }
    }
}
