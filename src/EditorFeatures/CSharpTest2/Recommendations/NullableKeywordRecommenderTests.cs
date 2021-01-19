// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestAfterHash()
        {
            VerifyKeyword(
@"#$$");
        }

        [Fact]
        public async Task TestAfterHashAndSpace()
        {
            VerifyKeyword(
@"# $$");
        }

        [Fact]
        public async Task TestNotAfterHashAndNullable()
        {
            VerifyAbsence(
@"#nullable $$");
        }

        [Fact]
        public async Task TestNotAfterPragma()
            => VerifyAbsence(@"#pragma $$");

        [Fact]
        public async Task TestNotAfterPragmaWarning()
            => VerifyAbsence(@"#pragma warning $$");

        [Fact]
        public async Task TestAfterPragmaWarningDisable()
            => VerifyKeyword(@"#pragma warning disable $$");

        [Fact]
        public async Task TestAfterPragmaWarningEnable()
            => VerifyKeyword(@"#pragma warning enable $$");

        [Fact]
        public async Task TestAfterPragmaWarningRestore()
            => VerifyKeyword(@"#pragma warning restore $$");

        [Fact]
        public async Task TestAfterPragmaWarningSafeOnly()
            => VerifyAbsence(@"#pragma warning safeonly $$");

        [Fact]
        public async Task TestNotAfterPragmaWarningSafeOnlyNullable()
            => VerifyAbsence(@"#pragma warning safeonly nullable $$");

        [Fact]
        public async Task TestNotAfterPragmaWarningRestoreNullable()
            => VerifyAbsence(@"#pragma warning restore nullable, $$");

        [Fact]
        public async Task TestNotAfterPragmaWarningDisableId()
            => VerifyAbsence(@"#pragma warning disable 114, $$");
    }
}
