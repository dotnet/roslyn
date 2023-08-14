// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForBuilderDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForBuilderCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
public partial class UseCollectionExpressionForBuilderTests
{
    private const string s_arrayBuilderApi = """

        internal sealed partial class ArrayBuilder<T>
        {
            public void Add(T item) { }
        
            public void AddRange(ArrayBuilder<T> items) { }
            public void AddRange(System.Collections.Immutable.ImmutableArray<T> items) { }
            public void AddRange(System.Collections.Generic.IEnumerable<T> items) { }
            public void AddRange(params T[] items) { }
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
        }
        """;

    #region ImmutableArray

    [Fact]
    public async Task TestNotInCSharp11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net70()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net80()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder.Add(|]0);
                        return builder.ToImmutable();
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        return [0];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestPassToArgument()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder.Add(|]0);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCapacity()
    {
        await new VerifyCS.Test
        {
            TestCode = """
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
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithOtherBuilderUsage()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        builder.Add(0);
                        builder.Clear();
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNoBuilderMutation()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder.Add(|]0);
                        [|foreach (var y in |]x)
                            builder.Add(y);

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder.Add(|]0);
                        [|foreach (var y in |]x)
                        {
                            builder.Add(y);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        builder.Add(0);
                        foreach (var y in x)
                        {
                            builder.Add(0);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int z)
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        builder.Add(0);
                        foreach (var y in x)
                        {
                            builder.Add(z);
                        }

                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
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
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
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
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
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
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAddRange1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder.AddRange(|]x);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAddRange2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder.AddRange(|]1);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAddRange3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>();|]
                        [|builder.AddRange(|]1, 2, 3);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMoveToImmutable()
    {
        await new VerifyCS.Test
        {
            TestCode = """
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
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestToArray()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ImmutableArray.[|CreateBuilder|]<int>(1);|]
                        [|builder.Add(|]0);
                        Goo(builder.ToArray());
                    }

                    void Goo(int[] values) { }
                }
                """,
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
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithOtherBuilderUsageAfter()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        var builder = ImmutableArray.CreateBuilder<int>();
                        builder.Add(0);
                        Goo(builder.ToImmutable());
                        builder.Add(0);
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    #endregion

    #region ArrayBuilder

    [Fact]
    public async Task TestNotInCSharp11_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        var builder = ArrayBuilder<int>.GetInstance();
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net70_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        var builder = ArrayBuilder<int>.GetInstance();
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net80_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestPassToArgument_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestWithCapacity_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
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
    }

    [Fact]
    public async Task TestNotWithOtherBuilderUsage_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        var builder = ArrayBuilder<int>.GetInstance();
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
    }

    [Fact]
    public async Task TestNotWithNoBuilderMutation_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        var builder = ArrayBuilder<int>.GetInstance();
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach1_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestWithForeach2_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestWithForeach3_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        var builder = ArrayBuilder<int>.GetInstance();
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
    }

    [Fact]
    public async Task TestWithForeach4_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int z)
                    {
                        var builder = ArrayBuilder<int>.GetInstance();
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
    }

    [Fact]
    public async Task TestWithForeach5_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestWithForeach6_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestWithForeach7_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestAddRange1_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestAddRange2_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestAddRange3_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|]();|]
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
    }

    [Fact]
    public async Task TestToImmutableAndFree_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|](1);|]
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
    }

    [Fact]
    public async Task TestToImmutableAndClear_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|](1);|]
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
    }

    [Fact]
    public async Task TestToArray_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|](1);|]
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
    }

    [Fact]
    public async Task TestNotWithOtherBuilderUsageAfter_ArrayBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        var builder = ArrayBuilder<int>.GetInstance();
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
    }

    #endregion

    #region ArrayBuilder - opt builder

    [Fact]
    public async Task TestNotInCSharp11_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(out var builder);
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net70_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(out var builder);
                        builder.Add(0);
                        return builder.ToImmutable();
                    }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12_Net80_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<int> M()
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestPassToArgument_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestWithCapacity_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
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
    }

    [Fact]
    public async Task TestNotWithOtherBuilderUsage_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(out var builder);
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
    }

    [Fact]
    public async Task TestNotWithNoBuilderMutation_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(out var builder);
                        Goo(builder.ToImmutable());
                    }

                    void Goo(ImmutableArray<int> values) { }
                }
                """ + s_arrayBuilderApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithForeach1_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestWithForeach2_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestWithForeach3_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(out var builder);
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
    }

    [Fact]
    public async Task TestWithForeach4_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int z)
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(out var builder);
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
    }

    [Fact]
    public async Task TestWithForeach5_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestWithForeach6_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestWithForeach7_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x, int[] y)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestAddRange1_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestAddRange2_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestAddRange3_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M(int[] x)
                    {
                        [|using var _ = ArrayBuilder<int>.[|GetInstance|](out var builder)|]
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
    }

    [Fact]
    public async Task TestToImmutableAndFree_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|](1);|]
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
    }

    [Fact]
    public async Task TestToImmutableAndClear_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|](1);|]
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
    }

    [Fact]
    public async Task TestToArray_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        [|var builder = ArrayBuilder<int>.[|GetInstance|](1);|]
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
    }

    [Fact]
    public async Task TestNotWithOtherBuilderUsageAfter_ArrayBuilder_OutBuilder()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        using var _ = ArrayBuilder<int>.GetInstance(out var builder);
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
    }

    #endregion
}
