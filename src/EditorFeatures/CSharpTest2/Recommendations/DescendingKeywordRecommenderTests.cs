// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class DescendingKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterOrderByExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x $$
                """));
        }

        [Fact]
        public async Task TestAfterSecondExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x, y $$
                """));
        }

        [Fact]
        public async Task TestBetweenExprs()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x, y $$, z
                """));
        }

        [Fact]
        public async Task TestNotAfterDot()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x.$$
                """));
        }

        [Fact]
        public async Task TestNotAfterComma()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x, $$
                """));
        }

        [Fact]
        public async Task TestAfterCloseParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x.ToString() $$
                """));
        }

        [Fact]
        public async Task TestAfterCloseBracket()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x.ToString()[0] $$
                """));
        }
    }
}
