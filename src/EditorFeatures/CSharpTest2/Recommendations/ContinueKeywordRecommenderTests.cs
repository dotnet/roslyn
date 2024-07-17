// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ContinueKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
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
        public async Task TestEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestBeforeStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                $$
                return true;
                """));
        }

        [Fact]
        public async Task TestAfterStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                return true;
                $$
                """));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                if (true) {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestAfterIf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                if (true) 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterDo()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                do 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterWhile()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                while (true) 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterFor()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                for (int i = 0; i < 10; i++) 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterForeach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                foreach (var v in bar)
                    $$
                """));
        }

        [Fact]
        public async Task TestNotInsideLambda()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                foreach (var v in bar) {
                   var d = () => {
                     $$
                """));
        }

        [Fact]
        public async Task TestOutsideLambda()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                foreach (var v in bar) {
                   var d = () => {
                   };
                   $$
                """));
        }

        [Fact]
        public async Task TestNotInsideAnonymousMethod()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                foreach (var v in bar) {
                   var d = delegate {
                     $$
                """));
        }

        [Fact]
        public async Task TestNotInsideSwitch()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                switch (a) {
                    case 0:
                      $$
                """));
        }

        [Fact]
        public async Task TestNotAfterContinue()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"continue $$"));
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
    }
}
