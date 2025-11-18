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
    CSharpUseIndexOperatorDiagnosticAnalyzer,
    CSharpUseIndexOperatorCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseIndexOperator)]
public sealed class UseIndexOperatorTests
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
                    var v = s[s.Length - 1];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp7,
        }.RunAsync();

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseRangeOperator)]
    public async Task TestWithMissingReference()
    {
        var source =
            """
            class {|#0:C|}
            {
                {|#1:void|} Goo({|#2:string|} s)
                {
                    var v = s[s.Length - {|#3:1|}];
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
                // /0/Test0.cs(5,30): error CS0518: Predefined type 'System.Int32' is not defined or imported
                DiagnosticResult.CompilerError("CS0518").WithLocation(3).WithArguments("System.Int32"),
            },
            FixedCode = source,
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
                    var v = s[[|s.Length - 1|]];
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public async Task TestMultipleDefinitions()
    {
        var source =
            """
            class C
            {
                void Goo(string s)
                {
                    var v = s[[|s.Length - 1|]];
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
                    var v = s[^1];
                }
            }
            """,
        }.RunAsync();
    }

    [Fact]
    public Task TestComplexSubtaction()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[[|s.Length - (1 + 1)|]];
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[^(1 + 1)];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestComplexInstance()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System.Linq;

            class C
            {
                void Goo(string[] ss)
                {
                    var v = ss.Last()[[|ss.Last().Length - 3|]];
                }
            }
            """,
            FixedCode = """
            using System.Linq;

            class C
            {
                void Goo(string[] ss)
                {
                    var v = ss.Last()[^3];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithoutSubtraction1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[s.Length];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithoutSubtraction2()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v = s[s.Length + 1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotWithMultipleArgs()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Length { get; } public int this[int i] { get => 0; } public int this[int i, int j] { get => 0; } public int this[System.Index i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[s.Length - 1, 2];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedTypeWithLength()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Length { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[[|s.Length - 2|]];
                }
            }
            """,
            FixedCode = """
            struct S { public int Length { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[^2];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedTypeWithCount()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Count { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[[|s.Count - 2|]];
                }
            }
            """,
            FixedCode = """
            struct S { public int Count { get; } public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[^2];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedTypeWithNoLengthOrCount()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int this[int i] { get => 0; } public int this[System.Index i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[s.{|CS1061:Count|} - 2];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedTypeWithNoInt32Indexer()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Length { get; } public int this[System.Index i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[s.Length - 2];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedTypeWithNoIndexIndexer()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Count { get; } public int this[int i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[[|s.Count - 2|]];
                }
            }
            """,
            FixedCode = """
            struct S { public int Count { get; } public int this[int i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s[^2];
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
            struct S { public int Length { get; } public int Get(int i) => 0; public int Get(System.Index i) => 0; }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Get([|s.Length - 1|]);
                }
            }
            """,
            FixedCode = """
            struct S { public int Length { get; } public int Get(int i) => 0; public int Get(System.Index i) => 0; }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Get(^1);
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestMethodToMethodMissingIndexIndexer()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Length { get; } public int Get(int i) => 0; }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Get(s.Length - 1);
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestMethodToMethodWithIntIndexer()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            struct S { public int Length { get; } public int Get(int i) => 0; public int this[int i] { get => 0; } }
            class C
            {
                void Goo(S s)
                {
                    var v = s.Get(s.Length - 1);
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36909")]
    public Task TestMissingWithNoSystemIndex()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp20,
            TestCode = """
            class C
            {
                void Goo(string[] s)
                {
                    var v = s[s.Length - 1];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp8,
        }.RunAsync();

    [Fact]
    public async Task TestMissingWithInaccessibleSystemIndex()
    {
        var source =
            """
            class C
            {
                void Goo(string[] s)
                {
                    var v = s[s.Length - 1];
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
                            "namespace System { internal struct Index { } }"
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
    public Task TestArray()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string[] s)
                {
                    var v = s[[|s.Length - 1|]];
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string[] s)
                {
                    var v = s[^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestFixAll1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string s)
                {
                    var v1 = s[[|s.Length - 1|]];
                    var v2 = s[[|s.Length - 1|]];
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string s)
                {
                    var v1 = s[^1];
                    var v2 = s[^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNestedFixAll1()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string[] s)
                {
                    var v1 = s[[|s.Length - 2|]][[|s[[|s.Length - 2|]].Length - 1|]];
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string[] s)
                {
                    var v1 = s[^2][^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNestedFixAll2()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            class C
            {
                void Goo(string[] s)
                {
                    var v1 = s[[|s.Length - 2|]][[|s[[|s.Length - 2|]].Length - 1|]];
                }
            }
            """,
            FixedCode = """
            class C
            {
                void Goo(string[] s)
                {
                    var v1 = s[^2][^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestSimple_NoIndexIndexer_SupportsIntIndexer()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System.Collections.Generic;
            class C
            {
                void Goo(List<int> s)
                {
                    var v = s[[|s.Count - 1|]];
                }
            }
            """,
            FixedCode = """
            using System.Collections.Generic;
            class C
            {
                void Goo(List<int> s)
                {
                    var v = s[^1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestSimple_NoIndexIndexer_SupportsIntIndexer_Set()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System.Collections.Generic;
            class C
            {
                void Goo(List<int> s)
                {
                    s[[|s.Count - 1|]] = 1;
                }
            }
            """,
            FixedCode = """
            using System.Collections.Generic;
            class C
            {
                void Goo(List<int> s)
                {
                    s[^1] = 1;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task NotOnConstructedIndexer()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
            using System.Collections.Generic;
            class C
            {
                void Goo(Dictionary<int, string> s)
                {
                    var v = s[s.Count - 1];
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
            using System.Collections.Generic;
            using System.Linq.Expressions;
            class C
            {
                void Goo(List<int> s)
                {
                    Expression<Func<int>> f = () => s[s.Count - 1];
                }
            }
            """,
        }.RunAsync();
}
