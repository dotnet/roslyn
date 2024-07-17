// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ByKeywordRecommenderTests : KeywordRecommenderTests
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

        [Theory, CombinatorialData]
        public async Task TestNotInEmptyStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterGroupExpr(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          group a $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterGroup(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          group $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterBy(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          group a by $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
