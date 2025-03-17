// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.QuickInfo;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.OnTheFlyDocs;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.OnTheFlyDocs)]
public sealed class OnTheFlyDocsUtilitiesTests
{
    [Fact]
    public async Task TestAdditionalContextNoContext()
    {
        var testCode = """
            class C
            {
                void AddMethod(int a, int b)
                {
                    return a + b;
                }
            }
            """;

        using var workspace = EditorTestWorkspace.CreateCSharp(testCode);
        var solution = workspace.CurrentSolution;
        var document = solution.Projects.First().Documents.First();

        var syntaxTree = await document.GetSyntaxTreeAsync();
        var semanticModel = await document.GetSemanticModelAsync();

        var methodDeclaration = syntaxTree!.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();

        var methodSymbol = semanticModel!.GetDeclaredSymbol(methodDeclaration);

        var result = OnTheFlyDocsUtilities.GetAdditionalOnTheFlyDocsContext(solution, methodSymbol!);
        Assert.True(result.All(item => item == null));
    }

    [Fact]
    public async Task TestAdditionalContextWithTypeParameters()
    {
        var testCode = """
            class C
            {
                int AddMethod(A a, int b)
                {
                    return a.x + b;
                }
            }

            class A
            {
                public int x;
            }
            """;

        using var workspace = EditorTestWorkspace.CreateCSharp(testCode);
        var solution = workspace.CurrentSolution;
        var document = solution.Projects.First().Documents.First();

        var syntaxTree = await document.GetSyntaxTreeAsync();
        var semanticModel = await document.GetSemanticModelAsync();

        var methodDeclaration = syntaxTree!.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .First();

        var methodSymbol = semanticModel!.GetDeclaredSymbol(methodDeclaration);

        var result = OnTheFlyDocsUtilities.GetAdditionalOnTheFlyDocsContext(solution, methodSymbol!);
        Assert.NotNull(result.First());
        Assert.Null(result.Last());
    }

    [Fact]
    public async Task TestAdditionalContextWithTypeArguments()
    {
        var testCode = """
            class C
            {
                void Method<T, U>(T t, U u) where T : class where U : struct
                {
                }

                void CallMethod()
                {
                    Method<CustomClass, CustomStruct>(new CustomClass(), new CustomStruct());
                }
            }

            class CustomClass
            {
                public string Name { get; set; }
            }

            struct CustomStruct
            {
                public int Value { get; set; }
            }
            """;

        using var workspace = EditorTestWorkspace.CreateCSharp(testCode);
        var solution = workspace.CurrentSolution;
        var document = solution.Projects.First().Documents.First();

        var syntaxTree = await document.GetSyntaxTreeAsync();
        var semanticModel = await document.GetSemanticModelAsync();

        var methodInvocation = syntaxTree!.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First();

        var methodSymbol = semanticModel!.GetSymbolInfo(methodInvocation).Symbol;

        var result = OnTheFlyDocsUtilities.GetAdditionalOnTheFlyDocsContext(solution, methodSymbol!);
        Assert.True(result.All(item => item is not null));
    }
}
