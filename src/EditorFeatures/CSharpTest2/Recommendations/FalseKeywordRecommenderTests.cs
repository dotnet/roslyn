// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class FalseKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotInPreprocessor1()
        => VerifyAbsenceAsync(
            """
            class C {
            #$$
            """);

    [Fact]
    public Task TestNotInPreprocessor2()
        => VerifyAbsenceAsync(
            """
            class C {
            #line $$
            """);

    [Fact]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));

    [Fact]
    public Task TestInPPIf()
        => VerifyKeywordAsync(
@"#if $$");

    [Fact]
    public Task TestInPPIf_Or()
        => VerifyKeywordAsync(
@"#if a || $$");

    [Fact]
    public Task TestInPPIf_And()
        => VerifyKeywordAsync(
@"#if a && $$");

    [Fact]
    public Task TestInPPIf_Not()
        => VerifyKeywordAsync(
@"#if ! $$");

    [Fact]
    public Task TestInPPIf_Paren()
        => VerifyKeywordAsync(
@"#if ( $$");

    [Fact]
    public Task TestInPPIf_Equals()
        => VerifyKeywordAsync(
@"#if a == $$");

    [Fact]
    public Task TestInPPIf_NotEquals()
        => VerifyKeywordAsync(
@"#if a != $$");

    [Fact]
    public Task TestInPPElIf()
        => VerifyKeywordAsync(
            """
            #if true
            #elif $$
            """);

    [Fact]
    public Task TestInPPelIf_Or()
        => VerifyKeywordAsync(
            """
            #if true
            #elif a || $$
            """);

    [Fact]
    public Task TestInPPElIf_And()
        => VerifyKeywordAsync(
            """
            #if true
            #elif a && $$
            """);

    [Fact]
    public Task TestInPPElIf_Not()
        => VerifyKeywordAsync(
            """
            #if true
            #elif ! $$
            """);

    [Fact]
    public Task TestInPPElIf_Paren()
        => VerifyKeywordAsync(
            """
            #if true
            #elif ( $$
            """);

    [Fact]
    public Task TestInPPElIf_Equals()
        => VerifyKeywordAsync(
            """
            #if true
            #elif a == $$
            """);

    [Fact]
    public Task TestInPPElIf_NotEquals()
        => VerifyKeywordAsync(
            """
            #if true
            #elif a != $$
            """);

    [Fact]
    public Task TestAfterUnaryOperator()
        => VerifyKeywordAsync(
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
    public Task TestInNamedParameter()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return new SingleDeclaration(
                            kind: GetKind(node.Kind),
                            hasUsings: $$
            """));

    [Fact]
    public Task TestInAttribute()
        => VerifyKeywordAsync(
@"[assembly: ComVisible($$");

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

    [Fact]
    public Task TestAfterRefExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));

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
