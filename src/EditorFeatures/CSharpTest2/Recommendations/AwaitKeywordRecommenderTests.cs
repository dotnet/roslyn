// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInSynchronousMethod()
        {
            await VerifyKeywordAsync(@"
class Program
{
    void foo()
    {
        $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestStatementInAsyncMethod()
        {
            await VerifyKeywordAsync(@"
class Program
{
    async void foo()
    {
        $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExpressionInAsyncMethod()
        {
            await VerifyKeywordAsync(@"
class Program
{
    async void foo()
    {
        var z = $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInQuery()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    async void foo()
    {
        var z = from a in ""char""
                select $$
    }
}");
        }

        [WorkItem(907052)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFinally()
        {
            await VerifyKeywordAsync(@"
class Program
{
    async void foo()
    {
        try { }
        finally { $$ } 
    }
}");
        }

        [WorkItem(907052)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCatch()
        {
            await VerifyKeywordAsync(@"
class Program
{
    async void foo()
    {
        try { }
        catch { $$ } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInLock()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    async void foo()
    {
       lock(this) { $$ } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAsyncLambdaInCatch()
        {
            await VerifyKeywordAsync(@"
class Program
{
    async void foo()
    {
        try { }
        catch { var z = async () => $$ } 
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAwaitInLock()
        {
            await VerifyKeywordAsync(@"
class Program
{
    async void foo()
    {
        lock($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInGlobalStatement()
        {
            const string text = @"$$";
            await VerifyKeywordAsync(SourceCodeKind.Script, text);
        }
    }
}
