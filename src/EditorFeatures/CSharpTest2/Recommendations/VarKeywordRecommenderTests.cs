// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class VarKeywordRecommenderTests : RecommenderTests
{
    protected override string KeywordText => "var";

    private readonly VarKeywordRecommender _recommender = new();

    public VarKeywordRecommenderTests()
    {
        this.RecommendKeywordsAsync = (position, context) => Task.FromResult(_recommender.RecommendKeywords(position, context, CancellationToken.None));
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
    public Task TestInCastType()
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$"));

    [Fact]
    public Task TestInCastType2()
        => VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;"));

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
    public Task TestAfterBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            if (true) {
            }
            $$
            """));

    [Fact]
    public Task TestNotAfterLock()
        => VerifyAbsenceAsync(AddInsideMethod(
@"lock $$"));

    [Fact]
    public Task TestNotAfterLock2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"lock ($$"));

    [Fact]
    public Task TestNotAfterLock3()
        => VerifyAbsenceAsync(AddInsideMethod(
@"lock (l$$"));

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
    public Task TestNotInFor()
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));

    [Fact]
    public Task TestInFor2()
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$;"));

    [Fact]
    public Task TestInFor3()
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$;;"));

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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForEachRefLoop0()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForEachRefLoop1()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref $$ x"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForEachRefLoop2()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref v$$ x"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForEachRefReadonlyLoop0()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref readonly $$ x"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForRefLoop0()
        => VerifyKeywordAsync(AddInsideMethod(
@"for (ref $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForRefLoop1()
        => VerifyKeywordAsync(AddInsideMethod(
@"for (ref v$$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForRefReadonlyLoop0()
        => VerifyKeywordAsync(AddInsideMethod(
@"for (ref readonly $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
    public Task TestInForRefReadonlyLoop1()
        => VerifyKeywordAsync(AddInsideMethod(
@"for (ref readonly v$$"));

    [Fact]
    public Task TestInUsing()
        => VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));

    [Fact]
    public Task TestNotInUsing()
        => VerifyAbsenceAsync(AddInsideMethod(
@"using (var $$"));

    [Fact]
    public Task TestInAwaitUsing()
        => VerifyKeywordAsync(AddInsideMethod(
@"await using ($$"));

    [Fact]
    public Task TestNotInAwaitUsing()
        => VerifyAbsenceAsync(AddInsideMethod(
@"await using (var $$"));

    [Fact]
    public Task TestAfterConstLocal()
        => VerifyKeywordAsync(AddInsideMethod(
@"const $$"));

    [Fact]
    public Task TestNotAfterConstField()
        => VerifyAbsenceAsync(
            """
            class C {
                const $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12121")]
    public Task TestAfterOutKeywordInArgument()
        => VerifyKeywordAsync(AddInsideMethod(
@"M(out $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12121")]
    public Task TestAfterOutKeywordInParameter()
        => VerifyAbsenceAsync(
            """
            class C {
                 void M1(out $$
            """);

    [Fact]
    public Task TestVarPatternInSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch(o)
                {
                    case $$
                }
            """));

    [Fact]
    public async Task TestVarPatternInIs()
        => await VerifyKeywordAsync(AddInsideMethod("var b = o is $$ "));

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
    public Task TestAfterRefInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));

    [Fact]
    public Task TestAfterRefReadonlyInStatementContext()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$"));

    [Fact]
    public Task TestAfterRefLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;"));

    [Fact]
    public Task TestAfterRefReadonlyLocalDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;"));

    // For a local function, we can't add any tests - sometimes the keyword is offered and sometimes it's not,
    // depending on whether the keyword is partially written or not. This is because a partially written keyword
    // causes this to be parsed as a local declaration instead. We can't add either test because
    // VerifyKeywordAsync & VerifyAbsenceAsync check for both cases - with the keyword partially written and without.

    [Fact]
    public Task TestNotAfterRefExpression()
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10170")]
    public Task TestInPropertyPattern()
        => VerifyKeywordAsync(
            """
            using System;

            class Person { public string Name; }

            class Program
            {
                void Goo(object o)
                {
                    if (o is Person { Name: $$ })
                    {
                        Console.WriteLine(n);
                    }
                }
            }
            """);

    [Fact]
    public Task TestNotInDeclarationDeconstruction()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var (x, $$) = (0, 0);"));

    [Fact]
    public Task TestInMixedDeclarationAndAssignmentInDeconstruction()
        => VerifyKeywordAsync(AddInsideMethod(
@"(x, $$) = (0, 0);"));

    [Fact]
    public async Task TestAfterScoped()
    {
        await VerifyKeywordAsync(AddInsideMethod("scoped $$"));
        await VerifyKeywordAsync("scoped $$");
    }

    [Fact]
    public Task TestWithinExtension()
        => VerifyAbsenceAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
