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
public sealed class PartialTypeCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    internal override Type GetCompletionProviderType()
        => typeof(PartialTypeCompletionProvider);

    [Fact]
    public async Task TestRecommendTypesWithoutPartial()
    {
        await VerifyItemIsAbsentAsync("""
            class C { }

            partial class $$
            """, "C");
    }

    [Fact]
    public async Task TestPartialClass1()
    {
        await VerifyItemExistsAsync("""
            partial class C { }

            partial class $$
            """, "C");
    }

    [Fact]
    public async Task TestPartialGenericClass1()
    {
        await VerifyItemExistsAsync("""
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """, "C<Bar>");
    }

    [Fact]
    public async Task TestPartialGenericClassCommitOnParen()
    {
        await VerifyProviderCommitAsync("""
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """, "C<Bar>", """
            class Bar { }

            partial class C<Bar> { }

            partial class C<
            """, '<');
    }

    [Fact]
    public async Task TestPartialGenericClassCommitOnTab()
    {
        await VerifyProviderCommitAsync("""
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """, "C<Bar>", """
            class Bar { }

            partial class C<Bar> { }

            partial class C<Bar>
            """, null);
    }

    [Fact]
    public async Task TestPartialGenericClassCommitOnSpace()
    {
        await VerifyProviderCommitAsync("""
            partial class C<T> { }

            partial class $$
            """, "C<T>", """
            partial class C<T> { }

            partial class C<T> 
            """, ' ');
    }

    [Fact]
    public async Task TestPartialClassWithModifiers()
    {
        await VerifyItemExistsAsync("""
            partial class C { }

            internal partial class $$
            """, "C");
    }

    [Fact]
    public async Task TestPartialStruct()
    {
        await VerifyItemExistsAsync("""
            partial struct S { }

            partial struct $$
            """, "S");
    }

    [Fact]
    public async Task TestPartialInterface()
    {
        await VerifyItemExistsAsync("""
            partial interface I { }

            partial interface $$
            """, "I");
    }

    [Fact]
    public async Task TestTypeKindMatches1()
    {
        await VerifyNoItemsExistAsync("""
            partial struct S { }

            partial class $$
            """);
    }

    [Fact]
    public async Task TestTypeKindMatches2()
    {
        await VerifyNoItemsExistAsync("""
            partial class C { }

            partial struct $$
            """);
    }

    [Fact]
    public async Task TestPartialClassesInSameNamespace()
    {
        await VerifyItemExistsAsync("""
            namespace N
            {
                partial class Goo { }
            }

            namespace N
            {
                partial class $$
            }
            """, "Goo");
    }

    [Fact]
    public async Task TestNotPartialClassesAcrossDifferentNamespaces()
    {
        await VerifyNoItemsExistAsync("""
            namespace N
            {
                partial class Goo { }
            }

            partial class $$
            """);
    }

    [Fact]
    public async Task TestNotPartialClassesInOuterNamespaces()
    {
        await VerifyNoItemsExistAsync("""
            partial class C { }

            namespace N
            {
                partial class $$
            }
            """);
    }

    [Fact]
    public async Task TestNotPartialClassesInOuterClass()
    {
        await VerifyNoItemsExistAsync("""
            partial class C
            {
                partial class $$
            }
            """);
    }

    [Fact]
    public async Task TestClassWithConstraint()
    {
        await VerifyProviderCommitAsync("""
            partial class C1<T> where T : System.Exception { }

            partial class $$
            """, "C1<T>", """
            partial class C1<T> where T : System.Exception { }

            partial class C1<T>
            """, null);
    }

    [Fact]
    public async Task TestDoNotSuggestCurrentMember()
    {
        await VerifyNoItemsExistAsync(@"partial class F$$");
    }

    [Fact]
    public async Task TestNotInTrivia()
    {
        await VerifyNoItemsExistAsync("""
            partial class C1 { }

            partial class //$$
            """);
    }

    [Fact]
    public async Task TestPartialClassWithReservedName()
    {
        await VerifyProviderCommitAsync("""
            partial class @class { }

            partial class $$
            """, "@class", """
            partial class @class { }

            partial class @class
            """, null);
    }

    [Fact]
    public async Task TestPartialGenericClassWithReservedName()
    {
        await VerifyProviderCommitAsync("""
            partial class @class<T> { }

            partial class $$
            """, "@class<T>", """
            partial class @class<T> { }

            partial class @class<T>
            """, null);
    }

    [Fact]
    public async Task TestPartialGenericInterfaceWithVariance()
    {
        await VerifyProviderCommitAsync("""
            partial interface I<out T> { }

            partial interface $$
            """, "I<out T>", """
            partial interface I<out T> { }

            partial interface I<out T>
            """, null);
    }
}
