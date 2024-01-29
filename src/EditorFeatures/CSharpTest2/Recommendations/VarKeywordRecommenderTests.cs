// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class VarKeywordRecommenderTests : RecommenderTests
    {
        protected override string KeywordText => "var";

        private readonly VarKeywordRecommender _recommender = new();

        public VarKeywordRecommenderTests()
        {
            this.RecommendKeywordsAsync = (position, context) => Task.FromResult(_recommender.RecommendKeywords(position, context, CancellationToken.None));
        }

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
        public async Task TestNotAfterStackAlloc()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                     int* goo = stackalloc $$
                """);
        }

        [Fact]
        public async Task TestNotInFixedStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"fixed ($$"));
        }

        [Fact]
        public async Task TestNotInDelegateReturnType()
        {
            await VerifyAbsenceAsync(
@"public delegate $$");
        }

        [Fact]
        public async Task TestInCastType()
        {
            // Could be a deconstruction
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$"));
        }

        [Fact]
        public async Task TestInCastType2()
        {
            // Could be a deconstruction
            await VerifyKeywordAsync(AddInsideMethod(
@"var str = (($$)items) as string;"));
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
        public async Task TestNotAfterLock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock $$"));
        }

        [Fact]
        public async Task TestNotAfterLock2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock ($$"));
        }

        [Fact]
        public async Task TestNotAfterLock3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock (l$$"));
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
        public async Task TestInFor()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$"));
        }

        [Fact]
        public async Task TestNotInFor()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));
        }

        [Fact]
        public async Task TestInFor2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;"));
        }

        [Fact]
        public async Task TestInFor3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;;"));
        }

        [Fact]
        public async Task TestNotAfterVar()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var $$"));
        }

        [Fact]
        public async Task TestInForEach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact]
        public async Task TestNotInForEach()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$"));
        }

        [Fact]
        public async Task TestInAwaitForEach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"await foreach ($$"));
        }

        [Fact]
        public async Task TestNotInAwaitForEach()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"await foreach (var $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForEachRefLoop0()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForEachRefLoop1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref $$ x"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForEachRefLoop2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref v$$ x"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForEachRefReadonlyLoop0()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (ref readonly $$ x"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForRefLoop0()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for (ref $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForRefLoop1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for (ref v$$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForRefReadonlyLoop0()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for (ref readonly $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37223")]
        public async Task TestInForRefReadonlyLoop1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for (ref readonly v$$"));
        }

        [Fact]
        public async Task TestInUsing()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));
        }

        [Fact]
        public async Task TestNotInUsing()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using (var $$"));
        }

        [Fact]
        public async Task TestInAwaitUsing()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"await using ($$"));
        }

        [Fact]
        public async Task TestNotInAwaitUsing()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"await using (var $$"));
        }

        [Fact]
        public async Task TestAfterConstLocal()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$"));
        }

        [Fact]
        public async Task TestNotAfterConstField()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    const $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12121")]
        public async Task TestAfterOutKeywordInArgument()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"M(out $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/12121")]
        public async Task TestAfterOutKeywordInParameter()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                     void M1(out $$
                """);
        }

        [Fact]
        public async Task TestVarPatternInSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch(o)
                    {
                        case $$
                    }
                """));
        }

        [Fact]
        public async Task TestVarPatternInIs()
            => await VerifyKeywordAsync(AddInsideMethod("var b = o is $$ "));

        [Fact]
        public async Task TestNotAfterRefInMemberContext()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRefReadonlyInMemberContext()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    ref readonly $$
                """);
        }

        [Fact]
        public async Task TestAfterRefInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyInStatementContext()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$"));
        }

        [Fact]
        public async Task TestAfterRefLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;"));
        }

        [Fact]
        public async Task TestAfterRefReadonlyLocalDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref readonly $$ int local;"));
        }

        // For a local function, we can't add any tests - sometimes the keyword is offered and sometimes it's not,
        // depending on whether the keyword is partially written or not. This is because a partially written keyword
        // causes this to be parsed as a local declaration instead. We can't add either test because
        // VerifyKeywordAsync & VerifyAbsenceAsync check for both cases - with the keyword partially written and without.

        [Fact]
        public async Task TestNotAfterRefExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/10170")]
        public async Task TestInPropertyPattern()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact]
        public async Task TestNotInDeclarationDeconstruction()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var (x, $$) = (0, 0);"));
        }

        [Fact]
        public async Task TestInMixedDeclarationAndAssignmentInDeconstruction()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"(x, $$) = (0, 0);"));
        }

        [Fact]
        public async Task TestAfterScoped()
        {
            await VerifyKeywordAsync(AddInsideMethod("scoped $$"));
            await VerifyKeywordAsync("scoped $$");
        }
    }
}
