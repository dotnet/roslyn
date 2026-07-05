// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

// Language features not covered by tests:
// - File-local types: compilation-unit-only `file` modifier; Razor documents contribute generated class members instead.
// - Cache delegates for static method group: compiler optimization rather than a Razor-authored source shape.

public sealed class CSharp11LanguageFeaturesIntegrationTest()
    : RazorBaselineIntegrationTestBase(layer: TestProject.Layer.Compiler)
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    internal override string DefaultFileName => "TestComponent.razor";

    protected override string GetDirectoryPath(string testName)
        => $"TestFiles/IntegrationTests/{GetType().Name}/{testName}";

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md")]
    public void RefFields()
    {
        var generated = CompileToCSharp("""
            @code {
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
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/required-members.md")]
    public void RequiredMembers()
    {
        var generated = CompileToCSharp("""
            @code {
                public class Person
                {
                    public required string Name { get; init; }
                }
                
                public static string Value()
                    => new Person { Name = "Razor" }.Name;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/static-abstracts-in-interfaces.md")]
    public void DimForStaticMembers()
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
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/numeric-intptr.md")]
    public void NumericIntPtr()
    {
        var generated = CompileToCSharp("""
            @{
                System.IntPtr value = 1;
                value += 2;
                _ = value;
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/unsigned-right-shift-operator.md")]
    public void UnsignedRightShift()
    {
        var generated = CompileToCSharp("""
            @{
                var value = -8 >>> 1;
                _ = value;
            }

            <div>@(-7 >>> 1)</div>
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/utf8-string-literals.md")]
    public void Utf8StringLiterals()
    {
        var generated = CompileToCSharp(""""
            @{
                System.ReadOnlySpan<byte> bytes = "Razor"u8;
                _ = bytes.Length;
            }

            <div>@("Razor"u8.Length)</div>
            <div>@("""Razor"""u8.Length)</div>
            """");

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/pattern-match-span-of-char-on-string.md")]
    public void PatternMatchingOnReadOnlySpanChar()
    {
        var generated = CompileToCSharp("""
            @{
                System.ReadOnlySpan<char> value = new char[] { 'R', 'a', 'z', 'o', 'r' };
                _ = value is "Razor";
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/checked-user-defined-operators.md")]
    public void CheckedOperators()
    {
        var generated = CompileToCSharp("""
            @code {
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
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/auto-default-structs.md")]
    public void AutoDefaultStructs()
    {
        var generated = CompileToCSharp("""
            @code {
                public struct Counter
                {
                    public int Value;
                
                    public Counter()
                    {
                    }
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/new-line-in-interpolation.md")]
    public void NewlinesInInterpolations()
    {
        var generated = CompileToCSharp("""
            @{
                var value = $"{
                    1 + 1
                }";
                _ = value.Length;
            }

            @($"{
                1 + 1
            }")
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/list-patterns.md")]
    public void ListPatterns()
    {
        var generated = CompileToCSharp("""
            @{
                var values = new[] { 1, 2, 3 };
                _ = values is [1, .., 3];
            }

            @if (values is [1, .., 3])
            {
                <p>Matched</p>
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/raw-string-literal.md")]
    public void RawStringLiterals()
    {
        var generated = CompileToCSharp(""""
            @{
                var text = """
                {"value":"Razor"}
                """;
                _ = text.Length;
            }

            <div>@("""
                {"value":"Razor"}
                """)</div>
            """");

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/extended-nameof-scope.md")]
    public void NameofParameter()
    {
        var generated = CompileToCSharp("""
            @code {
                public static string Capture(int value)
                    => nameof(value);
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/relaxing_shift_operator_requirements.md")]
    public void RelaxingShiftOperator()
    {
        var generated = CompileToCSharp("""
            @{
                short amount = 1;
                var value = 8 << amount;
                _ = value;
            }

            <div>@(8 << amount)</div>
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/csharplang/issues/124")]
    public void GenericAttributes()
    {
        var generated = CompileToCSharp("""
            @code {
                public class GenericAttribute<T> : System.Attribute
                {
                }
                
                [GenericAttribute<string>]
                public class Decorated
                {
                }
            }
            """);

        AssertDocumentNodeMatchesBaseline(generated.CodeDocument);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument);
        CompileToAssembly(generated);
    }

}

