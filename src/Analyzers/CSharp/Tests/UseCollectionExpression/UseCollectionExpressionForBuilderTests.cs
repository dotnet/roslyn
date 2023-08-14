// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
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

    private const string s_collectionBuilderApi = """

        namespace System.Runtime.CompilerServices
        {
            [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
            public sealed class CollectionBuilderAttribute : Attribute
            {
                public CollectionBuilderAttribute(Type builderType, string methodName) { }
            }
        }
        """;

    private const string s_basicCollectionApi = """

        static partial class MyCollection
        {
            public static MyCollection<T> Create<T>(System.ReadOnlySpan<T> values) => default;
            public static MyCollection<T> Create<T>(System.Span<T> values) => default;

            public static MyCollection<T> Create<T>() => default;
            public static MyCollection<T> Create<T>(T t1) => default;
            public static MyCollection<T> Create<T>(T t1, T t2) => default;
            public static MyCollection<T> Create<T>(T t1, T t2, T t3) => default;
            public static MyCollection<T> Create<T>(T t1, T t2, T t3, T t4) => default;
            public static MyCollection<T> Create<T>(params T[] values) => default;
            public static MyCollection<T> CreateRange<T>(System.Collections.Generic.IEnumerable<T> values) => default;
        }

        [System.Runtime.CompilerServices.CollectionBuilder(typeof(MyCollection), "Create")]
        class MyCollection<T> : System.Collections.Generic.IEnumerable<T>
        {
            public System.Collections.Generic.IEnumerator<T> GetEnumerator() => default;
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => default;
        }
        
        [System.Runtime.CompilerServices.CollectionBuilder(typeof(MyCollection), "Create")]
        interface IMyCollection<T> : System.Collections.Generic.IEnumerable<T>
        {
        }
        """;

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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
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
                
                    void Goo(ImmutableArray<int> value) { }
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

                    void Goo(ImmutableArray<int> value) { }
                }
                """,
            FixedCode = """
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        Goo([1, 2, 3]);
                    }
                
                    void Goo(ImmutableArray<int> value) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }
}
