// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ThrowKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterIf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (true) 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterDo()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                do 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterWhile()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                while (true) 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterFor()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                for (int i = 0; i < 10; i++) 
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterForeach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                foreach (var v in bar)
                    $$
                """));
        }

        [Fact]
        public async Task TestNotAfterThrow()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"throw $$"));
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

        [Fact]
        public async Task TestInNestedIf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                if (caseOrDefaultKeywordOpt != null) {
                    if (caseOrDefaultKeyword.Kind != SyntaxKind.CaseKeyword && caseOrDefaultKeyword.Kind != SyntaxKind.DefaultKeyword) 
                      $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
        public async Task TestAfterArrow()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    void Goo() => $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
        public async Task TestAfterQuestionQuestion()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    public C(object o)
                    {
                        _o = o ?? $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
        public async Task TestInConditional1()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    public C(object o)
                    {
                        var v= true ? $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9099")]
        public async Task TestInConditional2()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    public C(object o)
                    {
                        var v= true ? 0 : $$
                """);
        }
    }
}
