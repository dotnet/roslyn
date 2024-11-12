// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class CatchKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterTry()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                try {
                } $$
                """));
        }

        [Fact]
        public async Task TestAfterTryCatch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                try {
                } catch {
                } $$
                """));
        }

        [Fact]
        public async Task TestNotAfterFinally()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                try {
                } finally {
                } $$
                """));
        }

        [Fact]
        public async Task TestNotInStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestNotAfterBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                if (true)
                {
                    Console.WriteLine();
                }
                $$
                """));
        }

        [Fact]
        public async Task TestNotAfterCatch()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                try {
                } catch $$
                """));
        }

        [Fact]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync("""
                class C {
                    $$
                }
                """);
        }
    }
}
