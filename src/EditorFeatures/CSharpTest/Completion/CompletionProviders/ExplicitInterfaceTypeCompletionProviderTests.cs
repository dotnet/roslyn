// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class ExplicitInterfaceTypeCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(ExplicitInterfaceTypeCompletionProvider);

    [Fact]
    public async Task TestAtStartOfClass()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                int $$
            }
            """;
        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Theory]
    [InlineData("record")]
    [InlineData("record class")]
    [InlineData("record struct")]
    public async Task TestAtStartOfRecord(string record)
    {
        var markup = $$"""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" LanguageVersion="Preview">
                    <Document>
            using System.Collections;

            {{record}} C : IList
            {
                int $$
            }
                    </Document>
                </Project>
            </Workspace>
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=459044")]
    public Task TestInMisplacedUsing()
        => VerifyNoItemsExistAsync("""
            class C
            {
                using ($$)
            }
            """); // no crash

    [Fact]
    public async Task TestAtStartOfStruct()
    {
        var markup = """
            using System.Collections;

            struct C : IList
            {
                int $$
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestAfterField()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                int i;
                int $$
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestAfterMethod_01()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                void Goo() { }
                int $$
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestAfterMethod_02()
    {
        var markup = """
            using System.Collections;

            interface C : IList
            {
                void Goo() { }
                int $$
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestAfterExpressionBody()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                int Goo() => 0;
                int $$
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestWithAttributeFollowing()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                int Goo() => 0;
                int $$

                [Attr]
                int Bar();
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestWithModifierFollowing()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                int Goo() => 0;
                int $$

                public int Bar();
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestWithTypeFollowing()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                int Goo() => 0;
                int $$

                int Bar();
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestWithTypeFollowing2()
    {
        var markup = """
            using System.Collections;

            class C : IList
            {
                int Goo() => 0;
                int $$

                X Bar();
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public Task NotInMember()
        => VerifyNoItemsExistAsync("""
            using System.Collections;

            class C : IList
            {
                void Goo()
                {
                    int $$
                }
            }
            """);

    [Fact]
    public Task NotWithAccessibility()
        => VerifyNoItemsExistAsync("""
            using System.Collections;

            class C : IList
            {
                public int $$
            }
            """);

    [Fact]
    public async Task TestInInterface()
    {
        var markup = """
            using System.Collections;

            interface I : IList
            {
                int $$
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IEnumerable");
        await VerifyItemExistsAsync(markup, "ICollection");
        await VerifyItemExistsAsync(markup, "IList");
    }

    [Fact]
    public async Task TestImplementedAsAsync()
    {
        var markup = """
            interface IGoo
            {
                Task Goo();
            }

            class MyGoo : IGoo
            {
                 async Task $$
            }
            """;

        await VerifyAnyItemExistsAsync(markup, hasSuggestionModeItem: true);
        await VerifyItemExistsAsync(markup, "IGoo");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70382")]
    public Task TestAfterGenericType()
        => VerifyItemExistsAsync("""
            interface I<T>
            {
                I<T> M();
            }

            class C<T> : I<T>
            {
                 I<T> $$
            }
            """, "I", displayTextSuffix: "<>");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70382")]
    public Task TestAfterNestedGenericType()
        => VerifyItemExistsAsync("""
            interface I<T>
            {
                I<T> M();
            }

            class C<T> : I<T>
            {
                 I<I<T>> $$
            }
            """, "I", displayTextSuffix: "<>");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54005")]
    public Task TestWithStaticKeyword()
        => VerifyItemExistsAsync("""
            interface I1
            {
                static abstract void M1();
            }

            class C1 : I1
            {
                static void $$
            }
            """, "I1", displayTextSuffix: "");
}
