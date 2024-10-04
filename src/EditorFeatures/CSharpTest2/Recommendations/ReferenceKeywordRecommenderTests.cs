// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ReferenceKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(
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
        public async Task TestNotAfterHash()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"#$$");
        }

        [Fact]
        public async Task TestAfterHash_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"#$$");
        }

        [Fact]
        public async Task TestNotAfterHashAndSpace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"# $$");
        }

        [Fact]
        public async Task TestAfterHashAndSpace_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"# $$");
        }

        [Fact]
        public async Task TestNestedPreprocessor()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                #if true
                    #$$
                #endif
                """);
        }

        [Fact]
        public async Task TestBeforeUsing()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                #$$
                using System;
                """);
        }

        [Fact]
        public async Task TestBeforeGlobalUsing()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                #$$
                global using System;
                """);
        }

        [Fact]
        public async Task TestNotAfterUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                using System;
                #$$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                global using System;
                #$$
                """);
        }
    }
}
