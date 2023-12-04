// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    [Trait(Traits.Feature, Traits.Features.Formatting)]
    public class FormattingEngineElasticTriviaTests : CSharpFormattingTestBase
    {
        [Fact(Skip = "530167")]
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
                usings: default,
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
                        default,
                        SyntaxFactory.TokenList(),
                        SyntaxFactory.Identifier("My"),
                        null,
                        SyntaxFactory.BaseList(
                            SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("System.Attribute")))),
                                default,
                                default),
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
                        constraintClauses: default,
                        members: default)
                }));

            Assert.NotNull(compilation);

            using var workspace = new AdhocWorkspace();
            var newCompilation = Formatter.Format(compilation, workspace.Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
            Assert.Equal(expected, newCompilation.ToFullString());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1947")]
        public void ElasticLineBreaksBetweenMembers()
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

            var workspace = new AdhocWorkspace();
            var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
            var root = SyntaxFactory.ParseCompilationUnit(text);
            var decl = generator.GetDeclaration(root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First(vd => vd.Identifier.Text == "f2"));
            var newDecl = generator.AddAttributes(decl, generator.Attribute("Some")).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(decl, newDecl);
            var options = CSharpSyntaxFormattingOptions.Default;

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

            var formatted = Formatter.Format(newRoot, workspace.Services.SolutionServices, options, CancellationToken.None).ToFullString();
            Assert.Equal(expected, formatted);

            var elasticOnlyFormatted = Formatter.Format(newRoot, SyntaxAnnotation.ElasticAnnotation, workspace.Services.SolutionServices, options, CancellationToken.None).ToFullString();
            Assert.Equal(expected, elasticOnlyFormatted);

            var annotationFormatted = Formatter.Format(newRoot, Formatter.Annotation, workspace.Services.SolutionServices, options, CancellationToken.None).ToFullString();
            Assert.Equal(expected, annotationFormatted);
        }

        [Fact, WorkItem("https://roslyn.codeplex.com/workitem/408")]
        public void FormatElasticTriviaBetweenPropertiesWithoutAccessors()
        {
            var expected = @"class PropertyTest
{
    string MyProperty => ""42"";

    string MyProperty => ""42"";
}";
            var property = SyntaxFactory.PropertyDeclaration(
                attributeLists: default,
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
                externs: default,
                usings: default,
                attributeLists: default,
                members: SyntaxFactory.List(
                new MemberDeclarationSyntax[]
                {
                    SyntaxFactory.ClassDeclaration(
                        attributeLists: default,
                        modifiers: SyntaxFactory.TokenList(),
                        identifier: SyntaxFactory.Identifier("PropertyTest"),
                        typeParameterList: null,
                        baseList: null,
                        constraintClauses: default,
                        members: SyntaxFactory.List(
                            new MemberDeclarationSyntax[]
                            {
                                property,
                                property
                            }))
                }));

            Assert.NotNull(compilation);

            using var workspace = new AdhocWorkspace();
            var newCompilation = Formatter.Format(compilation, workspace.Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
            Assert.Equal(expected, newCompilation.ToFullString());
        }
    }
}
