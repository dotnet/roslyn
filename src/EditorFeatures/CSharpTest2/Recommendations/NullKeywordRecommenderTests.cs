// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class NullKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));

    [Fact]
    public Task TestNotInPPIf()
        => VerifyAbsenceAsync(
@"#if $$");

    [Fact]
    public Task TestNotInPPIf_Or()
        => VerifyAbsenceAsync(
@"#if a || $$");

    [Fact]
    public Task TestNotInPPIf_And()
        => VerifyAbsenceAsync(
@"#if a && $$");

    [Fact]
    public Task TestNotInPPIf_Not()
        => VerifyAbsenceAsync(
@"#if ! $$");

    [Fact]
    public Task TestNotInPPIf_Paren()
        => VerifyAbsenceAsync(
@"#if ( $$");

    [Fact]
    public Task TestNotInPPIf_Equals()
        => VerifyAbsenceAsync(
@"#if a == $$");

    [Fact]
    public Task TestNotInPPIf_NotEquals()
        => VerifyAbsenceAsync(
@"#if a != $$");

    [Fact]
    public Task TestNotInPPElIf()
        => VerifyAbsenceAsync(
            """
            #if true
            #elif $$
            """);

    [Fact]
    public Task TestNotInPPelIf_Or()
        => VerifyAbsenceAsync(
            """
            #if true
            #elif a || $$
            """);

    [Fact]
    public Task TestNotInPPElIf_And()
        => VerifyAbsenceAsync(
            """
            #if true
            #elif a && $$
            """);

    [Fact]
    public Task TestNotInPPElIf_Not()
        => VerifyAbsenceAsync(
            """
            #if true
            #elif ! $$
            """);

    [Fact]
    public Task TestNotInPPElIf_Paren()
        => VerifyAbsenceAsync(
            """
            #if true
            #elif ( $$
            """);

    [Fact]
    public Task TestNotInPPElIf_Equals()
        => VerifyAbsenceAsync(
            """
            #if true
            #elif a == $$
            """);

    [Fact]
    public Task TestNotInPPElIf_NotEquals()
        => VerifyAbsenceAsync(
            """
            #if true
            #elif a != $$
            """);

    [Fact]
    public Task TestNotAfterUnaryOperator()
        => VerifyAbsenceAsync(
            """
            class C {
               public static bool operator $$
            """);

    [Fact]
    public Task TestNotAfterImplicitOperator()
        => VerifyAbsenceAsync(
            """
            class C {
               public static implicit operator $$
            """);

    [Fact]
    public Task TestNotAfterExplicitOperator()
        => VerifyAbsenceAsync(
            """
            class C {
               public static implicit operator $$
            """);

    [Fact]
    public Task TestAfterCastInField()
        => VerifyKeywordAsync(
            """
            class C {
               public static readonly ImmutableList<T> Empty = new ImmutableList<T>((Segment)$$
            """);

    [Fact]
    public Task TestInTernary()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            SyntaxKind kind = caseOrDefaultKeywordOpt == $$ ? SyntaxKind.GotoStatement :
                            caseOrDefaultKeyword.Kind == SyntaxKind.CaseKeyword ? SyntaxKind.GotoCaseStatement : SyntaxKind.GotoDefaultStatement;
            """));

    [Fact]
    public Task TestInForMiddle()
        => VerifyKeywordAsync(AddInsideMethod(
@"for (int i = 0; $$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInTypeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInDefault()
        => VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInSizeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541670")]
    public Task TestInReferenceSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch ("goo")
                    {
                        case $$
                    }
            """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543766")]
    public Task TestNotInDefaultParameterValue()
        => VerifyKeywordAsync(
@"class C { void Goo(string[] args = $$");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task TestNotInObjectInitializerMemberContext()
        => VerifyAbsenceAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
    public Task TestNotInCrefContext()
        => VerifyAbsenceAsync("""
            class Program
            {
                /// <see cref="$$">
                static void Main(string[] args)
                {

                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
    public Task TestInCrefContextNotAfterDot()
        => VerifyAbsenceAsync("""
            /// <see cref="System.$$" />
            class C { }
            """);

    [Fact]
    public async Task TestAfterIs()
        => await VerifyKeywordAsync(AddInsideMethod(@"if (x is $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25293")]
    public Task TestAfterIs_BeforeExpression()
        => VerifyKeywordAsync(AddInsideMethod("""
            int x;
            int y = x is $$ Method();
            """));

    [Fact]
    public Task TestAfterLambdaOpenParen()
        => VerifyKeywordAsync(
@"var lam = ($$");

    [Fact]
    public Task TestAfterLambdaComma()
        => VerifyKeywordAsync(
@"var lam = (int i, $$");

    [Fact]
    public Task TestLambdaDefaultParameterValue()
        => VerifyKeywordAsync(
@"var lam = (int i = $$");

    #region Collection expressions

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [$$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [new object(), $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """);

    #endregion
}
