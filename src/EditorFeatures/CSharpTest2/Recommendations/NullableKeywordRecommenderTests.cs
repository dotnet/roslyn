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
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public void TestAfterHash()
        {
            VerifyKeyword(
@"#$$");
        }

        [Fact]
        public void TestAfterHashAndSpace()
        {
            VerifyKeyword(
@"# $$");
        }

        [Fact]
        public void TestNotAfterHashAndNullable()
        {
            VerifyAbsence(
@"#nullable $$");
        }

        [Fact]
        public void TestNotAfterPragma()
            => VerifyAbsence(@"#pragma $$");

        [Fact]
        public void TestNotAfterPragmaWarning()
            => VerifyAbsence(@"#pragma warning $$");

        [Fact]
        public void TestAfterPragmaWarningDisable()
            => VerifyKeyword(@"#pragma warning disable $$");

        [Fact]
        public void TestAfterPragmaWarningEnable()
            => VerifyKeyword(@"#pragma warning enable $$");

        [Fact]
        public void TestAfterPragmaWarningRestore()
            => VerifyKeyword(@"#pragma warning restore $$");

        [Fact]
        public void TestAfterPragmaWarningSafeOnly()
            => VerifyAbsence(@"#pragma warning safeonly $$");

        [Fact]
        public void TestNotAfterPragmaWarningSafeOnlyNullable()
            => VerifyAbsence(@"#pragma warning safeonly nullable $$");

        [Fact]
        public void TestNotAfterPragmaWarningRestoreNullable()
            => VerifyAbsence(@"#pragma warning restore nullable, $$");

        [Fact]
        public void TestNotAfterPragmaWarningDisableId()
            => VerifyAbsence(@"#pragma warning disable 114, $$");
    }
}
