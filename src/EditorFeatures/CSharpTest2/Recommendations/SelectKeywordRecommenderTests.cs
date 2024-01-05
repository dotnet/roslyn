﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class SelectKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotAtEndOfPreviousClause()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y$$"));
        }

        [Fact]
        public async Task TestNewClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterPreviousClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          where x > y
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterPreviousContinuationClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          group x by y into g
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterOrderByExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterOrderByAscendingExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x ascending
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterOrderByDescendingExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby x descending
                          $$
                """));
        }

        [Fact]
        public async Task TestBetweenClauses()
        {
            // Technically going to generate invalid code, but we 
            // shouldn't stop users from doing this.
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          $$
                          from z in w
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
    }
}
