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
    CSharpUseCollectionExpressionForEmptyDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForEmptyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public class UseCollectionExpressionForEmptyTests
{
    private const string CollectionBuilderAttributeDefinition = """

        namespace System.Runtime.CompilerServices
        {
            [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
            public sealed class CollectionBuilderAttribute : Attribute
            {
                public CollectionBuilderAttribute(Type builderType, string methodName) { }
            }
        }
        """;

    [Fact]
    public async Task ArrayEmpty1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    var v = Array.Empty<int>();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = Array.[|Empty|]<int>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty2_A()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = System.Array.[|Empty|]<int>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                class C
                {
                    void M()
                    {
                        object[] v = Array.[|Empty|]<string>();
                    }
                }
                """,
            FixedCode = """
                using System;
                class C
                {
                    void M()
                    {
                        object[] v = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty3_Strict()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            class C
            {
                void M()
                {
                    object[] v = Array.Empty<string>();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty5_InterfacesOn()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        IEnumerable<string> v = Array.[|Empty|]<string>();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        IEnumerable<string> v = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty5_InterfacesOff()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    IEnumerable<string> v = Array.Empty<string>();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    string[] v = Array.[|Empty|]<string>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    string[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string[] v = {|CS8619:Array.[|Empty|]<string?>()|};
                }
            }
            """,
            FixedCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string?[] v = Array.[|Empty|]<string>();
                }
            }
            """,
            FixedCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string?[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty9()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string?[] v = Array.[|Empty|]<string?>();
                }
            }
            """,
            FixedCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string?[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty10()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string[]? v = Array.[|Empty|]<string>();
                }
            }
            """,
            FixedCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    string[]? v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCast()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    var v = (int[])Array.[|Empty|]<int>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    var v = (int[])[];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestIdentifierCast()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using X = int[];

            class C
            {
                void M()
                {
                    var v = (X)Array.Empty<int>();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    int[] v = /*goo*/ Array.[|Empty|]<int>() /*bar*/;
                }
            }
            """,
            FixedCode = """
            #nullable enable
            using System;

            class C
            {
                void M()
                {
                    int[] v = /*goo*/ [] /*bar*/;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNonCollection()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    X x = X.Empty<int>();
                }
            }

            class X
            {
                public static X Empty<T>() => default;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestProperty1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    MyList<int> x = MyList<int>.[|Empty|];
                }
            }

            class MyList<T> : IEnumerable<T>
            {
                public static MyList<T> Empty { get; }

                public void Add(T value) { }

                public IEnumerator<T> GetEnumerator() => default;
            
                IEnumerator IEnumerable.GetEnumerator() => default;
            }
            """,
            FixedCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    MyList<int> x = [];
                }
            }

            class MyList<T> : IEnumerable<T>
            {
                public static MyList<T> Empty { get; }

                public void Add(T value) { }

                public IEnumerator<T> GetEnumerator() => default;

                IEnumerator IEnumerable.GetEnumerator() => default;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestBuilder1()
    {
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            TestCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            class C
            {
                void M()
                {
                    MyList<int> x = MyList<int>.[|Empty|];
                }
            }

            [CollectionBuilder(typeof(MyList), "Create")]
            class MyList<T> : IEnumerable<T>
            {
                public static MyList<T> Empty { get; }

                public IEnumerator<T> GetEnumerator() => default;
            
                IEnumerator IEnumerable.GetEnumerator() => default;
            }

            static class MyList
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> values) => default;
            }
            """ + CollectionBuilderAttributeDefinition,
            FixedCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            class C
            {
                void M()
                {
                    MyList<int> x = [];
                }
            }
            
            [CollectionBuilder(typeof(MyList), "Create")]
            class MyList<T> : IEnumerable<T>
            {
                public static MyList<T> Empty { get; }
            
                public IEnumerator<T> GetEnumerator() => default;
            
                IEnumerator IEnumerable.GetEnumerator() => default;
            }
            
            static class MyList
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> values) => default;
            }
            """ + CollectionBuilderAttributeDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestBuilder2()
    {
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            TestCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            class C
            {
                void M()
                {
                    MyList<int> x = MyList<int>.[|Empty|];
                }
            }

            [CollectionBuilder(typeof(MyList), "Create")]
            class MyList<T> : IEnumerable<T>
            {
                public static MyList<T> Empty { get; }

                public IEnumerator<T> GetEnumerator() => default;
            
                IEnumerator IEnumerable.GetEnumerator() => default;
            }

            static class MyList
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> values, int x) => default;
            }
            """ + CollectionBuilderAttributeDefinition,
            FixedCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            class C
            {
                void M()
                {
                    MyList<int> x = {|CS9187:[]|};
                }
            }
            
            [CollectionBuilder(typeof(MyList), "Create")]
            class MyList<T> : IEnumerable<T>
            {
                public static MyList<T> Empty { get; }
            
                public IEnumerator<T> GetEnumerator() => default;
            
                IEnumerator IEnumerable.GetEnumerator() => default;
            }
            
            static class MyList
            {
                public static MyList<T> Create<T>(ReadOnlySpan<T> values, int x) => default;
            }
            """ + CollectionBuilderAttributeDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ReadOnlySpan1()
    {
        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    ReadOnlySpan<int> v = ReadOnlySpan<int>.[|Empty|];
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    ReadOnlySpan<int> v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
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
                    ImmutableArray<int> v = ImmutableArray<int>.Empty;
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
                    ImmutableArray<int> v = ImmutableArray<int>.[|Empty|];
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
                    ImmutableArray<int> v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
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
                        ImmutableList<int> v = ImmutableList<int>.Empty;
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
                        ImmutableList<int> v = ImmutableList<int>.[|Empty|];
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
                        ImmutableList<int> v = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task NotForValueTypeWithoutNoArgConstructorAndWithoutCollectionBuilderAttribute()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.Empty;
                    }
                }

                struct V<T> : IEnumerable<T>
                {
                    public static readonly V<T> Empty = default;

                    public IEnumerator<T> GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;

                    public void Add(T x) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task NotForValueTypeWithOneArgConstructorAndWithoutCollectionBuilderAttribute()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.Empty;
                    }
                }

                struct V<T> : IEnumerable<T>
                {
                    public static readonly V<T> Empty = default;

                    public V(int val) { }

                    public IEnumerator<T> GetEnumerator() => default;
                    IEnumerator IEnumerable.GetEnumerator() => default;

                    public void Add(T x) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task ForValueTypeWithCapacityConstructor()
    {
        var collectionDefinition = """

            struct V<T> : IEnumerable<T>
            {
                public static readonly V<T> Empty = default;
            
                public V(int capacity) { }
            
                public IEnumerator<T> GetEnumerator() => default;
                IEnumerator IEnumerable.GetEnumerator() => default;
            
                public void Add(T x) { }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.[|Empty|];
                    }
                }
                """ + collectionDefinition,
            FixedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = [];
                    }
                }
                """ + collectionDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task NotForValueTypeWithInvalidCapacityConstructor()
    {
        var collectionDefinition = """

            struct V<T> : IEnumerable<T>
            {
                public static readonly V<T> Empty = default;
            
                public V(string capacity) { }
            
                public IEnumerator<T> GetEnumerator() => default;
                IEnumerator IEnumerable.GetEnumerator() => default;
            
                public void Add(T x) { }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.Empty;
                    }
                }
                """ + collectionDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task ForValueTypeWithInvalidCapacityButValidEmptyConstructor()
    {
        var collectionDefinition = """

            struct V<T> : IEnumerable<T>
            {
                public static readonly V<T> Empty = default;
            
                public V(string capacity) { }
                public V() { }
            
                public IEnumerator<T> GetEnumerator() => default;
                IEnumerator IEnumerable.GetEnumerator() => default;
            
                public void Add(T x) { }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.[|Empty|];
                    }
                }
                """ + collectionDefinition,
            FixedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = [];
                    }
                }
                """ + collectionDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task ForValueTypeWithNoArgConstructorAndWithoutCollectionBuilderAttribute()
    {
        var collectionDefinition = """
            
            struct V<T> : IEnumerable<T>
            {
                public static readonly V<T> Empty = default;
            
                public V()
                {
                }
            
                public IEnumerator<T> GetEnumerator() => default;
                IEnumerator IEnumerable.GetEnumerator() => default;
            
                public void Add(T x) { }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.[|Empty|];
                    }
                }
                """ + collectionDefinition,
            FixedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = [];
                    }
                }
                """ + collectionDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task ForValueTypeWithoutNoArgConstructorAndWithCollectionBuilderAttribute()
    {
        var collectionDefinition = """
            
            [System.Runtime.CompilerServices.CollectionBuilder(typeof(V), "Create")]
            struct V<T> : IEnumerable<T>
            {
                public static readonly V<T> Empty = default;
            
                public IEnumerator<T> GetEnumerator() => default;
                IEnumerator IEnumerable.GetEnumerator() => default;
            
                public void Add(T x) { }
            }
            
            static class V
            {
                public static V<T> Create<T>(ReadOnlySpan<T> values) => default;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.[|Empty|];
                    }
                }
                """ + collectionDefinition,
            FixedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = [];
                    }
                }
                """ + collectionDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task NotForAbstractClassWithoutCollectionBuilderAttribute()
    {
        var collectionDefinition = """
            
            abstract class V<T> : IEnumerable<T>
            {
                public static readonly V<T> Empty = null;

                public V() { }
            
                public IEnumerator<T> GetEnumerator() => default;
                IEnumerator IEnumerable.GetEnumerator() => default;
            
                public void Add(T x) { }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.Empty;
                    }
                }
                """ + collectionDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69507")]
    public async Task ForAbstractClassWithCollectionBuilderAttribute()
    {
        var collectionDefinition = """

            [System.Runtime.CompilerServices.CollectionBuilder(typeof(V), "Create")]
            abstract class V<T> : IEnumerable<T>
            {
                public static readonly V<T> Empty = null;

                public V() { }
            
                public IEnumerator<T> GetEnumerator() => default;
                IEnumerator IEnumerable.GetEnumerator() => default;
            
                public void Add(T x) { }
            }
            
            static class V
            {
                public static V<T> Create<T>(ReadOnlySpan<T> values) => default;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = V<int>.[|Empty|];
                    }
                }
                """ + collectionDefinition,
            FixedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        V<int> v = [];
                    }
                }
                """ + collectionDefinition,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWhenChildOfInvocation()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        // Handled by the fluent chain analyzer.
                        List<int> list = Array.Empty<int>().ToList();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGlobalStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            int[] v = Array.[|Empty|]<int>();
            """,
            FixedCode = """
            using System;

            int[] v = [];
            """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
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

                int[] v = Array.[|Empty|]<int>();

                """.ReplaceLineEndings(endOfLine),
            FixedCode = """
                using System;

                int[] v = [];

                """.ReplaceLineEndings(endOfLine),
            LanguageVersion = LanguageVersion.CSharp12,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestForSpanField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                ref struct C
                {
                    private ReadOnlySpan<int> span = Array.[|Empty|]<int>();

                    public C() { }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                ref struct C
                {
                    private ReadOnlySpan<int> span = [];
                
                    public C() { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForSpanProperty1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span => Array.[|Empty|]<int>();
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span => [];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForSpanProperty2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get => Array.[|Empty|]<int>(); }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get => []; }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForSpanProperty3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get { return Array.[|Empty|]<int>(); } }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get { return []; } }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForMethodReturn()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span() => Array.[|Empty|]<int>();
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span() => [];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForMethodLocal1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> span = Array.[|Empty|]<int>();
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> span = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForArgument()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        X(Array.[|Empty|]<int>());
                    }

                    void X(ReadOnlySpan<int> span) { }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        X([]);
                    }
                
                    void X(ReadOnlySpan<int> span) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
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
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<int[]> f = () => Array.[|Empty|]<int>();
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<int[]> f = () => [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        var f = () => Array.Empty<int>();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Expression<Func<int[]>> f = () => Array.Empty<int>();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOn(
        [CombinatorialValues("IEnumerable<int>", "IReadOnlyCollection<int>", "IReadOnlyList<int>")] string type,
        [CombinatorialValues("Array.[|Empty|]<int>()", "ImmutableArray<int>.[|Empty|]")] string expression)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{type}} v = {{expression}};
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{type}} v = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOn_ReadWriteDestination(
        [CombinatorialValues("IList<int>", "ICollection<int>")] string type,
        [CombinatorialValues("Array.Empty<int>()", "ImmutableArray<int>.Empty")] string expression)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                class C
                {
                    void M()
                    {
                        {{type}} v = {{expression}};
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOff()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        IEnumerable<int> v = Array.Empty<int>();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();
    }
}
