// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForCreateCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public sealed class UseCollectionExpressionForCreateTests
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
    public Task TestNotInCSharp11()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Immutable;

                class C
                {
                    MyCollection<int> i = MyCollection.Create(1, 2, 3);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestInCSharp12_Net70()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1, 2, 3);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestEmpty()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|]<int>(|]);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCast()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        var i = (MyCollection<int>)[|MyCollection.[|Create|]<int>(|]);
                    }
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        var i = (MyCollection<int>)[];
                    }
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestIdentifierCast()
        => new VerifyCS.Test
        {
            TestCode = """
                using X = MyCollection<int>;

                class C
                {
                    void M()
                    {
                        var i = (X)MyCollection.Create<int>();
                    }
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestOneElement()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestTwoElements()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1, 2);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestThreeElements()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1, 2, 3);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestFourElements()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1, 2, 3, 4);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3, 4];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestParamsWithMultipleElements()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1, 2, 3, 4, 5);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3, 4, 5];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestParamsWithExplicitArrayArgument1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = MyCollection.Create(new int[5]);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestParamsWithExplicitArrayArgument2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]new int[] { });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestParamsWithExplicitArrayArgument3()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]new int[] { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestParamsWithImplicitArrayArgument1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]new[] { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestReadOnlySpan_ExplicitStackAlloc_Net70()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = MyCollection.Create<int>(stackalloc int[] { 1 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestReadOnlySpan_ImplicitStackAlloc_Net70()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = MyCollection.Create<int>(stackalloc[] { 1 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestReadOnlySpan_ExplicitStackAlloc_Net80_1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|]<int>(|]stackalloc int[] { });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReadOnlySpan_ExplicitStackAlloc_Net80_2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|]<int>(|]stackalloc int[] { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestReadOnlySpan_ImplicitStackAlloc_Net80_1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|]<int>(|]stackalloc[] { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_Null()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = MyCollection.CreateRange<int>(null);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_ComputedExpression()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]GetValues());

                    static IEnumerable<int> GetValues() => default;
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [.. GetValues()];

                    static IEnumerable<int> GetValues() => default;
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_ExplicitArray1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new int [5]);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [.. new int [5]];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_ExplicitArray2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new int[] { });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_ExplicitArray3()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new int[] { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_ImplicitArray1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new[] { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewObjectWithArgument()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = MyCollection.CreateRange(new List<int>(capacity: 0));
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewObjectWithArgument2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = MyCollection.CreateRange(new List<int>(capacity: 0) { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewObjectWithoutArgument()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new List<int>());
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    MyCollection<int> i = [];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewObjectWithInitializer1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new List<int> { });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    MyCollection<int> i = [];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewObjectWithInitializer2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new List<int>() { });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    MyCollection<int> i = [];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewObjectWithInitializer3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new List<int> { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewObjectWithInitializer4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|](|]new List<int> { 1, 2, 3 });
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    MyCollection<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestCreateRange_NewImplicitObject()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [|MyCollection.[|CreateRange|]<int>(|]{|CS0144:new() { }|});
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = [.. {|CS8754:new() { }|}];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestInterfaceDestination()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    // Will start working once we allow reference conversions.
                    IMyCollection<int> i = {|CS0266:MyCollection.Create(1, 2, 3)|};
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestTrivia1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = /*leading*/ [|MyCollection.[|Create|](|]1) /*trailing*/;
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = /*leading*/ [1] /*trailing*/;
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestMultiLine1()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1 +
                        2);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i = [1 +
                        2];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact]
    public Task TestMultiLine2()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = [|MyCollection.[|Create|](|]1 +
                        2,
                        3 +
                            4);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                class C
                {
                    MyCollection<int> i =
                    [
                        1 +
                            2,
                        3 +
                            4,
                    ];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public Task NotForImmutableArrayNet70()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<int> v = ImmutableArray.Create(1, 2, 3);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public Task ForImmutableArrayNet80()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<int> v = [|ImmutableArray.[|Create|](|]1, 2, 3);
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<int> v = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507"), WorkItem("https://github.com/dotnet/roslyn/issues/69521")]
    public Task NotForImmutableListNet70()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableList<int> v = ImmutableList.Create(1, 2, 3);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public Task ForImmutableListNet80()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableList<int> v = [|ImmutableList.[|Create|](|]1, 2, 3);
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableList<int> v = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact]
    public Task TestGlobalStatement1()
        => new VerifyCS.Test
        {
            TestCode = """
                MyCollection<int> i = [|MyCollection.[|Create|]<int>(|]);
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                MyCollection<int> i = [];
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();

    [Fact]
    public Task TestGlobalStatement2()
        => new VerifyCS.Test
        {
            TestCode = """
                MyCollection<int> i = [|MyCollection.[|Create|]<int>(|]1 +
                    2, 3 +
                    4);
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                MyCollection<int> i =
                [
                    1 +
                        2,
                    3 +
                        4,
                ];
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();

    [Fact]
    public Task TestNested1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<ImmutableArray<int>> v = [|ImmutableArray.[|Create|](|]ImmutableArray.Create(1, 2, 3));
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<ImmutableArray<int>> v = [[1, 2, 3]];
                    }
                }
                """,
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
            TestCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<ImmutableArray<int>> v = [|ImmutableArray.[|Create|](|]ImmutableArray.Create(1, 2, 3));
                    }
                }
                """.ReplaceLineEndings(endOfLine),
            FixedCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        ImmutableArray<ImmutableArray<int>> v = [[1, 2, 3]];
                    }
                }
                """.ReplaceLineEndings(endOfLine),
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            NumberOfFixAllIterations = 2,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestInLambda()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<ImmutableArray<int>> f = () => [|ImmutableArray.[|Create|](|]1, 2, 3);
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<ImmutableArray<int>> f = () => [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestNotInLambda1()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        var f = () => ImmutableArray.Create(1, 2, 3);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestNotInExpressionTree()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Expression<Func<ImmutableArray<int>>> f = () => ImmutableArray.Create(1, 2, 3);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70998")]
    public Task ForMismatchedTupleNames()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<(int A, int B)> M()
                    {
                        return [|ImmutableArray.[|Create|](|](A: 1, 2));
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Immutable;

                class C
                {
                    ImmutableArray<(int A, int B)> M()
                    {
                        return [(A: 1, 2)];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public Task TestInterfaceOn()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> i = [|MyCollection.[|Create|](|]1, 2, 3);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> i = [1, 2, 3];
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public Task TestInterfaceOff()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    IEnumerable<int> i = MyCollection.Create(1, 2, 3);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75870")]
    public Task TestIEnumerablePassedToCreateRange()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    ImmutableArray<string> GetFormattedRange()
                    {
                        return [|ImmutableArray.[|CreateRange|](|]Enumerable.Range(1, 10).Select(n => $"Item {n}"));
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    ImmutableArray<string> GetFormattedRange()
                    {
                        return [.. Enumerable.Range(1, 10).Select(n => $"Item {n}")];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
}
