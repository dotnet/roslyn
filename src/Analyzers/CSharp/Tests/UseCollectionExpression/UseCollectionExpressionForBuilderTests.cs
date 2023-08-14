// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCollectionExpression;

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
}
