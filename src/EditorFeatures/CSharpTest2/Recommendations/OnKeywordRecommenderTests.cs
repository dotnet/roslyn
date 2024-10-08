// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class OnKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterJoinInExpr1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a in e $$
                """));
        }

        [Fact]
        public async Task TestAfterJoinInExpr2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in e $$
                """));
        }

        [Fact]
        public async Task TestNotAfterOn1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in e on $$
                """));
        }

        [Fact]
        public async Task TestNotAfterOn2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in e on o$$
                """));
        }

        [Fact]
        public async Task TestNotAfterOn3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in e on o1 $$
                """));
        }

        [Fact]
        public async Task TestNotAfterOn4()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in e on o1 e$$
                """));
        }

        [Fact]
        public async Task TestNotAfterOn5()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in e on o1 equals $$
                """));
        }

        [Fact]
        public async Task TestNotAfterOn6()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in e on o1 equals o$$
                """));
        }

        [Fact]
        public async Task TestNotAfterIn()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a.b c in $$
                """));
        }
    }
}
