// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public sealed class CSharp8LanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
{
    private const string DefaultLegacyFileName = "TestView.cshtml";

    private const string LegacyTemplateBaseSource =
        """
        public abstract class LegacyTemplateBase
        {
            public virtual System.Threading.Tasks.Task ExecuteAsync()
                => System.Threading.Tasks.Task.CompletedTask;

            protected void WriteLiteral(string value)
            {
            }

            protected void Write(object value)
            {
            }
        }
        """;

    public CSharp8LanguageFeaturesIntegrationTest_Legacy()
        : base(layer: TestProject.Layer.Compiler)
    {
        AddCSharpSyntaxTree(LegacyTemplateBaseSource, filePath: "LegacyTemplateBase.cs");
    }

    public override string GetTestFileName([CallerMemberName] string? testName = null)
    {
        var fileName = $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";
        var directory = Path.GetDirectoryName(fileName);
        if (directory is not null)
        {
            Directory.CreateDirectory(Path.Combine(TestProjectRoot, directory));
        }

        return fileName;
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/default-interface-methods.md")]
    public void DefaultInterfaceMethods()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public interface IExample
                {
                    int GetValue() => 1;
                }
                
                public class Example : IExample
                {
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/nullable-reference-types-specification.md")]
    public void NullableReferenceType()
    {
        NullableEnable = true;
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static int GetLength(string? value)
                    => value?.Length ?? 0;
            }

            @GetLength(null!)
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/patterns.md")]
    public void RecursivePatterns()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = new { X = 1, Y = 2 };
                _ = value is { X: 1, Y: > 0 };
            }

            @if (value is { X: 1, Y: > 0 })
            {
                <p>Pattern matched!</p>
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/async-streams.md")]
    public void AsyncStreams()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static async System.Collections.Generic.IAsyncEnumerable<int> Values()
                {
                    yield return 1;
                    await System.Threading.Tasks.Task.Yield();
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/using.md")]
    public void EnhancedUsing()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                using var stream = new System.IO.MemoryStream();
                _ = stream.Length;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md")]
    public void Ranges()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var values = new[] { 1, 2, 3, 4 };
                var slice = values[1..^1];
                _ = slice.Length;
            }

            @values[1..^1]
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/null-coalescing-assignment.md")]
    public void NullCoalescingAssignment()
    {
        NullableEnable = true;
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                string? value = null;
                value ??= "Razor";
                _ = value.Length;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1630")]
    public void AlternativeInterpolatedVerbatimStrings()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = $@"Hello {1}";
                _ = value.Length;
            }

            @($@"Hello {1}")
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/stackalloc-array-initializers.md")]
    public void StackallocInNestedContexts()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var flag = true;
                System.Span<int> values = flag ? stackalloc int[] { 1, 2 } : stackalloc int[] { 3, 4 };
                _ = values[0];
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1744")]
    public void UnmanagedGenericStructs()
    {
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithAllowUnsafe(true));

        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static unsafe int SizeOf<T>()
                    where T : unmanaged
                    => sizeof(T);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/1565")]
    public void StaticLocalFunctions()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                static int Increment(int value) => value + 1;
                _ = Increment(1);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/readonly-instance-members.md")]
    public void ReadonlyMembers()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
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
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}


