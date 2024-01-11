// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ExternKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestAfterClass()
        {
            await VerifyKeywordAsync(
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement()
        {
            await VerifyKeywordAsync(
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            await VerifyKeywordAsync(
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterStaticInStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterAttributesInStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Attr] $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterAttributesInSwitchCase(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (c)
                {
                    case 0:
                         [Goo]
                         $$
                }
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterAttributesAndStaticInStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Attr] static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndReturnStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                [Attr]
                $$
                return x;
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndLocalDeclarationStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                [Attr]
                $$
                x y = bar();
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndAwaitExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                [Attr]
                $$
                await bar;
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndAssignmentStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                [Goo]
                $$
                y = bar();
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndCallStatement1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                [Goo]
                $$
                bar();
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndCallStatement2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                [Goo1]
                [Goo2]
                $$
                bar();
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterExternInStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"extern $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotAfterExternKeyword()
            => await VerifyAbsenceAsync(@"extern $$");

        [Fact]
        public async Task TestAfterPreviousExternAlias()
        {
            await VerifyKeywordAsync(
                """
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync("""
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync("""
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestInsideNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                    $$
                """);
        }

        [Fact]
        public async Task TestInsideFileScopedNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N;$$");
        }

        [Fact]
        public async Task TestNotAfterExternKeyword_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    extern $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousExternAlias_InsideNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                   extern alias Goo;
                   $$
                """);
        }

        [Fact]
        public async Task TestNotAfterUsing_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    using Goo;
                    $$
                """);
        }

        [Fact]
        public async Task TestNotAfterMember_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    class C {}
                    $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNamespace_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    namespace N {}
                    $$
                """);
        }

        [Fact]
        public async Task TestInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    $$
                """);
        }

        [Fact]
        public async Task TestInStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                    $$
                """);
        }

        [Fact]
        public async Task TestInInterface()
        {
            await VerifyKeywordAsync(
                """
                interface I {
                    $$
                """);
        }

        [Fact]
        public async Task TestNotAfterAbstract()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestNotAfterExtern()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    extern $$
                """);
        }

        [Fact]
        public async Task TestAfterPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public $$
                """);
        }
    }
}
