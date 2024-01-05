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
public class UseCollectionExpressionForCreateTests
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
                    MyCollection<int> i = MyCollection.Create(1, 2, 3);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
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
    }

    [Fact]
    public async Task TestEmpty()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCast()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestIdentifierCast()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestOneElement()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestTwoElements()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestThreeElements()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestFourElements()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestParamsWithMultipleElements()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestParamsWithExplicitArrayArgument1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestParamsWithExplicitArrayArgument2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestParamsWithExplicitArrayArgument3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestParamsWithImplicitArrayArgument1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReadOnlySpan_ExplicitStackAlloc_Net70()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReadOnlySpan_ImplicitStackAlloc_Net70()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReadOnlySpan_ExplicitStackAlloc_Net80_1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReadOnlySpan_ExplicitStackAlloc_Net80_2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestReadOnlySpan_ImplicitStackAlloc_Net80_1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_Null()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_ComputedExpression()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = MyCollection.CreateRange(GetValues());

                    static IEnumerable<int> GetValues() => default;
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCreateRange_ExplicitArray1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    MyCollection<int> i = MyCollection.CreateRange(new int [5]);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCreateRange_ExplicitArray2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_ExplicitArray3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_ImplicitArray1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewObjectWithArgument()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewObjectWithArgument2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewObjectWithoutArgument()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewObjectWithInitializer1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewObjectWithInitializer2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewObjectWithInitializer3()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewObjectWithInitializer4()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestCreateRange_NewImplicitObject()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    MyCollection<int> i = MyCollection.CreateRange<int>({|CS0144:new() { }|]);
                }
                """ + s_collectionBuilderApi + s_basicCollectionApi,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInterfaceDestination()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestMultiLine2()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task NotForImmutableArrayNet70()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task ForImmutableArrayNet80()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507"), WorkItem("https://github.com/dotnet/roslyn/issues/69521")]
    public async Task NotForImmutableListNet70()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task ForImmutableListNet80()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestGlobalStatement1()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestGlobalStatement2()
    {
        await new VerifyCS.Test
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
    }

    [Fact]
    public async Task TestNested1()
    {
        await new VerifyCS.Test
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
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task TestWithDifferentNewLines(string endOfLine)
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestInLambda()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestNotInLambda1()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestNotInExpressionTree()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70998")]
    public async Task ForMismatchedTupleNames()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOn()
    {
        await new VerifyCS.Test
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOff()
    {
        await new VerifyCS.Test
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
    }
}
