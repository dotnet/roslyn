// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class PartialTypeCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(PartialTypeCompletionProvider);

    [Fact]
    public async Task TestRecommendTypesWithoutPartial()
    {
        var text = """
            class C { }

            partial class $$
            """;

        await VerifyItemIsAbsentAsync(text, "C");
    }

    [Fact]
    public async Task TestPartialClass1()
    {
        var text = """
            partial class C { }

            partial class $$
            """;

        await VerifyItemExistsAsync(text, "C");
    }

    [Fact]
    public async Task TestPartialGenericClass1()
    {
        var text = """
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """;

        await VerifyItemExistsAsync(text, "C<Bar>");
    }

    [Fact]
    public async Task TestPartialGenericClassCommitOnParen()
    {
        var text = """
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """;

        var expected = """
            class Bar { }

            partial class C<Bar> { }

            partial class C<
            """;

        await VerifyProviderCommitAsync(text, "C<Bar>", expected, '<');
    }

    [Fact]
    public async Task TestPartialGenericClassCommitOnTab()
    {
        var text = """
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """;

        var expected = """
            class Bar { }

            partial class C<Bar> { }

            partial class C<Bar>
            """;

        await VerifyProviderCommitAsync(text, "C<Bar>", expected, null);
    }

    [Fact]
    public async Task TestPartialGenericClassCommitOnSpace()
    {
        var text = """
            partial class C<T> { }

            partial class $$
            """;

        var expected = """
            partial class C<T> { }

            partial class C<T> 
            """;

        await VerifyProviderCommitAsync(text, "C<T>", expected, ' ');
    }

    [Fact]
    public async Task TestPartialClassWithModifiers()
    {
        var text = """
            partial class C { }

            internal partial class $$
            """;

        await VerifyItemExistsAsync(text, "C");
    }

    [Fact]
    public async Task TestPartialStruct()
    {
        var text = """
            partial struct S { }

            partial struct $$
            """;

        await VerifyItemExistsAsync(text, "S");
    }

    [Fact]
    public async Task TestPartialInterface()
    {
        var text = """
            partial interface I { }

            partial interface $$
            """;

        await VerifyItemExistsAsync(text, "I");
    }

    [Fact]
    public async Task TestTypeKindMatches1()
    {
        var text = """
            partial struct S { }

            partial class $$
            """;

        await VerifyNoItemsExistAsync(text);
    }

    [Fact]
    public async Task TestTypeKindMatches2()
    {
        var text = """
            partial class C { }

            partial struct $$
            """;

        await VerifyNoItemsExistAsync(text);
    }

    [Fact]
    public async Task TestPartialClassesInSameNamespace()
    {
        var text = """
            namespace N
            {
                partial class Goo { }
            }

            namespace N
            {
                partial class $$
            }
            """;

        await VerifyItemExistsAsync(text, "Goo");
    }

    [Fact]
    public async Task TestNotPartialClassesAcrossDifferentNamespaces()
    {
        var text = """
            namespace N
            {
                partial class Goo { }
            }

            partial class $$
            """;

        await VerifyNoItemsExistAsync(text);
    }

    [Fact]
    public async Task TestNotPartialClassesInOuterNamespaces()
    {
        var text = """
            partial class C { }

            namespace N
            {
                partial class $$
            }
            """;

        await VerifyNoItemsExistAsync(text);
    }

    [Fact]
    public async Task TestNotPartialClassesInOuterClass()
    {
        var text = """
            partial class C
            {
                partial class $$
            }
            """;

        await VerifyNoItemsExistAsync(text);
    }

    [Fact]
    public async Task TestClassWithConstraint()
    {
        var text = """
            partial class C1<T> where T : System.Exception { }

            partial class $$
            """;

        var expected = """
            partial class C1<T> where T : System.Exception { }

            partial class C1<T>
            """;

        await VerifyProviderCommitAsync(text, "C1<T>", expected, null);
    }

    [Fact]
    public async Task TestDoNotSuggestCurrentMember()
    {
        var text = @"partial class F$$";

        await VerifyNoItemsExistAsync(text);
    }

    [Fact]
    public async Task TestNotInTrivia()
    {
        var text = """
            partial class C1 { }

            partial class //$$
            """;

        await VerifyNoItemsExistAsync(text);
    }

    [Fact]
    public async Task TestPartialClassWithReservedName()
    {
        var text = """
            partial class @class { }

            partial class $$
            """;

        var expected = """
            partial class @class { }

            partial class @class
            """;

        await VerifyProviderCommitAsync(text, "@class", expected, null);
    }

    [Fact]
    public async Task TestPartialGenericClassWithReservedName()
    {
        var text = """
            partial class @class<T> { }

            partial class $$
            """;

        var expected = """
            partial class @class<T> { }

            partial class @class<T>
            """;

        await VerifyProviderCommitAsync(text, "@class<T>", expected, null);
    }

    [Fact]
    public async Task TestPartialGenericInterfaceWithVariance()
    {
        var text = """
            partial interface I<out T> { }

            partial interface $$
            """;

        var expected = """
            partial interface I<out T> { }

            partial interface I<out T>
            """;

        await VerifyProviderCommitAsync(text, "I<out T>", expected, null);
    }
}
