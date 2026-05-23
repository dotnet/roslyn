// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - First-class Span Types: still primarily a compiler/runtime interaction surface and not clearly isolated by a Razor-specific snippet here.
// - String literals in data section as UTF8: emit/codegen optimization rather than Razor-authored source.
// - Simple lambda parameters with modifiers: syntax is still evolving and not yet represented with a stable Razor-specific case here.
// - Partial Events and Constructors: requires a larger multi-part type setup than this Razor integration sweep is targeting.
// - Ignored directives: file-based directive surface that Razor does not author directly.

public sealed class CSharp14LanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/field-keyword.md")]
    public void FieldKeywordInProperties()
    {
        var generated = CompileToCSharp("""
            @code {
                public int Value
                {
                    get => field;
                    set => field = value;
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/unbound-generic-types-in-nameof.md")]
    public void UnboundGenericTypesInNameof()
    {
        var generated = CompileToCSharp("""
            @{
                _ = nameof(System.Collections.Generic.List<>);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/extensions.md")]
    public void Extensions()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            public static class NumberExtensions
            {
                extension(int value)
                {
                    public int Double()
                        => value * 2;
                }
            }
            """));

        var generated = CompileToCSharp("""
            @{
                var value = 1;
                _ = value.Double();
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/null-conditional-assignment.md")]
    public void NullConditionalAssignment()
    {
        var generated = CompileToCSharp("""
            @{
                int[] values = new int[1];
                values?[0] = 1;
                _ = values[0];
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/user-defined-compound-assignment.md")]
    public void UserDefinedCompoundAssignmentOperators()
    {
        var generated = CompileToCSharp("""
            @{
                var counter = new Counter();
                counter += 1;
                _ = counter.Value;
            }

            @code {
                public class Counter
                {
                    public int Value { get; private set; }
                
                    public void operator +=(int value)
                    {
                        Value += value;
                    }
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-14.0/optional-and-named-parameters-in-expression-trees.md")]
    public void OptionalAndNamedArgumentsInExpressionTrees()
    {
        var generated = CompileToCSharp("""
            @code {
                public static int Sum(int first = 0, int second = 0)
                    => first + second;
            }
            
            @{
                System.Linq.Expressions.Expression<System.Func<int>> named = () => Sum(first: 1, second: 2);
                System.Linq.Expressions.Expression<System.Func<int>> optional = () => Sum();
                _ = named.Body;
                _ = optional.Body;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}


