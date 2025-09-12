// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class AllowsKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot()
        => VerifyAbsenceAsync("$$");

    [Fact]
    public Task TestNotAfterClassDeclaration()
        => VerifyAbsenceAsync(
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement()
        => VerifyAbsenceAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration()
        => VerifyAbsenceAsync(
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
            """
            using Goo = $$
            """);

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
            """
            global using Goo = $$
            """);

    [Theory]
    [CombinatorialData]
    public Task TestNotEmptyStatement(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod("$$", topLevelStatement: topLevelStatement));

    [Fact]
    public Task TestAfterNewTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : $$
            """);

    [Fact]
    public Task TestAfterTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C<T>
                where T : $$
                where U : U
            """);

    [Fact]
    public Task TestAfterMethodTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : $$
            """);

    [Fact]
    public Task TestAfterMethodTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : $$
                  where U : T
            """);

    [Fact]
    public Task TestNotAfterClassTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : class, $$
            """);

    [Fact]
    public Task TestAfterStructTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : struct, $$
            """);

    [Fact]
    public Task TestAfterSimpleTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : IGoo, $$
            """);

    [Fact]
    public Task TestAfterConstructorTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : new(), $$
            """);

    [Fact]
    public Task TestNotAfterMethodInClass()
        => VerifyAbsenceAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public async Task TestNotAfterClass()
        => await VerifyAbsenceAsync("class $$");
}
