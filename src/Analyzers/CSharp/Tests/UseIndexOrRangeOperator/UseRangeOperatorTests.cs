// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIndexOrRangeOperator;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseRangeOperatorDiagnosticAnalyzer,
    CSharpUseRangeOperatorCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
public sealed class UseRangeOperatorTests
{
    [Fact]
    public Task TestNotInCSharp7()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring(1, s.Length - 1);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact]
    public async Task TestWithMissingReference()
    {
        var source =
            """
            class {|#0:C|}
            {
                {|#1:void|} Goo({|#2:string|} s)
                {
                    var v = s.Substring({|#3:1|}, s.Length - {|#4:1|});
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = new ReferenceAssemblies("custom"),
            TestCode = source,
            ExpectedDiagnostics =
            {
                // /0/Test0.cs(1,7): error CS0518: Predefined type 'System.Object' is not defined or imported
                DiagnosticResult.CompilerError("CS0518").WithLocation(0).WithArguments("System.Object"),
                // /0/Test0.cs(1,7): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                DiagnosticResult.CompilerError("CS1729").WithLocation(0).WithArguments("object", "0"),
                // /0/Test0.cs(3,5): error CS0518: Predefined type 'System.Void' is not defined or imported
                DiagnosticResult.CompilerError("CS0518").WithLocation(1).WithArguments("System.Void"),
                // /0/Test0.cs(3,14): error CS0518: Predefined type 'System.String' is not defined or imported
                DiagnosticResult.CompilerError("CS0518").WithLocation(2).WithArguments("System.String"),
                // /0/Test0.cs(5,29): error CS0518: Predefined type 'System.Int32' is not defined or imported
                DiagnosticResult.CompilerError("CS0518").WithLocation(3).WithArguments("System.Int32"),
                // /0/Test0.cs(5,43): error CS0518: Predefined type 'System.Int32' is not defined or imported
                DiagnosticResult.CompilerError("CS0518").WithLocation(4).WithArguments("System.Int32"),
            },
            FixedCode = source,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36909")]
    public Task TestNotWithoutSystemRange()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp20,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring(1, s.Length - 1);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public async Task TestNotWithInaccessibleSystemRange()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring(1, s.Length - 1);
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp20,
            TestState =
            {
                Sources = { source },
                AdditionalProjects =
                {
                    ["AdditionalProject"] =
                    {
                        Sources =
                        {
                            "namespace System { internal struct Range { } }"
                        }
                    }
                },
                AdditionalProjectReferences = { "AdditionalProject" },
            },
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

    [Fact]
    public Task TestSimple()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|1, s.Length - 1|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..];
                }
            }
            """,
        }.RunAsync();

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
    public async Task TestMultipleDefinitions()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|1, s.Length - 1|]);
                }
            }
            """;

        // Adding a dependency with internal definitions of Index and Range should not break the feature
        var source1 = "namespace System { internal struct Index { } internal struct Range { } }";

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestState =
            {
                Sources = { source },
                AdditionalProjects =
                {
                    ["DependencyProject"] =
                    {
                        ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20,
                        Sources = { source1 },
                    },
                },
                AdditionalProjectReferences = { "DependencyProject" },
            },
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..];
                }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public Task TestComplexSubstraction()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s, int bar, int baz)
                {
                    var v = s.Substring([|bar, s.Length - baz - bar|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s, int bar, int baz)
                {
                    var v = s[bar..^baz];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestSubstringOneArgument()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|1|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestSliceOneArgument()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1|]);
                }
            }
            """,
            FixedCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestExpressionOneArgument()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s.Substring([|bar|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s[bar..];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestConstantSubtraction1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|1, s.Length - 2|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithoutSubtraction()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring(1, 2);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact]
    public Task TestNonStringType()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } public S this[System.Range r] { get => default; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """,
            FixedCode = """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } public S this[System.Range r] { get => default; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[1..^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNonStringTypeWithoutRangeIndexer()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """,
            FixedCode = """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[1..^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNonStringType_Assignment()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public ref S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    s.Slice([|1, s.Length - 2|]) = default;
                }
            }
            """,
            FixedCode = """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public ref S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    s[1..^1] = default;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestMethodToMethod()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Slice(int start, int length) => 0; public int Length { get; } public int Slice(System.Range r) => 0; }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """,
            FixedCode = """
            struct S { public int Slice(int start, int length) => 0; public int Length { get; } public int Slice(System.Range r) => 0; }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice(1..^1);
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestFixAllInvocationToElementAccess1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t.Substring([|s.Substring([|1, s.Length - 2|])[0], t.Length - s.Substring([|1, s.Length - 2|])[0]|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t[s[1..^1][0]..];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestFixAllInvocationToElementAccess2()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t.Substring([|s.Substring([|1|])[0]|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t[s[1..][0]..];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestWithTypeWithActualSliceMethod1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1, s.Length - 1|]);
                }
            }
            """,
            FixedCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestWithTypeWithActualSliceMethod2()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """,
            FixedCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..^1];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43202")]
    public Task TestWritableType()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            struct S { 
                public ref S Slice(int start, int length) => throw null; 
                public int Length { get; } 
                public S this[System.Range r] { get => default; } 
            }

            class C
            {
                void Goo(S s)
                {
                    s.Slice(1, s.Length - 2) = default;
                }
            }
            """,
        }.RunAsync();
    [Fact]
    public Task TestReturnByRef()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    var x = s.Slice([|1, s.Length - 2|]);
                }
            }
            """,
            FixedCode = """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    var x = s[1..^1];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43202")]
    public Task TestIntWritableType()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            struct S { 
                public ref S Slice(int start, int length) => throw null;
                public int Length { get; }
                public S this[int r] { get => default; }
            }

            class C
            {
                void Goo(S s)
                {
                    s.Slice([|1, s.Length - 2|]) = default;
                }
            }
            """,
            FixedCode = """
            using System;
            struct S { 
                public ref S Slice(int start, int length) => throw null;
                public int Length { get; }
                public S this[int r] { get => default; }
            }

            class C
            {
                void Goo(S s)
                {
                    s[1..^1] = default;
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43202")]
    public Task TestReadWriteProperty()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            struct S { 
                public ref S Slice(int start, int length) => throw null;
                public int Length { get; }
                public S this[System.Range r] { get => default; set { } }
            }

            class C
            {
                void Goo(S s)
                {
                    s.Slice([|1, s.Length - 2|]) = default;
                }
            }
            """,
            FixedCode = """
            using System;
            struct S { 
                public ref S Slice(int start, int length) => throw null;
                public int Length { get; }
                public S this[System.Range r] { get => default; set { } }
            }

            class C
            {
                void Goo(S s)
                {
                    s[1..^1] = default;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestWithTypeWithActualSliceMethod3()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1|]);
                }
            }
            """,
            FixedCode = """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36997")]
    public Task TestExpressionWithAddOperatorArgument()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s.Substring([|bar + 1|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s[(bar + 1)..];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestExpressionWithElementAccessShouldNotAddParentheses()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s, int[] bar)
                {
                    _ = s.Substring([|bar[0]|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s, int[] bar)
                {
                    _ = s[bar[0]..];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47183")]
    public Task TestExpressionWithNullConditionalAccess()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            #nullable enable
            public class Test
            {
                public string? M(string? arg)
                    => arg?.Substring([|42|]);
            }
            """,
            FixedCode = """
            #nullable enable
            public class Test
            {
                public string? M(string? arg)
                    => arg?[42..];
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47183")]
    public Task TestExpressionWithNullConditionalAccessWithPropertyAccess()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            public class Test
            {
                public int? M(string arg)
                    => arg?.Substring([|42|]).Length;
            }
            """,
            FixedCode = """
            public class Test
            {
                public int? M(string arg)
                    => arg?[42..].Length;
            }
            """,
        }.RunAsync();

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47183")]
    [InlineData(
        "c.Prop.Substring([|42|])",
        "c.Prop[42..]")]
    [InlineData(
        "c.Prop.Substring([|1, c.Prop.Length - 2|])",
        "c.Prop[1..^1]")]
    [InlineData(
        "c?.Prop.Substring([|42|])",
        "c?.Prop[42..]")]
    [InlineData(
        "c.Prop?.Substring([|42|])",
        "c.Prop?[42..]")]
    [InlineData(
        "c?.Prop?.Substring([|42|])",
        "c?.Prop?[42..]")]
    public Task TestExpressionWithNullConditionalAccessVariations(string subStringCode, string rangeCode)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = $$"""
            public class C
            {
                public string Prop { get; set; }
            }
            public class Test
            {
                public object M(C c)
                    => {{subStringCode}};
            }
            """,
            FixedCode = $$"""
            public class C
            {
                public string Prop { get; set; }
            }
            public class Test
            {
                public object M(C c)
                    => {{rangeCode}};
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38055")]
    public Task TestStringMethod()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = new ReferenceAssemblies("nostdlib"),
            TestCode = """
            namespace System
            {
                public class Object {}
                public class ValueType : Object {}
                public struct Void {}
                public struct Int32 {}
                public struct Index
                {
                    public int GetOffset(int length) => 0;
                    public static implicit operator Index(int value) => default;
                }
                public struct Range
                {
                    public Range(Index start, Index end) {}
                    public Index Start => default;
                    public Index End => default;
                }
                public class String : Object
                {
                    public int Length => 0;
                    public string Substring(int start, int length) => this;

                    string Foo(int x) => Substring([|1, x - 1|]);
                }
            }
            """,
            FixedCode = """
            namespace System
            {
                public class Object {}
                public class ValueType : Object {}
                public struct Void {}
                public struct Int32 {}
                public struct Index
                {
                    public int GetOffset(int length) => 0;
                    public static implicit operator Index(int value) => default;
                }
                public struct Range
                {
                    public Range(Index start, Index end) {}
                    public Index Start => default;
                    public Index End => default;
                }
                public class String : Object
                {
                    public int Length => 0;
                    public string Substring(int start, int length) => this;

                    string Foo(int x) => this[1..x];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38055")]
    public Task TestSliceOnThis()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                public int Length => 0;
                public C Slice(int start, int length) => this;

                public C Foo(int x) => Slice([|1, x - 1|]);
            }
            """,
            FixedCode = """
            class C
            {
                public int Length => 0;
                public C Slice(int start, int length) => this;

                public C Foo(int x) => this[1..x];
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public Task TestStartingFromZero()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public Task TestStartingFromAribtraryPosition()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|5|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[5..];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public Task TestStartingFromZeroToArbitraryEnd()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0, 5|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..5];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public Task TestStartingFromZeroGoingToLength()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0, s.Length|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40438")]
    public Task TestStartingFromZeroGoingToLengthMinus1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0, s.Length - 1|]);
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..^1];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49347")]
    public Task TestNotInExpressionTree()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    Expression<Func<string, int, string>> e = (s, i) => s.Substring(i);
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem(60988, "https://github.com/dotnet/roslyn/issues/60988")]
    public Task TestCheckedExpression()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode =
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    Span<byte> buffer = new byte[]{ (byte)'h', (byte)'i', 0 };
                    long length = 2;
                    var sliced = buffer.Slice([|0, unchecked((int)length)|]); // or checked((int)length)
                }
            }
            """,
            FixedCode =
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    Span<byte> buffer = new byte[]{ (byte)'h', (byte)'i', 0 };
                    long length = 2;
                    var sliced = buffer[..unchecked((int)length)]; // or checked((int)length)
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestRemoveFromZeroToArbitraryLocation()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|0, x|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[x..];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestRemoveFromZeroToArbitraryLocation1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|0, x + 1|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[(x + 1)..];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestRemoveFromZeroToLengthMinusSomeAmount()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|0, s.Length - x|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[^x..];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestRemoveFromZeroToLengthMinusSomeAmount1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|0, s.Length - (x + 1)|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[^(x + 1)..];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestRemoveFromPositionToLengthMinusThatPosition()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|x, s.Length - x|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[..x];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestRemoveFromLengthMinusPositionToPosition()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|s.Length - x, x|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[..^x];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestRemoveFromLengthMinusPositionToPosition2()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|s.Length - (x + 1), x + 1|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[..^(x + 1)];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestOneArgRemove()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|x|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[..x];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestOneArgRemove1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|x + 1|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[..(x + 1)];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76091")]
    public Task TestOneArgFromEnd()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s.Remove([|s.Length - x|]);
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void Goo(string s, int x)
                    {
                        var v = s[..^x];
                    }
                }
                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78069")]
    public Task TestStackAllocSlice()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            class C
            {
                void Goo(int length)
                {
                    Span<byte> a = stackalloc byte[10].Slice([|0, length|]);
                }
            }
            """,
            FixedCode = """
            using System;
            class C
            {
                void Goo(int length)
                {
                    Span<byte> a = (stackalloc byte[10])[..length];
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78069")]
    public Task TestImplicitStackAllocSlice()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System;
            class C
            {
                void Goo(int length)
                {
                    Span<int> a = stackalloc[] { 1, 2, 3, 4, 5 }.Slice([|0, length|]);
                }
            }
            """,
            FixedCode = """
            using System;
            class C
            {
                void Goo(int length)
                {
                    Span<int> a = (stackalloc[] { 1, 2, 3, 4, 5 })[..length];
                }
            }
            """,
        }.RunAsync();
}
