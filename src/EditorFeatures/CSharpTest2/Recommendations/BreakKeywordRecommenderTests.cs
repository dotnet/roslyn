// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class BreakKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot()
        {
            await VerifyAbsenceAsync(
@"$$", options: CSharp9ParseOptions);
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
        public async Task TestNotAfterGlobalStatement()
        {
            await VerifyAbsenceAsync(
                """
                System.Console.WriteLine();
                $$
                """, options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration()
        {
            await VerifyAbsenceAsync(
                """
                int i = 0;
                $$
                """, options: CSharp9ParseOptions);
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
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestBeforeStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                $$
                return true;
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                return true;
                $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterBlock(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
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
            await VerifyAbsenceAsync(AddInsideMethod(
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

        [Theory]
        [CombinatorialData]
        public async Task TestNotInsideLambda(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                foreach (var v in bar) {
                   var d = () => {
                     $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotInsideAnonymousMethod(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                foreach (var v in bar) {
                   var d = delegate {
                     $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInsideSwitch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (a) {
                    case 0:
                       $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotInsideSwitchWithLambda(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                switch (a) {
                    case 0:
                      var d = () => {
                        $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInsideSwitchOutsideLambda(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (a) {
                    case 0:
                      var d = () => {
                      };
                      $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterBreak(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"break $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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

        [Theory]
        [CombinatorialData]
        public async Task TestAfterYield(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"yield $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterSwitchInSwitch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      switch (expr) {
                      }
                      $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterBlockInSwitch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      {
                      }
                      $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
