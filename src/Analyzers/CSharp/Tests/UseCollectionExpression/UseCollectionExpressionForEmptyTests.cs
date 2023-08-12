// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
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
                    object[] v = Array.Empty<string>();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty5()
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
}
