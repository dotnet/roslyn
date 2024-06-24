// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ScopedKeywordRecommenderTests : RecommenderTests
    {
        protected override string KeywordText => "scoped";

        private readonly ScopedKeywordRecommender _recommender = new();

        public ScopedKeywordRecommenderTests()
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
        public async Task TestPossibleLambda()
        {
            // Could be `var x = ((scoped ref int x) => x);`
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = (($$"));
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
        public async Task TestNotInFor()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));
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

        [Fact]
        public async Task TestNotInForEachRefLoop0()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref $$"));
        }

        [Fact]
        public async Task TestNotInForEachRefLoop1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref $$ x"));
        }

        [Fact]
        public async Task TestNotInForEachRefLoop2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref s$$ x"));
        }

        [Fact]
        public async Task TestNotInForEachRefReadonlyLoop0()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (ref readonly $$ x"));
        }

        [Fact]
        public async Task TestNotInForRefLoop0()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (ref $$"));
        }

        [Fact]
        public async Task TestNotInForRefLoop1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (ref s$$"));
        }

        [Fact]
        public async Task TestNotInForRefReadonlyLoop0()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (ref readonly $$"));
        }

        [Fact]
        public async Task TestNotInForRefReadonlyLoop1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (ref readonly s$$"));
        }

        [Fact]
        public async Task TestNotInUsing()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using ($$"));
        }

        [Fact]
        public async Task TestNotInUsing2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using (var $$"));
        }

        [Fact]
        public async Task TestNotInAwaitUsing()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"await using ($$"));
        }

        [Fact]
        public async Task TestNotInAwaitUsing2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"await using (var $$"));
        }

        [Fact]
        public async Task TestNotAfterConstLocal()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
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

        [Fact]
        public async Task TestAfterOutKeywordInArgument()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"M(out $$"));
        }

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
        public async Task TestNotAfterRefInStatementContext()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref $$"));
        }

        [Fact]
        public async Task TestNotAfterRefReadonlyInStatementContext()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref readonly $$"));
        }

        [Fact]
        public async Task TestNotAfterRefLocalDeclaration()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref $$ int local;"));
        }

        [Fact]
        public async Task TestNotAfterRefReadonlyLocalDeclaration()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref readonly $$ int local;"));
        }

        [Fact]
        public async Task TestNotAfterRefExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact]
        public async Task TestInParameter1()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    public void M($$)
                }
                """);
        }

        [Fact]
        public async Task TestInParameter2()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    public void M($$ ref)
                }
                """);
        }

        [Fact]
        public async Task TestInParameter3()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    public void M($$ ref int i)
                }
                """);
        }

        [Fact]
        public async Task TestInParameter4()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    public void M($$ ref int i)
                }
                """);
        }

        [Fact]
        public async Task TestInOperatorParameter()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    public static C operator +($$ in C c)
                }
                """);
        }

        [Fact]
        public async Task TestInAnonymousMethodParameter()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M()
                    {
                        var x = delegate ($$) { };
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInParameterAfterThisScoped()
        {
            await VerifyKeywordAsync("""
                static class C
                {
                    static void M(this $$)
                }
                """);
        }

        [Fact]
        public async Task TestNotInExtensionForType()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E for $$
                """);
        }
    }
}
