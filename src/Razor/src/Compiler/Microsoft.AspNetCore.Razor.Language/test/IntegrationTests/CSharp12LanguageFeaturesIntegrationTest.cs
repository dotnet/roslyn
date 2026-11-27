// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Interceptors (experimental feature): requires dedicated feature switches and source-location plumbing outside a plain Razor document.

public sealed class CSharp12LanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/ref-readonly-parameters.md")]
    public void RefReadonlyParameters()
    {
        var generated = CompileToCSharp("""
            @code {
                public static int Sum(ref readonly int value)
                    => value;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/collection-expressions.md")]
    public void CollectionExpressions()
    {
        var generated = CompileToCSharp("""
            @{
                int[] values = [1, 2, 3];
                _ = values.Length;
            }

            @((int[])[1, 2, 3])
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/inline-arrays.md")]
    public void InlineArrays()
    {
        var generated = CompileToCSharp("""
            @code {
                [System.Runtime.CompilerServices.InlineArray(4)]
                public struct Buffer
                {
                    private int _element0;
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/4037")]
    public void NameofAccessingInstanceMembers()
    {
        var generated = CompileToCSharp("""
            @code {
                public string Capture()
                    => nameof(ToString);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/4284")]
    public void UsingAliasesForAnyType()
    {
        var generated = CompileToCSharp("""
            @using NumberPair = (int Left, int Right)
            
            @{
                NumberPair pair = (1, 2);
                _ = pair.Left + pair.Right;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/primary-constructors.md")]
    public void PrimaryConstructors()
    {
        var generated = CompileToCSharp("""
            @code {
                public class Person(string name)
                {
                    public string Name => name;
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/lambda-method-group-defaults.md")]
    public void LambdaOptionalParameters()
    {
        var generated = CompileToCSharp("""
            @{
                var formatter = (int value = 1) => value + 1;
                _ = formatter();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/experimental-attribute.md")]
    public void ExperimentalAttribute()
    {
        var generated = CompileToCSharp("""
            @code {
                [System.Diagnostics.CodeAnalysis.Experimental("RAZOR0001")]
                public class ExperimentalType
                {
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}


