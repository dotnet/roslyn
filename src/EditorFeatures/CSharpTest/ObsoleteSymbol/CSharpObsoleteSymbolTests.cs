// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ObsoleteSymbol;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ObsoleteSymbol;

public sealed class CSharpObsoleteSymbolTests : AbstractObsoleteSymbolTests
{
    protected override EditorTestWorkspace CreateWorkspace(string markup)
        => EditorTestWorkspace.CreateCSharp(markup);

    private new Task TestAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup)
        => base.TestAsync(markup);

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    [InlineData("interface")]
    [InlineData("enum")]
    public Task TestObsoleteTypeDefinition(string keyword)
        => TestAsync(
            $$"""
            [System.Obsolete]
            {{keyword}} [|ObsoleteType|]
            {
            }

            {{keyword}} NonObsoleteType
            {
            }
            """);

    [Fact]
    public Task TestObsoleteDelegateTypeDefinition()
        => TestAsync(
            """
            [System.Obsolete]
            delegate void [|ObsoleteType|]();

            delegate void NonObsoleteType();
            """);

    [Fact]
    public Task TestDeclarationAndUseOfObsoleteAlias()
        => TestAsync(
            """
            using [|ObsoleteAlias|] = [|ObsoleteType|];

            [System.Obsolete]
            class [|ObsoleteType|];

            /// <seealso cref="[|ObsoleteType|]"/>
            /// <seealso cref="[|ObsoleteAlias|]"/>
            class NonObsoleteType
            {
                [|ObsoleteAlias|] field = new [|ObsoleteType|]();
            }
            """);

    [Fact]
    public Task TestParametersAndReturnTypes()
        => TestAsync(
            """
            [System.Obsolete]
            class [|ObsoleteType|];

            class NonObsoleteType([|ObsoleteType|] field2)
            {
                [|ObsoleteType|] Method([|ObsoleteType|] arg) => [|new|]();

                System.Func<[|ObsoleteType|], [|ObsoleteType|]> field = [|ObsoleteType|] ([|ObsoleteType|] arg) => [|new|]();
            }
            """);

    [Fact]
    public Task TestImplicitType()
        => TestAsync(
            """
            [System.Obsolete]
            class [|ObsoleteType|]
            {
                public ObsoleteType() { }

                [System.Obsolete]
                public [|ObsoleteType|](int x) { }
            }

            class ObsoleteCtor
            {
                public ObsoleteCtor() { }
            
                [System.Obsolete]
                public [|ObsoleteCtor|](int x) { }
            }
            
            class C
            {
                void Method()
                {
                    [|var|] t1 = new [|ObsoleteType|]();
                    [|var|] t2 = [|new|] [|ObsoleteType|](3);
                    [|ObsoleteType|] t3 = [|new|]();
                    [|ObsoleteType|] t4 = [|new|](3);
                    [|var|] t5 = CreateObsoleteType();
                    var t6 = nameof([|ObsoleteType|]);

                    var u1 = new ObsoleteCtor();
                    var u2 = [|new|] ObsoleteCtor(3);
                    ObsoleteCtor u3 = new();
                    ObsoleteCtor u4 = [|new|](3);
                    var u6 = nameof(ObsoleteCtor);

                    [|ObsoleteType|] CreateObsoleteType() => [|new|]();
                }
            }
            """);

    [Fact]
    public Task TestExtensionMethods()
        => TestAsync(
            """
            [System.Obsolete]
            static class [|ObsoleteType|]
            {
                public static void ObsoleteMember1(this C ignored) { }

                [System.Obsolete]
                public static void [|ObsoleteMember2|](this C ignored) { }
            }

            class C
            {
                void Method()
                {
                    this.ObsoleteMember1();
                    this.[|ObsoleteMember2|]();
                    [|ObsoleteType|].ObsoleteMember1(this);
                    [|ObsoleteType|].[|ObsoleteMember2|](this);
                }
            }
            """);

    [Fact]
    public Task TestGenerics()
        => TestAsync(
            """
            [System.Obsolete]
            class [|ObsoleteType|];

            [System.Obsolete]
            struct [|ObsoleteValueType|];

            class G<T>
            {
            }

            class C
            {
                void M<T>() { }

                /// <summary>
                /// This looks like a reference to an obsolete type, but it's actually just an identifier alias for the
                /// generic type parameter 'T'.
                /// </summary>
                /// <seealso cref="G{ObsoleteType}"/>
                void Method()
                {
                    _ = new G<[|ObsoleteType|]>();
                    _ = new G<G<[|ObsoleteType|]>>();
                    M<[|ObsoleteType|]>();
                    M<G<[|ObsoleteType|]>>();
                    M<G<G<[|ObsoleteType|]>>>();

                    // Mark 'var' as obsolete even when it points to Nullable<T> where T is obsolete
                    [|var|] nullableValue = CreateNullableValueType();

                    [|ObsoleteValueType|]? CreateNullableValueType() => new [|ObsoleteValueType|]();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79413")]
    public Task TestObsoleteFeatureAttribute()
        => TestAsync(
            """
            [System.Obsolete]
            [System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute("This is an obsolete feature.")]
            static class ObsoleteType
            {
            }
            """);
}
