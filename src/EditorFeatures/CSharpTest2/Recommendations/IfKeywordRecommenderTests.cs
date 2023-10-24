// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class IfKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
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
        public async Task TestNotInPreprocessor1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
"#if $$"));
        }

        [Fact]
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterHash()
        {
            await VerifyKeywordAsync(
@"#$$");
        }

        [Fact]
        public async Task TestAfterHashFollowedBySkippedTokens()
        {
            await VerifyKeywordAsync(
                """
                #$$
                aeu
                """);
        }

        [Fact]
        public async Task TestAfterHashAndSpace()
        {
            await VerifyKeywordAsync(
@"# $$");
        }

        [Fact]
        public async Task TestInsideMethod()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestBeforeStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                $$
                return true;
                """));
        }

        [Fact]
        public async Task TestAfterStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                return true;
                $$
                """));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (true) {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestNotAfterIf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if $$"));
        }

        [Fact]
        public async Task TestInCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (true) {
                  case 0:
                    $$
                }
                """));
        }

        [Fact]
        public async Task TestInCaseBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (true) {
                  case 0: {
                    $$
                  }
                }
                """));
        }

        [Fact]
        public async Task TestInDefaultCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (true) {
                  default:
                    $$
                }
                """));
        }

        [Fact]
        public async Task TestInDefaultCaseBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (true) {
                  default: {
                    $$
                  }
                }
                """));
        }

        [Fact]
        public async Task TestAfterLabel()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                label:
                  $$
                """));
        }

        [Fact]
        public async Task TestNotAfterDoBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                do {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestInActiveRegion1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                #if true
                $$
                """));
        }

        [Fact]
        public async Task TestInActiveRegion2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                #if true

                $$
                """));
        }

        [Fact]
        public async Task TestAfterElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (goo) {
                } else $$
                """));
        }

        [Fact]
        public async Task TestAfterCatch()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch $$"));
        }

        [Fact]
        public async Task TestAfterCatchDeclaration1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception) $$"));
        }

        [Fact]
        public async Task TestAfterCatchDeclaration2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) $$"));
        }

        [Fact]
        public async Task TestAfterCatchDeclarationEmpty()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch () $$"));
        }

        [Fact]
        public async Task TestNotAfterTryBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} $$"));
        }
    }
}
