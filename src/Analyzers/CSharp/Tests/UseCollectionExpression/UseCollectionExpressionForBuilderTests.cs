// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForBuilderDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForBuilderCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
public sealed partial class UseCollectionExpressionForBuilderTests
{
    private const string s_arrayBuilderApi = """

        internal sealed partial class ArrayBuilder<T>
        {
            public void Add(T item) { }
        
            public void AddRange(ArrayBuilder<T> items) { }
            public void AddRange(System.Collections.Immutable.ImmutableArray<T> items) { }
            public void AddRange(System.Collections.Generic.IEnumerable<T> items) { }
            public void AddRange(params T[] items) { }
            public void AddRange(T[] items, int length) { }
            public void Clear() { }

            public System.Collections.Immutable.ImmutableArray<T> ToImmutable() => default;
            public System.Collections.Immutable.ImmutableArray<T> ToImmutableAndClear() => default;
            public System.Collections.Immutable.ImmutableArray<T> ToImmutableAndFree() => default;
        
            public T[] ToArray() => default;
            public T[] ToArrayAndFree() => default;

            public static ArrayBuilder<T> GetInstance() => default;
            public static ArrayBuilder<T> GetInstance(int capacity) => default;

            public static System.IDisposable GetInstance(out ArrayBuilder<T> instance) { instance = default; return null; }
            public static System.IDisposable GetInstance(int capacity, out ArrayBuilder<T> instance) { instance = default; return null; }
            public static System.IDisposable GetInstance(int capacity, T fillWithValue, out ArrayBuilder<T> instance) { instance = default; return null; }
        }
        """;

    public static readonly IEnumerable<object[]> FailureCreationPatterns =
    [
        ["var builder = ImmutableArray.CreateBuilder<int>();"],
        ["var builder = ArrayBuilder<int>.GetInstance();"],
        ["using var _ = ArrayBuilder<int>.GetInstance(out var builder);"],
    ];

    public static readonly IEnumerable<object[]> SuccessCreationPatterns =
    [
        ["[|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]"],
        ["[|var builder = ArrayBuilder<int>.[|GetInstance|]();|]"],
        ["[|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder);|]"],
    ];

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestNotInCSharp11(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        {{pattern}}
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestInCSharp12_Net70(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        {{pattern}}
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestInCSharp12_Net80(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        return [0];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestCast(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        var v = (ImmutableArray<int>)builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        var v = (ImmutableArray<int>)[0];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestIdentifierCast(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;
                using X = System.Collections.Immutable.ImmutableArray<int>;

                class C
                {
                    void M()
                    {
                        {{pattern}}
                        builder.Add(0);
                        var v = (X)builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestPassToArgument(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestWithCapacity_CreateBuilder()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>(1);|]
                        [|builder.Add(|]0);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestWithCapacity_ArrayBuilder1()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|](1);|]
                        [|builder.Add(|]0);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestWithCapacity_ArrayBuilder2()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](1, out var builder);|]
                        [|builder.Add(|]0);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestNotWithOtherBuilderUsage(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{pattern}}
                        builder.Add(0);
                        builder.Clear();
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestNotWithNoBuilderMutation(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{pattern}}
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestWithForeach1(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        [|foreach (var y in |]x)
                            builder.Add(y);

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        Goo([0, .. x]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestWithForeach2(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        [|foreach (var y in |]x)
                        {
                            builder.Add(y);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        Goo([0, .. x]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestWithForeach3(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        builder.Add(0);
                        foreach (var y in x)
                        {
                            builder.Add(0);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestWithForeach4(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int z)
                    {
                        {{pattern}}
                        builder.Add(0);
                        foreach (var y in x)
                        {
                            builder.Add(z);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestWithForeach5(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        [|foreach (var z in |]x)
                        {
                            builder.Add(z);
                        }
                        [|foreach (var z in |]y)
                        {
                            builder.Add(z);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        Goo([0, .. x, .. y]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestWithForeach6(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        {{pattern}}
                        [|foreach (var z in |]x)
                        {
                            builder.Add(z);
                        }
                        [|builder.Add(|]0);
                        [|foreach (var z in |]y)
                        {
                            builder.Add(z);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        Goo([.. x, 0, .. y]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestWithForeach7(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        {{pattern}}
                        [|foreach (var z in |]x)
                        {
                            builder.Add(z);
                        }
                        [|foreach (var z in |]y)
                        {
                            builder.Add(z);
                        }
                        [|builder.Add(|]0);

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        Goo([.. x, .. y, 0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/70172"), MemberData(nameof(SuccessCreationPatterns)), WorkItem("https://github.com/dotnet/roslyn/issues/69277")]
    public Task TestWithIfStatement1(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        {{pattern}}
                        if (b)
                            builder.Add(0);

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        Goo([.. {|CS0173:b ? [0] : []|}]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestWithIfStatement2(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        {{pattern}}
                        if (b)
                            builder.Add(0);
                        else
                            builder.Add(1);

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        Goo([b ? 0 : 1]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/70172"), MemberData(nameof(SuccessCreationPatterns)), WorkItem("https://github.com/dotnet/roslyn/issues/69277")]
    public Task TestWithIfStatement3(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        {{pattern}}
                        if (b)
                        {
                            builder.Add(0);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        Goo([.. {|CS0173:b ? [0] : []|}]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestWithIfStatement4(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        {{pattern}}
                        if (b)
                        {
                            builder.Add(0);
                        }
                        else
                        {
                            builder.Add(1);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, bool b)
                    {
                        Goo([b ? 0 : 1]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestAddRange1(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        [|builder.AddRange(|]x);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        Goo([.. x]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestAddRange2(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        [|builder.AddRange(|]1);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        Goo([1]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestAddRange3(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        [|builder.AddRange(|]1, 2, 3);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        Goo([1, 2, 3]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestAddRange4(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        builder.AddRange(x, 2);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/71607")]
    public Task TestAddRange5(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        {{pattern}}
                        [|builder.{|CS0121:AddRange|}(|][1, 2, 3]);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        Goo([1, 2, 3]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestMoveToImmutable()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>(1);|]
                        [|builder.Add(|]0);
                        Goo(builder.MoveToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestToImmutableAndFree()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
                        [|builder.Add(|]0);
                        Goo(builder.ToImmutableAndFree());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestToImmutableAndClear()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
                        [|builder.Add(|]0);
                        Goo(builder.ToImmutableAndClear());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestToArray(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        Goo(builder.ToArray());
                    }

                    void Goo(int[] values) { }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([0]);
                    }
                
                    void Goo(int[] values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    public Task TestNotWithOtherBuilderUsageAfter(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{pattern}}
                        builder.Add(0);
                        Goo(builder.ToImmutable());
                        builder.Add(0);
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestNotWithOtherDisposableUsageAfter()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        using var d = ArrayBuilder<int>.GetInstance(out var builder);
                        builder.Add(0);
                        Goo(builder.ToImmutable());
                        d.Dispose();
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestTrivia1(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        {{pattern}}

                        // Leading
                        [|builder.Add(|]0); // Trailing
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        return
                        [
                            // Leading
                            0, // Trailing
                        ];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestMultiLine1(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        {{pattern}}
                        [|builder.Add(|]1 +
                            2);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        return
                        [
                            1 +
                                2,
                        ];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestMultiLine2(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        {{pattern}}
                        [|builder.Add(|]1 +
                            2);
                        [|builder.Add(|]3 +
                            4);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        return
                        [
                            1 +
                                2,
                            3 +
                                4,
                        ];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestOnFillWithValue()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(10, 0, out var builder);
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestGlobalStatement1(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                {{pattern}}
                [|builder.Add(|]0);
                ImmutableArray<int> array = builder.ToImmutable();
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                ImmutableArray<int> array = [0];
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    public Task TestGlobalStatement2(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                {{pattern}}
                [|builder.Add(|]0);
                [|builder.Add(|]1 +
                    2);
                [|builder.Add(|]3 +
                    4);
                ImmutableArray<int> array = builder.ToImmutable();
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                ImmutableArray<int> array =
                [
                    0,
                    1 +
                        2,
                    3 +
                        4,
                ];
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();

    [Fact]
    public Task TestNested1()
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<ImmutableArray<int>> M()
                    {
                        var builder1 = ImmutableArray.CreateBuilder<ImmutableArray<int>>();
                        [|var builder2 = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder2.Add(|]0);
                        builder1.Add(builder2.ToImmutable());
                        return builder1.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<ImmutableArray<int>> M()
                    {
                        return [[0]];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            NumberOfFixAllIterations = 2,
        }.RunAsync();

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public Task TestWithDifferentNewLines(string endOfLine)
        => new VerifyCS.Test
        {
            TestCode = ($$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<ImmutableArray<int>> M()
                    {
                        var builder1 = ImmutableArray.CreateBuilder<ImmutableArray<int>>();
                        [|var builder2 = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder2.Add(|]0);
                        builder1.Add(builder2.ToImmutable());
                        return builder1.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi).ReplaceLineEndings(endOfLine),
            FixedCode = ("""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<ImmutableArray<int>> M()
                    {
                        return [[0]];
                    }
                }
                """ + s_arrayBuilderApi).ReplaceLineEndings(endOfLine),
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            NumberOfFixAllIterations = 2,
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public Task TestInterfaceOn(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Generic;
                using System.Collections.Immutable;

                class C
                {
                    IEnumerable<int> M()
                    {
                        {{pattern}}
                        [|builder.Add(|]0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Generic;
                using System.Collections.Immutable;

                class C
                {
                    IEnumerable<int> M()
                    {
                        return [0];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Theory, MemberData(nameof(FailureCreationPatterns))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public Task TestInterfaceOff(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Generic;
                using System.Collections.Immutable;

                class C
                {
                    IEnumerable<int> M()
                    {
                        {{pattern}}
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();

    [Theory, MemberData(nameof(SuccessCreationPatterns))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/74208")]
    public Task TestComment(string pattern)
        => new VerifyCS.Test
        {
            TestCode = $$"""
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        // Comment to keep
                        {{pattern}}
                        [|builder.Add(|]0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        // Comment to keep
                        return [0];
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
}
