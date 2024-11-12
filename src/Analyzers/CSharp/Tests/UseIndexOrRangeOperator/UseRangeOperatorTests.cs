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
public class UseRangeOperatorTests
{
    [Fact]
    public async Task TestNotInCSharp7()
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
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();
    }

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
    public async Task TestNotWithoutSystemRange()
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
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();
    }

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
    public async Task TestSimple()
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
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

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
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..];
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
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestComplexSubstraction()
    {
        var source =
            """
            class C
            {
                void Goo(string s, int bar, int baz)
                {
                    var v = s.Substring([|bar, s.Length - baz - bar|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s, int bar, int baz)
                {
                    var v = s[bar..^baz];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSubstringOneArgument()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|1|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSliceOneArgument()
    {
        var source =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1|]);
                }
            }
            """;
        var fixedSource =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExpressionOneArgument()
    {
        var source =
            """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s.Substring([|bar|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s[bar..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestConstantSubtraction1()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|1, s.Length - 2|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[1..^1];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithoutSubtraction()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring(1, 2);
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = source,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNonStringType()
    {
        var source =
            """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } public S this[System.Range r] { get => default; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """;
        var fixedSource =
            """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } public S this[System.Range r] { get => default; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[1..^1];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNonStringTypeWithoutRangeIndexer()
    {
        var source =
            """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """;
        var fixedSource =
            """
            struct S { public S Slice(int start, int length) => default; public int Length { get; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[1..^1];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNonStringType_Assignment()
    {
        var source =
            """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public ref S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    s.Slice([|1, s.Length - 2|]) = default;
                }
            }
            """;
        var fixedSource =
            """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public ref S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    s[1..^1] = default;
                }
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMethodToMethod()
    {
        var source =
            """
            struct S { public int Slice(int start, int length) => 0; public int Length { get; } public int Slice(System.Range r) => 0; }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """;
        var fixedSource =
            """
            struct S { public int Slice(int start, int length) => 0; public int Length { get; } public int Slice(System.Range r) => 0; }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Slice(1..^1);
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAllInvocationToElementAccess1()
    {
        // Note: once the IOp tree has support for range operators, this should 
        // simplify even further.
        var source =
            """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t.Substring([|s.Substring([|1, s.Length - 2|])[0], t.Length - s.Substring([|1, s.Length - 2|])[0]|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t[s[1..^1][0]..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAllInvocationToElementAccess2()
    {
        // Note: once the IOp tree has support for range operators, this should 
        // simplify even further.
        var source =
            """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t.Substring([|s.Substring([|1|])[0]|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s, string t)
                {
                    var v = t[s[1..][0]..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithTypeWithActualSliceMethod1()
    {
        var source =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1, s.Length - 1|]);
                }
            }
            """;
        var fixedSource =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithTypeWithActualSliceMethod2()
    {
        var source =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1, s.Length - 2|]);
                }
            }
            """;
        var fixedSource =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..^1];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43202")]
    public async Task TestWritableType()
    {
        var source =
            """
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
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = source,
        }.RunAsync();
    }
    [Fact]
    public async Task TestReturnByRef()
    {
        var source =
            """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    var x = s.Slice([|1, s.Length - 2|]);
                }
            }
            """;
        var fixedSource =
            """
            struct S { public ref S Slice(int start, int length) => throw null; public int Length { get; } public S this[System.Range r] { get => throw null; } }
            class C
            {
                void Goo(S s)
                {
                    var x = s[1..^1];
                }
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43202")]
    public async Task TestIntWritableType()
    {
        var source =
            """
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
            """;
        var fixedSource =
            """
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
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43202")]
    public async Task TestReadWriteProperty()
    {
        var source =
            """
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
            """;
        var fixedSource =
            """
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
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithTypeWithActualSliceMethod3()
    {
        var source =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s.Slice([|1|]);
                }
            }
            """;
        var fixedSource =
            """
            using System;
            class C
            {
                void Goo(Span<int> s)
                {
                    var v = s[1..];
                }
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36997")]
    public async Task TestExpressionWithAddOperatorArgument()
    {
        var source =
            """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s.Substring([|bar + 1|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s, int bar)
                {
                    var v = s[(bar + 1)..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact]
    public async Task TestExpressionWithElementAccessShouldNotAddParentheses()
    {
        var source =
            """
            class C
            {
                void Goo(string s, int[] bar)
                {
                    _ = s.Substring([|bar[0]|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s, int[] bar)
                {
                    _ = s[bar[0]..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47183")]
    public async Task TestExpressionWithNullConditionalAccess()
    {
        var source =
            """
            #nullable enable
            public class Test
            {
                public string? M(string? arg)
                    => arg?.Substring([|42|]);
            }
            """;
        var fixedSource =
            """
            #nullable enable
            public class Test
            {
                public string? M(string? arg)
                    => arg?[42..];
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47183")]
    public async Task TestExpressionWithNullConditionalAccessWithPropertyAccess()
    {
        var source =
            """
            public class Test
            {
                public int? M(string arg)
                    => arg?.Substring([|42|]).Length;
            }
            """;
        var fixedSource =
            """
            public class Test
            {
                public int? M(string arg)
                    => arg?[42..].Length;
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

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
    public async Task TestExpressionWithNullConditionalAccessVariations(string subStringCode, string rangeCode)
    {
        var source =
            $$"""
            public class C
            {
                public string Prop { get; set; }
            }
            public class Test
            {
                public object M(C c)
                    => {{subStringCode}};
            }
            """;
        var fixedSource =
            $$"""
            public class C
            {
                public string Prop { get; set; }
            }
            public class Test
            {
                public object M(C c)
                    => {{rangeCode}};
            }
            """;
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38055")]
    public async Task TestStringMethod()
    {
        var source =
            """
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
            """;
        var fixedSource =
            """
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
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = new ReferenceAssemblies("nostdlib"),
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38055")]
    public async Task TestSliceOnThis()
    {
        var source =
            """
            class C
            {
                public int Length => 0;
                public C Slice(int start, int length) => this;

                public C Foo(int x) => Slice([|1, x - 1|]);
            }
            """;
        var fixedSource =
            """
            class C
            {
                public int Length => 0;
                public C Slice(int start, int length) => this;

                public C Foo(int x) => this[1..x];
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public async Task TestStartingFromZero()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public async Task TestStartingFromAribtraryPosition()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|5|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[5..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public async Task TestStartingFromZeroToArbitraryEnd()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0, 5|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..5];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56269")]
    public async Task TestStartingFromZeroGoingToLength()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0, s.Length|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40438")]
    public async Task TestStartingFromZeroGoingToLengthMinus1()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s.Substring([|0, s.Length - 1|]);
                }
            }
            """;
        var fixedSource =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[..^1];
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = fixedSource,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49347")]
    public async Task TestNotInExpressionTree()
    {
        var source =
            """
            using System;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    Expression<Func<string, int, string>> e = (s, i) => s.Substring(i);
                }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = source,
            FixedCode = source,
        }.RunAsync();
    }

    [Fact, WorkItem(60988, "https://github.com/dotnet/roslyn/issues/60988")]
    public async Task TestCheckedExpression()
    {
        await new VerifyCS.Test
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
    }
}
