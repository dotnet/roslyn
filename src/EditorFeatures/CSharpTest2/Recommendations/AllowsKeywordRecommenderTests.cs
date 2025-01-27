// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class AllowsKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot()
        {
            await VerifyAbsenceAsync("$$");
        }

        [Fact]
        public async Task TestNotAfterClassDeclaration()
        {
            await VerifyAbsenceAsync(
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement()
        {
            await VerifyAbsenceAsync(
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration()
        {
            await VerifyAbsenceAsync(
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
                """
                using Goo = $$
                """);
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
                """
                global using Goo = $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotEmptyStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod("$$", topLevelStatement: topLevelStatement));
        }

        [Fact]
        public async Task TestAfterNewTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : $$
                """);
        }

        [Fact]
        public async Task TestAfterTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C<T>
                    where T : $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestAfterMethodTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : $$
                      where U : T
                """);
        }

        [Fact]
        public async Task TestNotAfterClassTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : class, $$
                """);
        }

        [Fact]
        public async Task TestAfterStructTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : struct, $$
                """);
        }

        [Fact]
        public async Task TestAfterSimpleTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : IGoo, $$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : new(), $$
                """);
        }

        [Fact]
        public async Task TestNotAfterMethodInClass()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync("class $$");
    }
}
