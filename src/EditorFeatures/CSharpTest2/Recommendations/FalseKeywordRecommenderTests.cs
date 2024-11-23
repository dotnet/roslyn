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
    public class FalseKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInPreprocessor1()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                #$$
                """);
        }

        [Fact]
        public async Task TestNotInPreprocessor2()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                #line $$
                """);
        }

        [Fact]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));
        }

        [Fact]
        public async Task TestInPPIf()
        {
            await VerifyKeywordAsync(
@"#if $$");
        }

        [Fact]
        public async Task TestInPPIf_Or()
        {
            await VerifyKeywordAsync(
@"#if a || $$");
        }

        [Fact]
        public async Task TestInPPIf_And()
        {
            await VerifyKeywordAsync(
@"#if a && $$");
        }

        [Fact]
        public async Task TestInPPIf_Not()
        {
            await VerifyKeywordAsync(
@"#if ! $$");
        }

        [Fact]
        public async Task TestInPPIf_Paren()
        {
            await VerifyKeywordAsync(
@"#if ( $$");
        }

        [Fact]
        public async Task TestInPPIf_Equals()
        {
            await VerifyKeywordAsync(
@"#if a == $$");
        }

        [Fact]
        public async Task TestInPPIf_NotEquals()
        {
            await VerifyKeywordAsync(
@"#if a != $$");
        }

        [Fact]
        public async Task TestInPPElIf()
        {
            await VerifyKeywordAsync(
                """
                #if true
                #elif $$
                """);
        }

        [Fact]
        public async Task TestInPPelIf_Or()
        {
            await VerifyKeywordAsync(
                """
                #if true
                #elif a || $$
                """);
        }

        [Fact]
        public async Task TestInPPElIf_And()
        {
            await VerifyKeywordAsync(
                """
                #if true
                #elif a && $$
                """);
        }

        [Fact]
        public async Task TestInPPElIf_Not()
        {
            await VerifyKeywordAsync(
                """
                #if true
                #elif ! $$
                """);
        }

        [Fact]
        public async Task TestInPPElIf_Paren()
        {
            await VerifyKeywordAsync(
                """
                #if true
                #elif ( $$
                """);
        }

        [Fact]
        public async Task TestInPPElIf_Equals()
        {
            await VerifyKeywordAsync(
                """
                #if true
                #elif a == $$
                """);
        }

        [Fact]
        public async Task TestInPPElIf_NotEquals()
        {
            await VerifyKeywordAsync(
                """
                #if true
                #elif a != $$
                """);
        }

        [Fact]
        public async Task TestAfterUnaryOperator()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   public static bool operator $$
                """);
        }

        [Fact]
        public async Task TestNotAfterImplicitOperator()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   public static implicit operator $$
                """);
        }

        [Fact]
        public async Task TestNotAfterExplicitOperator()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   public static implicit operator $$
                """);
        }

        [Fact]
        public async Task TestInNamedParameter()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                return new SingleDeclaration(
                                kind: GetKind(node.Kind),
                                hasUsings: $$
                """));
        }

        [Fact]
        public async Task TestInAttribute()
        {
            await VerifyKeywordAsync(
@"[assembly: ComVisible($$");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInDefault()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    public int x, y;
                    void M()
                    {
                        var c = new C { x = 2, y = 3, $$
                """);
        }

        [Fact]
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        #region Collection expressions

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [$$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [new object(), $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. ($$
                }
                """);
        }

        #endregion
    }
}
