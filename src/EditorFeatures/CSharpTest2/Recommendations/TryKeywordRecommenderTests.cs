// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class TryKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestEmptyStatement()
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
        public async Task TestNotAfterTry()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try $$"));
        }

        [Fact]
        public async Task TestInsideTry()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                try {
                    $$
                """));
        }

        [Fact]
        public async Task TestInsideCatch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                try {
                } catch {
                    $$
                """));
        }

        [Fact]
        public async Task TestInsideFinally()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                try {
                } finally {
                    $$
                """));
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
