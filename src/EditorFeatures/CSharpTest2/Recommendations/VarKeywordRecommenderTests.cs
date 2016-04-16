// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Text;
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
            this.RecommendKeywordsAsync = (position, context) => _recommender.RecommendKeywordsAsync(position, context, CancellationToken.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStackAlloc()
        {
            await VerifyAbsenceAsync(
@"class C {
     int* foo = stackalloc $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInFixedStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"fixed ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDelegateReturnType()
        {
            await VerifyAbsenceAsync(
@"public delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCastType()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var str = (($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCastType2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var str = (($$)items) as string;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterLock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterLock2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterLock3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock (l$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFor()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInFor()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFor2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFor3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVar()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInForEach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInForEach()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUsing()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsing()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstLocal()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"const $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConstField()
        {
            await VerifyAbsenceAsync(
@"class C {
    const $$");
        }
    }
}
