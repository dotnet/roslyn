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

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingEngineElasticTriviaTests : CSharpFormattingTestBase
{
    [Fact(Skip = "530167")]
    public void FormatElasticTrivia()
    {
        var compilation = CompilationUnit(
            externs: [ExternAliasDirective("A1")],
            usings: default,
            attributeLists: [AttributeList(
                                Token(
                                    [Trivia(
                                        LineDirectiveTrivia(
                                            Literal("99", 99), false))],
                                    SyntaxKind.OpenBracketToken,
                                    TriviaList()),
                                AttributeTargetSpecifier(
                                    Identifier("assembly")),
                                [Attribute(
                                    ParseName("My"))],
                                Token(
                                    SyntaxKind.CloseBracketToken))],
            members:
            [
                ClassDeclaration(
                    default,
                    modifiers: [],
                    Identifier("My"),
                    null,
                    BaseList([SimpleBaseType(ParseTypeName("System.Attribute"))]),
                    default,
                    default),
                ClassDeclaration("A"),
                ClassDeclaration(
                    attributeLists: [
                        AttributeList([
                                Attribute(
                                    ParseName("My"))])],
                    modifiers: [],
                    identifier: Identifier("B"),
                    typeParameterList: null,
                    baseList: null,
                    constraintClauses: default,
                    members: default)
            ]);

        Assert.NotNull(compilation);

        using var workspace = new AdhocWorkspace();
        var newCompilation = Formatter.Format(compilation, workspace.Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
        Assert.Equal("""
            extern alias A1;

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
            }
            """, newCompilation.ToFullString());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1947")]
    public void ElasticLineBreaksBetweenMembers()
    {
        var text = """
            public class C
            {
                public string f1;

                // example comment
                public string f2;
            }

            public class SomeAttribute : System.Attribute { }
            """;

        var workspace = new AdhocWorkspace();
        var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);
        var root = ParseCompilationUnit(text);
        var decl = generator.GetDeclaration(root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First(vd => vd.Identifier.Text == "f2"));
        var newDecl = generator.AddAttributes(decl, generator.Attribute("Some")).WithAdditionalAnnotations(Formatter.Annotation);
        var newRoot = root.ReplaceNode(decl, newDecl);
        var options = CSharpSyntaxFormattingOptions.Default;

        var expected = """
            public class C
            {
                public string f1;

                // example comment
                [Some]
                public string f2;
            }

            public class SomeAttribute : System.Attribute { }
            """;

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
        var property = PropertyDeclaration(
            attributeLists: default,
            modifiers: [],
            type: PredefinedType(
                Token(
                    SyntaxKind.StringKeyword)),
            explicitInterfaceSpecifier: null,
            identifier: Identifier("MyProperty"),
            accessorList: null,
            expressionBody:
                ArrowExpressionClause(
                    LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal("42"))),
            initializer: null,
            semicolonToken: SemicolonToken);

        var compilation = CompilationUnit(
            externs: default,
            usings: default,
            attributeLists: default,
            members: List(
            new MemberDeclarationSyntax[]
            {
                ClassDeclaration(
                    attributeLists: default,
                    modifiers: [],
                    identifier: Identifier("PropertyTest"),
                    typeParameterList: null,
                    baseList: null,
                    constraintClauses: default,
                    members: [property, property])
            }));

        Assert.NotNull(compilation);

        using var workspace = new AdhocWorkspace();
        var newCompilation = Formatter.Format(compilation, workspace.Services.SolutionServices, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
        Assert.Equal("""
            class PropertyTest
            {
                string MyProperty => "42";

                string MyProperty => "42";
            }
            """, newCompilation.ToFullString());
    }
}
