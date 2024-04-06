// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class IntoKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInSelectMemberExpressionOnlyADot()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.$$"));
        }
        [Fact]
        public async Task TestNotInSelectMemberExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.i$$"));
        }
        [Fact]
        public async Task TestAfterJoinRightExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a in e on o1 equals o2 $$
                """));
        }

        [Fact]
        public async Task TestAfterJoinRightExpr_NotAfterInto()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in o1 equals o2 into $$
                """));
        }

        [Fact]
        public async Task TestNotAfterEquals()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in o1 equals $$
                """));
        }

        [Fact]
        public async Task TestAfterSelectClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          select z
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterSelectClauseWithMemberExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          select z.i
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterSelectClause_NotAfterInto()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          select z
                          into $$
                """));
        }

        [Fact]
        public async Task TestAfterGroupClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          group z by w
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterGroupClause_NotAfterInto()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          group z by w
                          into $$
                """));
        }

        [Fact]
        public async Task TestNotAfterSelect()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          select $$
                """));
        }

        [Fact]
        public async Task TestNotAfterGroupKeyword()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          group $$
                """));
        }

        [Fact]
        public async Task TestNotAfterGroupExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          group x $$
                """));
        }

        [Fact]
        public async Task TestNotAfterGroupBy()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          group x by $$
                """));
        }
    }
}
