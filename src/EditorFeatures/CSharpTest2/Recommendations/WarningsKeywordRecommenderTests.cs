// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class WarningsKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterHash()
        {
            await VerifyAbsenceAsync(
@"#$$");
        }

        [Fact]
        public async Task TestAfterHashAndSpace()
        {
            await VerifyAbsenceAsync(
@"# $$");
        }

        [Fact]
        public async Task TestAfterPragma()
        {
            await VerifyAbsenceAsync(
@"#pragma $$");
        }

        [Fact]
        public async Task TestAfterNullable()
        {
            await VerifyAbsenceAsync(
@"#nullable $$");
        }

        [Fact]
        public async Task TestAfterNullableEnable()
        {
            await VerifyKeywordAsync(
@"#nullable enable $$");
        }

        [Fact]
        public async Task TestAfterNullableDisable()
        {
            await VerifyKeywordAsync(
@"#nullable disable $$");
        }

        [Fact]
        public async Task TestAfterNullableRestore()
        {
            await VerifyKeywordAsync(
@"#nullable restore $$");
        }

        [Fact]
        public async Task TestAfterNullableBadSetting()
        {
            await VerifyAbsenceAsync(
@"#nullable true $$");
        }
    }
}
