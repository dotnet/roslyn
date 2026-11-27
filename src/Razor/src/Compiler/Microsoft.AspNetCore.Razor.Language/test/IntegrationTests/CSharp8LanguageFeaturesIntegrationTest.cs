// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public sealed class CSharp8LanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/default-interface-methods.md")]
    public void DefaultInterfaceMethods()
    {
        var generated = CompileToCSharp("""
            @code {
                public interface IExample
                {
                    int GetValue() => 1;
                }
                
                public class Example : IExample
                {
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/nullable-reference-types-specification.md")]
    public void NullableReferenceType()
    {
        var generated = CompileToCSharp("""
            @code {
                public static int GetLength(string? value)
                    => value?.Length ?? 0;
            }

            @GetLength(null!)
            """,
            nullableEnable: true);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/patterns.md")]
    public void RecursivePatterns()
    {
        var generated = CompileToCSharp("""
            @{
                var value = new { X = 1, Y = 2 };
                _ = value is { X: 1, Y: > 0 };
            }

            @if (value is { X: 1, Y: > 0 })
            {
                <p>Pattern matched!</p>
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/async-streams.md")]
    public void AsyncStreams()
    {
        var generated = CompileToCSharp("""
            @code {
                public static async System.Collections.Generic.IAsyncEnumerable<int> Values()
                {
                    yield return 1;
                    await System.Threading.Tasks.Task.Yield();
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/using.md")]
    public void EnhancedUsing()
    {
        var generated = CompileToCSharp("""
            @{
                using var stream = new System.IO.MemoryStream();
                _ = stream.Length;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md")]
    public void Ranges()
    {
        var generated = CompileToCSharp("""
            @{
                var values = new[] { 1, 2, 3, 4 };
                var slice = values[1..^1];
                _ = slice.Length;
            }

            @values[1..^1]
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/null-coalescing-assignment.md")]
    public void NullCoalescingAssignment()
    {
        var generated = CompileToCSharp("""
            @{
                string? value = null;
                value ??= "Razor";
                _ = value.Length;
            }
            """,
            nullableEnable: true);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1630")]
    public void AlternativeInterpolatedVerbatimStrings()
    {
        var generated = CompileToCSharp("""
            @{
                var value = $@"Hello {1}";
                _ = value.Length;
            }

            @($@"Hello {1}")
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/stackalloc-array-initializers.md")]
    public void StackallocInNestedContexts()
    {
        var generated = CompileToCSharp("""
            @{
                var flag = true;
                System.Span<int> values = flag ? stackalloc int[] { 1, 2 } : stackalloc int[] { 3, 4 };
                _ = values[0];
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1744")]
    public void UnmanagedGenericStructs()
    {
        var baseCompilation = BaseCompilation;
        baseCompilation = baseCompilation.WithOptions(baseCompilation.Options.WithAllowUnsafe(true));

        var generated = CompileToCSharp("""
            @code {
                public static unsafe int SizeOf<T>()
                    where T : unmanaged
                    => sizeof(T);
            }
            """,
            baseCompilation: baseCompilation);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1565")]
    public void StaticLocalFunctions()
    {
        var generated = CompileToCSharp("""
            @{
                static int Increment(int value) => value + 1;
                _ = Increment(1);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/readonly-instance-members.md")]
    public void ReadonlyMembers()
    {
        var generated = CompileToCSharp("""
            @code {
                public readonly struct Measurement
                {
                    private readonly int _value;
                
                    public Measurement(int value)
                    {
                        _value = value;
                    }
                
                    public readonly int GetValue()
                        => _value;
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}


