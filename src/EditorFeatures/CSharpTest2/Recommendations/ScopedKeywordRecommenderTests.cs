// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

public sealed class ScopedKeywordRecommenderTests : RecommenderTests
{
    protected override string KeywordText => "scoped";

    private readonly ScopedKeywordRecommender _recommender = new();

    public ScopedKeywordRecommenderTests()
    {
        this.RecommendKeywordsAsync = async (position, context) => _recommender.RecommendKeywords(position, context, CancellationToken.None);
    }

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
    public Task TestNotAfterStackAlloc()
        => VerifyAbsenceAsync(
            """
            class C {
                 int* goo = stackalloc $$
            """);

    [Fact]
    public Task TestNotInFixedStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"fixed ($$"));

    [Fact]
    public Task TestNotInDelegateReturnType()
        => VerifyAbsenceAsync(
@"public delegate $$");

    [Fact]
    public Task TestPossibleLambda()
        => VerifyKeywordAsync(AddInsideMethod(
@"var x = (($$"));

    [Fact]
    public Task TestEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestBeforeStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            $$
            return true;
            """));

    [Fact]
    public Task TestAfterStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            return true;
            $$
            """));

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync("""
            class C
            {
              $$
            }
            """);

    [Fact]
    public Task TestInFor()
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$"));

    [Fact]
    public Task TestInFor2()
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$;"));

    [Fact]
    public Task TestInFor3()
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$;;"));

    [Fact]
    public Task TestNotInFor()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));

    [Fact]
    public Task TestNotAfterVar()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var $$"));

    [Fact]
    public Task TestInForEach()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$"));

    [Fact]
    public Task TestNotInForEach()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$"));

    [Fact]
    public Task TestInAwaitForEach()
        => VerifyKeywordAsync(AddInsideMethod(
@"await foreach ($$"));

    [Fact]
    public Task TestNotInAwaitForEach()
        => VerifyAbsenceAsync(AddInsideMethod(
@"await foreach (var $$"));

    [Fact]
    public Task TestNotInForEachRefLoop0()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref $$"));

    [Fact]
    public Task TestNotInForEachRefLoop1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref $$ x"));

    [Fact]
    public Task TestNotInForEachRefLoop2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref s$$ x"));

    [Fact]
    public Task TestNotInForEachRefReadonlyLoop0()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref readonly $$ x"));

    [Fact]
    public Task TestNotInForRefLoop0()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (ref $$"));

    [Fact]
    public Task TestNotInForRefLoop1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (ref s$$"));

    [Fact]
    public Task TestNotInForRefReadonlyLoop0()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (ref readonly $$"));

    [Fact]
    public Task TestNotInForRefReadonlyLoop1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (ref readonly s$$"));

    [Fact]
    public Task TestNotInUsing()
        => VerifyAbsenceAsync(AddInsideMethod(
@"using ($$"));

    [Fact]
    public Task TestNotInUsing2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"using (var $$"));

    [Fact]
    public Task TestNotInAwaitUsing()
        => VerifyAbsenceAsync(AddInsideMethod(
@"await using ($$"));

    [Fact]
    public Task TestNotInAwaitUsing2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"await using (var $$"));

    [Fact]
    public Task TestNotAfterConstLocal()
        => VerifyAbsenceAsync(AddInsideMethod(
@"const $$"));

    [Fact]
    public Task TestNotAfterConstField()
        => VerifyAbsenceAsync(
            """
            class C {
                const $$
            """);

    [Fact]
    public Task TestAfterOutKeywordInArgument()
        => VerifyKeywordAsync(AddInsideMethod(
@"M(out $$"));

    [Fact]
    public Task TestNotAfterRefInMemberContext()
        => VerifyAbsenceAsync(
            """
            class C {
                ref $$
            """);

    [Fact]
    public Task TestNotAfterRefReadonlyInMemberContext()
        => VerifyAbsenceAsync(
            """
            class C {
                ref readonly $$
            """);

    [Fact]
    public Task TestNotAfterRefInStatementContext()
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref $$"));

    [Fact]
    public Task TestNotAfterRefReadonlyInStatementContext()
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref readonly $$"));

    [Fact]
    public Task TestNotAfterRefLocalDeclaration()
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref $$ int local;"));

    [Fact]
    public Task TestNotAfterRefReadonlyLocalDeclaration()
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref readonly $$ int local;"));

    [Fact]
    public Task TestNotAfterRefExpression()
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$"));

    [Fact]
    public Task TestInParameter1()
        => VerifyKeywordAsync("""
            class C
            {
                public void M($$)
            }
            """);

    [Fact]
    public Task TestInParameter2()
        => VerifyKeywordAsync("""
            class C
            {
                public void M($$ ref)
            }
            """);

    [Fact]
    public Task TestInParameter3()
        => VerifyKeywordAsync("""
            class C
            {
                public void M($$ ref int i)
            }
            """);

    [Fact]
    public Task TestInParameter4()
        => VerifyKeywordAsync("""
            class C
            {
                public void M($$ ref int i)
            }
            """);

    [Fact]
    public Task TestInOperatorParameter()
        => VerifyKeywordAsync("""
            class C
            {
                public static C operator +($$ in C c)
            }
            """);

    [Fact]
    public Task TestInAnonymousMethodParameter()
        => VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    var x = delegate ($$) { };
                }
            }
            """);

    [Fact]
    public Task TestInParameterAfterThisScoped()
        => VerifyKeywordAsync("""
            static class C
            {
                static void M(this $$)
            }
            """);
}
