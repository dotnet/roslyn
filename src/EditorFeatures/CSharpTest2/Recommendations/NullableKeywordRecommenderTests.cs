// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class NullableKeywordRecommenderTests : KeywordRecommenderTests
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
@"class C { }
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
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
            await VerifyKeywordAsync(
@"#$$");
        }

        [Fact]
        public async Task TestAfterHashAndSpace()
        {
            await VerifyKeywordAsync(
@"# $$");
        }

        [Fact]
        public async Task TestNotAfterHashAndNullable()
        {
            await VerifyAbsenceAsync(
@"#nullable $$");
        }

        [Fact]
        public async Task TestNotAfterPragma()
        {
            await VerifyAbsenceAsync(@"#pragma $$");
        }

        [Fact]
        public async Task TestNotAfterPragmaWarning()
        {
            await VerifyAbsenceAsync(@"#pragma warning $$");
        }

        [Fact]
        public async Task TestAfterPragmaWarningDisable()
        {
            await VerifyKeywordAsync(@"#pragma warning disable $$");
        }

        [Fact]
        public async Task TestAfterPragmaWarningEnable()
        {
            await VerifyKeywordAsync(@"#pragma warning enable $$");
        }

        [Fact]
        public async Task TestAfterPragmaWarningRestore()
        {
            await VerifyKeywordAsync(@"#pragma warning restore $$");
        }

        [Fact]
        public async Task TestAfterPragmaWarningSafeOnly()
        {
            await VerifyAbsenceAsync(@"#pragma warning safeonly $$");
        }

        [Fact]
        public async Task TestNotAfterPragmaWarningSafeOnlyNullable()
        {
            await VerifyAbsenceAsync(@"#pragma warning safeonly nullable $$");
        }

        [Fact]
        public async Task TestNotAfterPragmaWarningRestoreNullable()
        {
            await VerifyAbsenceAsync(@"#pragma warning restore nullable, $$");
        }

        [Fact]
        public async Task TestNotAfterPragmaWarningDisableId()
        {
            await VerifyAbsenceAsync(@"#pragma warning disable 114, $$");
        }
    }
}
