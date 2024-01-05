// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class UsingKeywordRecommenderTests : KeywordRecommenderTests
    {
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
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterAwait()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    async void M()
                    {
                        await $$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterAwaitInAssignment()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    async void M()
                    {
                        _ = await $$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestNotAfterUsingKeyword()
            => await VerifyAbsenceAsync(@"using $$");

        [Fact]
        public async Task TestAfterPreviousUsing()
        {
            await VerifyKeywordAsync(
                """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(
                """
                extern alias goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalAfterExtern()
        {
            await VerifyKeywordAsync(
                """
                extern alias goo;
                global $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalAfterExternBeforeUsing_01()
        {
            await VerifyKeywordAsync(
                """
                extern alias goo;
                global $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterGlobalAfterExternBeforeUsing_02()
        {
            await VerifyKeywordAsync(
                """
                extern alias goo;
                global $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestBeforeUsing()
        {
            await VerifyKeywordAsync(
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestBeforeUsingAfterGlobal()
        {
            await VerifyKeywordAsync(
                """
                global $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterUsingAlias()
        {
            await VerifyKeywordAsync(
                """
                using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsingAlias()
        {
            await VerifyKeywordAsync(
                """
                global using Goo = Bar;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedTypeDeclaration()
        {
            await VerifyAbsenceAsync("""
                class A {
                    class C {}
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
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N;
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterUsingKeyword_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    using $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousUsing_InsideNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                   using Goo;
                   $$
                """);
        }

        [Fact]
        public async Task TestBeforeUsing_InsideNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                    $$
                    using Goo;
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
        public async Task TestNotAfterNestedMember_InsideNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace N {
                    class A {
                      class C {}
                      $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeExtern()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                extern alias Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeExtern_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                extern alias Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeExternAfterGlobal()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                global $$
                extern alias Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeExternAfterGlobal_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                global $$
                extern alias Goo;
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBeforeStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                $$
                return true;
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                return true;
                $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterBlock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (true) {
                }
                $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterIf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (true) 
                    $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterDo(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                do 
                    $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterWhile(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                while (true) 
                    $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterFor(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                for (int i = 0; i < 10; i++) 
                    $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterForeach(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                foreach (var v in bar)
                    $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotAfterUsing()
        {
            await VerifyAbsenceAsync(
@"using $$");
        }

        [Fact]
        public async Task TestNotAfterGlobalUsing()
        {
            await VerifyAbsenceAsync(
@"global using $$");
        }

        [Fact]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                  $$
                }
                """);
        }

        [Fact]
        public async Task TestBetweenUsings_01()
        {
            await VerifyKeywordAsync(
                """
                using Goo;
                $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestBetweenUsings_02()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestAfterGlobalBetweenUsings_01()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                global $$
                using Bar;
                """);
        }

        [Fact]
        public async Task TestAfterGlobalBetweenUsings_02()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                global $$
                global using Bar;
                """);
        }

        [Fact]
        public async Task TestAfterGlobal()
        {
            await VerifyKeywordAsync(
@"global $$");
        }

        [Fact]
        public async Task TestBeforeNamespace()
        {
            await VerifyKeywordAsync(
                """
                $$
                namespace NS
                {}
                """);
        }

        [Fact]
        public async Task TestBeforeFileScopedNamespace()
        {
            await VerifyKeywordAsync(
                """
                $$
                namespace NS;
                """);
        }

        [Fact]
        public async Task TestBeforeClass()
        {
            await VerifyKeywordAsync(
                """
                $$
                class C1
                {}
                """);
        }

        [Fact]
        public async Task TestBeforeAttribute_01()
        {
            await VerifyKeywordAsync(
                """
                $$
                [Call()]
                """);
        }

        [Fact]
        public async Task TestBeforeAttribute_02()
        {
            await VerifyKeywordAsync(
                """
                $$
                [assembly: Call()]
                """);
        }

        [Fact]
        public async Task TestBeforeNamespaceAfterGlobal()
        {
            await VerifyKeywordAsync(
                """
                global $$
                namespace NS
                {}
                """);
        }

        [Fact]
        public async Task TestBeforeClassAfterGlobal()
        {
            await VerifyKeywordAsync(
                """
                global $$
                class C1
                {}
                """);
        }

        [Fact]
        public async Task TestBeforeStatementAfterGlobal()
        {
            await VerifyKeywordAsync(
                """
                global $$
                Call();
                """);
        }

        [Fact]
        public async Task TestBeforeAttributeAfterGlobal_01()
        {
            await VerifyKeywordAsync(
                """
                global $$
                [Call()]
                """);
        }

        [Fact]
        public async Task TestBeforeAttributeAfterGlobal_02()
        {
            await VerifyKeywordAsync(
                """
                global $$
                [assembly: Call()]
                """);
        }
    }
}
