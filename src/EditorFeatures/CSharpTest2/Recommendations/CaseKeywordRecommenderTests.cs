// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class CaseKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotAfterExpr()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = goo $$"));
        }

        [Fact]
        public async Task TestNotAfterDottedName()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = goo.Current $$"));
        }

        [Fact]
        public async Task TestAfterSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    case 0:
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterDefault()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterPatternCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    case String s:
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterOneStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      Console.WriteLine();
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterOneStatementPatternCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    case String s:
                      Console.WriteLine();
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterTwoStatements()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      Console.WriteLine();
                      Console.WriteLine();
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default: {
                    }
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterBlockPatternCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    case String s: {
                    }
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterIfElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      if (goo) {
                      } else {
                      }
                    $$
                """));
        }

        [Fact]
        public async Task TestNotAfterIncompleteStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                       Console.WriteLine(
                    $$
                """));
        }

        [Fact]
        public async Task TestNotInsideBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                switch (expr) {
                    default: {
                      $$
                """));
        }

        [Fact]
        public async Task TestAfterIf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      if (goo)
                        Console.WriteLine();
                    $$
                """));
        }

        [Fact]
        public async Task TestNotAfterIf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      if (goo)
                        $$
                """));
        }

        [Fact]
        public async Task TestAfterWhile()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      while (true) {
                      }
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterGotoInSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      goto $$
                """));
        }

        [Fact]
        public async Task TestNotAfterGotoOutsideSwitch()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"goto $$"));
        }
    }
}
