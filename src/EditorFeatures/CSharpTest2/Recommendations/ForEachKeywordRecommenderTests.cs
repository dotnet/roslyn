// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ForEachKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAtRoot_TopLevelStatement()
        {
            await VerifyKeywordAsync(
@"$$", options: CSharp9ParseOptions);
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
        public async Task TestAfterStatement_TopLevelStatement()
        {
            await VerifyKeywordAsync(
                """
                System.Console.WriteLine();
                $$
                """, options: CSharp9ParseOptions);
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
        public async Task TestAfterVariableDeclaration_TopLevelStatement()
        {
            await VerifyKeywordAsync(
                """
                int i = 0;
                $$
                """, options: CSharp9ParseOptions);
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

        [Theory, CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterAwait(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"await $$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestBeforeStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                $$
                return 0;
                """, returnType: "int", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                return true;
                $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBlock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (true) {
                }
                $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInsideForEach(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                foreach (var v in c)
                     $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInsideForEachInsideForEach(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                foreach (var v in c)
                     foreach (var v in c)
                        $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInsideForEachBlock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                foreach (var v in c) {
                     $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterForEach1(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterForEach2(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterForEach3(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterForEach4(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterForEach5(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterForEach6(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in c $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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
