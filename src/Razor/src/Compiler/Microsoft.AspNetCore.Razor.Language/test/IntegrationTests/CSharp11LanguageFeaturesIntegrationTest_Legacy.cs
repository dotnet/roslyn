// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - File-local types: compilation-unit-only `file` modifier; Razor documents contribute generated class members instead.
// - Cache delegates for static method group: compiler optimization rather than a Razor-authored source shape.

public sealed class CSharp11LanguageFeaturesIntegrationTest_Legacy : IntegrationTestBase
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

    public CSharp11LanguageFeaturesIntegrationTest_Legacy()
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md")]
    public void RefFields()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public ref struct ValueRef
                {
                    private ref int _value;
                
                    public ValueRef(ref int value)
                    {
                        _value = ref value;
                    }
                
                    public ref int Value => ref _value;
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/required-members.md")]
    public void RequiredMembers()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public class Person
                {
                    public required string Name { get; init; }
                }
                
                public static string Value()
                    => new Person { Name = "Razor" }.Name;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/static-abstracts-in-interfaces.md")]
    public void DimForStaticMembers()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
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
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/numeric-intptr.md")]
    public void NumericIntPtr()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                System.IntPtr value = 1;
                value += 2;
                _ = value;
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/unsigned-right-shift-operator.md")]
    public void UnsignedRightShift()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = -8 >>> 1;
                _ = value;
            }

            <div>@(-7 >>> 1)</div>
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/utf8-string-literals.md")]
    public void Utf8StringLiterals()
    {
        var generated = CompileToCSharp(""""
            @inherits global::LegacyTemplateBase
            
            @{
                System.ReadOnlySpan<byte> bytes = "Razor"u8;
                _ = bytes.Length;
            }

            <div>@("Razor"u8.Length)</div>
            <div>@("""Razor"""u8.Length)</div>
            """",
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/pattern-match-span-of-char-on-string.md")]
    public void PatternMatchingOnReadOnlySpanChar()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                System.ReadOnlySpan<char> value = new char[] { 'R', 'a', 'z', 'o', 'r' };
                _ = value is "Razor";
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/checked-user-defined-operators.md")]
    public void CheckedOperators()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public readonly struct Counter
                {
                    public int Value { get; }
                
                    public Counter(int value)
                    {
                        Value = value;
                    }
                
                    public static Counter operator +(Counter left, Counter right)
                        => new(left.Value + right.Value);
                
                    public static Counter operator checked +(Counter left, Counter right)
                        => new(checked(left.Value + right.Value));
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/auto-default-structs.md")]
    public void AutoDefaultStructs()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public struct Counter
                {
                    public int Value;
                
                    public Counter()
                    {
                    }
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/new-line-in-interpolation.md")]
    public void NewlinesInInterpolations()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var value = $"{
                    1 + 1
                }";
                _ = value.Length;
            }

            @($"{
                1 + 1
            }")
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/list-patterns.md")]
    public void ListPatterns()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                var values = new[] { 1, 2, 3 };
                _ = values is [1, .., 3];
            }

            @if (values is [1, .., 3])
            {
                <p>Matched</p>
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/raw-string-literal.md")]
    public void RawStringLiterals()
    {
        var generated = CompileToCSharp(""""
            @inherits global::LegacyTemplateBase
            
            @{
                var text = """
                {"value":"Razor"}
                """;
                _ = text.Length;
            }

            <div>@("""
                {"value":"Razor"}
                """)</div>
            """",
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/extended-nameof-scope.md")]
    public void NameofParameter()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public static string Capture(int value)
                    => nameof(value);
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/relaxing_shift_operator_requirements.md")]
    public void RelaxingShiftOperator()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @{
                short amount = 1;
                var value = 8 << amount;
                _ = value;
            }

            <div>@(8 << amount)</div>
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/124")]
    public void GenericAttributes()
    {
        var generated = CompileToCSharp("""
            @inherits global::LegacyTemplateBase
            
            @functions {
                public class GenericAttribute<T> : System.Attribute
                {
                }
                
                [GenericAttribute<string>]
                public class Decorated
                {
                }
            }
            """,
            path: DefaultLegacyFileName);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetRequiredDocumentNode());
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetRequiredImplCSharpDocument());
        AssertCSharpDiagnosticsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}

