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
    public Task TestRecommendTypesWithoutPartial()
        => VerifyItemIsAbsentAsync("""
            class C { }

            partial class $$
            """, "C");

    [Fact]
    public Task TestPartialClass1()
        => VerifyItemExistsAsync("""
            partial class C { }

            partial class $$
            """, "C");

    [Fact]
    public Task TestPartialGenericClass1()
        => VerifyItemExistsAsync("""
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """, "C<Bar>");

    [Fact]
    public Task TestPartialGenericClassCommitOnParen()
        => VerifyProviderCommitAsync("""
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """, "C<Bar>", """
            class Bar { }

            partial class C<Bar> { }

            partial class C<
            """, '<');

    [Fact]
    public Task TestPartialGenericClassCommitOnTab()
        => VerifyProviderCommitAsync("""
            class Bar { }

            partial class C<Bar> { }

            partial class $$
            """, "C<Bar>", """
            class Bar { }

            partial class C<Bar> { }

            partial class C<Bar>
            """, null);

    [Fact]
    public Task TestPartialGenericClassCommitOnSpace()
        => VerifyProviderCommitAsync("""
            partial class C<T> { }

            partial class $$
            """, "C<T>", """
            partial class C<T> { }

            partial class C<T> 
            """, ' ');

    [Fact]
    public Task TestPartialClassWithModifiers()
        => VerifyItemExistsAsync("""
            partial class C { }

            internal partial class $$
            """, "C");

    [Fact]
    public Task TestPartialStruct()
        => VerifyItemExistsAsync("""
            partial struct S { }

            partial struct $$
            """, "S");

    [Fact]
    public Task TestPartialInterface()
        => VerifyItemExistsAsync("""
            partial interface I { }

            partial interface $$
            """, "I");

    [Fact]
    public Task TestTypeKindMatches1()
        => VerifyNoItemsExistAsync("""
            partial struct S { }

            partial class $$
            """);

    [Fact]
    public Task TestTypeKindMatches2()
        => VerifyNoItemsExistAsync("""
            partial class C { }

            partial struct $$
            """);

    [Fact]
    public Task TestPartialClassesInSameNamespace()
        => VerifyItemExistsAsync("""
            namespace N
            {
                partial class Goo { }
            }

            namespace N
            {
                partial class $$
            }
            """, "Goo");

    [Fact]
    public Task TestNotPartialClassesAcrossDifferentNamespaces()
        => VerifyNoItemsExistAsync("""
            namespace N
            {
                partial class Goo { }
            }

            partial class $$
            """);

    [Fact]
    public Task TestNotPartialClassesInOuterNamespaces()
        => VerifyNoItemsExistAsync("""
            partial class C { }

            namespace N
            {
                partial class $$
            }
            """);

    [Fact]
    public Task TestNotPartialClassesInOuterClass()
        => VerifyNoItemsExistAsync("""
            partial class C
            {
                partial class $$
            }
            """);

    [Fact]
    public Task TestClassWithConstraint()
        => VerifyProviderCommitAsync("""
            partial class C1<T> where T : System.Exception { }

            partial class $$
            """, "C1<T>", """
            partial class C1<T> where T : System.Exception { }

            partial class C1<T>
            """, null);

    [Fact]
    public Task TestDoNotSuggestCurrentMember()
        => VerifyNoItemsExistAsync(@"partial class F$$");

    [Fact]
    public Task TestNotInTrivia()
        => VerifyNoItemsExistAsync("""
            partial class C1 { }

            partial class //$$
            """);

    [Fact]
    public Task TestPartialClassWithReservedName()
        => VerifyProviderCommitAsync("""
            partial class @class { }

            partial class $$
            """, "@class", """
            partial class @class { }

            partial class @class
            """, null);

    [Fact]
    public Task TestPartialGenericClassWithReservedName()
        => VerifyProviderCommitAsync("""
            partial class @class<T> { }

            partial class $$
            """, "@class<T>", """
            partial class @class<T> { }

            partial class @class<T>
            """, null);

    [Fact]
    public Task TestPartialGenericInterfaceWithVariance()
        => VerifyProviderCommitAsync("""
            partial interface I<out T> { }

            partial interface $$
            """, "I<out T>", """
            partial interface I<out T> { }

            partial interface I<out T>
            """, null);
}
