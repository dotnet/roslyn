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
    public class NullKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInPPIf()
        {
            await VerifyAbsenceAsync(
@"#if $$");
        }

        [Fact]
        public async Task TestNotInPPIf_Or()
        {
            await VerifyAbsenceAsync(
@"#if a || $$");
        }

        [Fact]
        public async Task TestNotInPPIf_And()
        {
            await VerifyAbsenceAsync(
@"#if a && $$");
        }

        [Fact]
        public async Task TestNotInPPIf_Not()
        {
            await VerifyAbsenceAsync(
@"#if ! $$");
        }

        [Fact]
        public async Task TestNotInPPIf_Paren()
        {
            await VerifyAbsenceAsync(
@"#if ( $$");
        }

        [Fact]
        public async Task TestNotInPPIf_Equals()
        {
            await VerifyAbsenceAsync(
@"#if a == $$");
        }

        [Fact]
        public async Task TestNotInPPIf_NotEquals()
        {
            await VerifyAbsenceAsync(
@"#if a != $$");
        }

        [Fact]
        public async Task TestNotInPPElIf()
        {
            await VerifyAbsenceAsync(
                """
                #if true
                #elif $$
                """);
        }

        [Fact]
        public async Task TestNotInPPelIf_Or()
        {
            await VerifyAbsenceAsync(
                """
                #if true
                #elif a || $$
                """);
        }

        [Fact]
        public async Task TestNotInPPElIf_And()
        {
            await VerifyAbsenceAsync(
                """
                #if true
                #elif a && $$
                """);
        }

        [Fact]
        public async Task TestNotInPPElIf_Not()
        {
            await VerifyAbsenceAsync(
                """
                #if true
                #elif ! $$
                """);
        }

        [Fact]
        public async Task TestNotInPPElIf_Paren()
        {
            await VerifyAbsenceAsync(
                """
                #if true
                #elif ( $$
                """);
        }

        [Fact]
        public async Task TestNotInPPElIf_Equals()
        {
            await VerifyAbsenceAsync(
                """
                #if true
                #elif a == $$
                """);
        }

        [Fact]
        public async Task TestNotInPPElIf_NotEquals()
        {
            await VerifyAbsenceAsync(
                """
                #if true
                #elif a != $$
                """);
        }

        [Fact]
        public async Task TestNotAfterUnaryOperator()
        {
            await VerifyAbsenceAsync(
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
        public async Task TestAfterCastInField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   public static readonly ImmutableList<T> Empty = new ImmutableList<T>((Segment)$$
                """);
        }

        [Fact]
        public async Task TestInTernary()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                SyntaxKind kind = caseOrDefaultKeywordOpt == $$ ? SyntaxKind.GotoStatement :
                                caseOrDefaultKeyword.Kind == SyntaxKind.CaseKeyword ? SyntaxKind.GotoCaseStatement : SyntaxKind.GotoDefaultStatement;
                """));
        }

        [Fact]
        public async Task TestInForMiddle()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for (int i = 0; $$"));
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541670")]
        public async Task TestInReferenceSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch ("goo")
                        {
                            case $$
                        }
                """));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543766")]
        public async Task TestNotInDefaultParameterValue()
        {
            await VerifyKeywordAsync(
@"class C { void Goo(string[] args = $$");
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
        public async Task TestNotInCrefContext()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    /// <see cref="$$">
                    static void Main(string[] args)
                    {

                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
        public async Task TestInCrefContextNotAfterDot()
        {
            await VerifyAbsenceAsync("""
                /// <see cref="System.$$" />
                class C { }
                """);
        }

        [Fact]
        public async Task TestAfterIs()
            => await VerifyKeywordAsync(AddInsideMethod(@"if (x is $$"));

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25293")]
        public async Task TestAfterIs_BeforeExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                int x;
                int y = x is $$ Method();
                """));
        }

        [Fact]
        public async Task TestAfterLambdaOpenParen()
        {
            await VerifyKeywordAsync(
@"var lam = ($$");
        }

        [Fact]
        public async Task TestAfterLambdaComma()
        {
            await VerifyKeywordAsync(
@"var lam = (int i, $$");
        }

        [Fact]
        public async Task TestLambdaDefaultParameterValue()
        {
            await VerifyKeywordAsync(
@"var lam = (int i = $$");
        }
    }
}
