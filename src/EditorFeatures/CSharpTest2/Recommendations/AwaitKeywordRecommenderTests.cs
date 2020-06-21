// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class AwaitKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInTypeContext()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    $$
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestStatementInMethod(bool isAsync, bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", isAsync: isAsync, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestExpressionInAsyncMethod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var z = $$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestUsingStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestUsingDirective()
            => await VerifyAbsenceAsync("using $$");

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestForeachStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInQuery(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var z = from a in ""char""
          select $$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFinally(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try { }
finally { $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInCatch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try { }
catch { $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInLock(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock(this) { $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInAsyncLambdaInCatch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try { }
catch { var z = async () => $$ }", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAwaitInLock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"lock($$", isAsync: true, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
