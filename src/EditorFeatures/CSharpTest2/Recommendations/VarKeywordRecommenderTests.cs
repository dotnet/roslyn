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
    public class VarKeywordRecommenderTests : RecommenderTests
    {
        private readonly VarKeywordRecommender _recommender = new VarKeywordRecommender();

        public VarKeywordRecommenderTests()
        {
            this.keywordText = "var";
            this.RecommendKeywords = (position, context) => _recommender.RecommendKeywords(position, context, CancellationToken.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterStackAlloc()
        {
            VerifyAbsence(
@"class C {
     int* goo = stackalloc $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInFixedStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"fixed ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInDelegateReturnType()
        {
            VerifyAbsence(
@"public delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInCastType()
        {
            // Could be a deconstruction
            VerifyKeyword(AddInsideMethod(
@"var str = (($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInCastType2()
        {
            // Could be a deconstruction
            VerifyKeyword(AddInsideMethod(
@"var str = (($$)items) as string;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestBeforeStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterLock()
        {
            VerifyAbsence(AddInsideMethod(
@"lock $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterLock2()
        {
            VerifyAbsence(AddInsideMethod(
@"lock ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterLock3()
        {
            VerifyAbsence(AddInsideMethod(
@"lock (l$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInClass()
        {
            VerifyAbsence(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFor()
        {
            VerifyKeyword(AddInsideMethod(
@"for ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInFor()
        {
            VerifyAbsence(AddInsideMethod(
@"for (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFor2()
        {
            VerifyKeyword(AddInsideMethod(
@"for ($$;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFor3()
        {
            VerifyKeyword(AddInsideMethod(
@"for ($$;;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterVar()
        {
            VerifyAbsence(AddInsideMethod(
@"var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInForEach()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInForEach()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInAwaitForEach()
        {
            VerifyKeyword(AddInsideMethod(
@"await foreach ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInAwaitForEach()
        {
            VerifyAbsence(AddInsideMethod(
@"await foreach (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForEachRefLoop0()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForEachRefLoop1()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (ref $$ x"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForEachRefLoop2()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (ref v$$ x"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForEachRefReadonlyLoop0()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (ref readonly $$ x"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForRefLoop0()
        {
            VerifyKeyword(AddInsideMethod(
@"for (ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForRefLoop1()
        {
            VerifyKeyword(AddInsideMethod(
@"for (ref v$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForRefReadonlyLoop0()
        {
            VerifyKeyword(AddInsideMethod(
@"for (ref readonly $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(37223, "https://github.com/dotnet/roslyn/issues/37223")]
        public void TestInForRefReadonlyLoop1()
        {
            VerifyKeyword(AddInsideMethod(
@"for (ref readonly v$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInUsing()
        {
            VerifyKeyword(AddInsideMethod(
@"using ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsing()
        {
            VerifyAbsence(AddInsideMethod(
@"using (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInAwaitUsing()
        {
            VerifyKeyword(AddInsideMethod(
@"await using ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInAwaitUsing()
        {
            VerifyAbsence(AddInsideMethod(
@"await using (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterConstLocal()
        {
            VerifyKeyword(AddInsideMethod(
@"const $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterConstField()
        {
            VerifyAbsence(
@"class C {
    const $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(12121, "https://github.com/dotnet/roslyn/issues/12121")]
        public void TestAfterOutKeywordInArgument()
        {
            VerifyKeyword(AddInsideMethod(
@"M(out $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(12121, "https://github.com/dotnet/roslyn/issues/12121")]
        public void TestAfterOutKeywordInParameter()
        {
            VerifyAbsence(
@"class C {
     void M1(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestVarPatternInSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch(o)
    {
        case $$
    }
"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestVarPatternInIs()
            => VerifyKeyword(AddInsideMethod("var b = o is $$ "));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterRefInMemberContext()
        {
            VerifyAbsence(
@"class C {
    ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterRefReadonlyInMemberContext()
        {
            VerifyAbsence(
@"class C {
    ref readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRefInStatementContext()
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRefReadonlyInStatementContext()
        {
            VerifyKeyword(AddInsideMethod(
@"ref readonly $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRefLocalDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$ int local;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRefReadonlyLocalDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"ref readonly $$ int local;"));
        }

        // For a local function, we can't add any tests - sometimes the keyword is offered and sometimes it's not,
        // depending on whether the keyword is partially written or not. This is because a partially written keyword
        // causes this to be parsed as a local declaration instead. We can't add either test because
        // VerifyKeywordAsync & VerifyAbsenceAsync check for both cases - with the keyword partially written and without.

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterRefExpression()
        {
            VerifyAbsence(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [WorkItem(10170, "https://github.com/dotnet/roslyn/issues/10170")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInPropertyPattern()
        {
            VerifyKeyword(
@"
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
}");
        }
    }
}
