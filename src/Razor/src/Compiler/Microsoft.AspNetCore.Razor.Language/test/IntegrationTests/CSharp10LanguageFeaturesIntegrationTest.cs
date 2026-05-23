// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - Global Using Directive: compilation-unit-only directive that Razor source does not author directly.
// - Improved Definite Assignment: flow-analysis improvement without a distinct Razor syntax surface.
// - Source Generator V2 APIs: Roslyn API surface, not Razor-authored source.
// - Async method builder override: requires substantial task-like / builder plumbing that is not a Razor-specific surface.
// - Enhanced `#line` directive: Razor already owns emitted line directives; this sweep focuses on end-user Razor-authored source.
// - Interpolated string improvements: mostly handler plumbing / overload-shape interaction rather than a Razor-specific surface.

public sealed class CSharp10LanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/record-structs.md")]
    public void RecordStructs()
    {
        var generated = CompileToCSharp("""
            @code {
                public readonly record struct Point(int X, int Y);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/constant_interpolated_strings.md")]
    public void ConstantInterpolatedStrings()
    {
        var generated = CompileToCSharp("""
            @code {
                private const string Name = "Razor";
                private const string Value = $"{Name} Tests";
            }

            @($"{Name} Tests")
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/extended-property-patterns.md")]
    public void ExtendedPropertyPatterns()
    {
        var generated = CompileToCSharp("""
            @{
                var value = new { A = new { B = 1 } };
                _ = value is { A.B: 1 };
            }

            @if (value is { A.B: 1 })
            {
                <p>Matched</p>
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/4174")]
    public void SealedRecordToString()
    {
        var generated = CompileToCSharp("""
            @code {
                public sealed record Person(string Name);
                
                public static string Format()
                    => new Person("Razor").ToString();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/125")]
    public void MixDeclarationsAndVariablesInDeconstruction()
    {
        var generated = CompileToCSharp("""
            @{
                var tuple = (1, 2);
                int second = 0;
                (int first, second) = tuple;
                _ = first + second;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/lambda-improvements.md")]
    public void LambdaImprovements()
    {
        var generated = CompileToCSharp("""
            @{
                var increment = int (int value) => value + 1;
                _ = increment(1);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/static-abstracts-in-interfaces.md")]
    public void StaticAbstractMembersInInterfaces()
    {
        var generated = CompileToCSharp("""
            @code {
                public interface IValue<TSelf>
                    where TSelf : IValue<TSelf>
                {
                    static abstract TSelf Zero { get; }
                }
                
                public readonly struct Value : IValue<Value>
                {
                    public static Value Zero => new();
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/file-scoped-namespaces.md")]
    public void FileScopedNamespace()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            namespace Helpers;
            
            public static class Utility
            {
                public static string GetValue()
                    => "Razor";
            }
            """));

        var generated = CompileToCSharp("""
            @{
                _ = Helpers.Utility.GetValue();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/parameterless-struct-constructors.md")]
    public void ParameterlessStructConstructors()
    {
        var generated = CompileToCSharp("""
            @code {
                public struct Counter
                {
                    public int Value { get; }
                
                    public Counter()
                    {
                        Value = 1;
                    }
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-10.0/caller-argument-expression.md")]
    public void CallerExpressionAttribute()
    {
        var generated = CompileToCSharp("""
            @code {
                public static string Capture(
                    bool condition,
                    [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(condition))] string? expression = null)
                    => expression ?? string.Empty;
            }

            <div>@Capture(1 + 1 == 2)</div>
            """,
            nullableEnable: true);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}


