// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class AwaitKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInTypeContext()
        {
            VerifyAbsence(@"
class Program
{
    $$
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InSynchronousMethod()
        {
            VerifyKeyword(@"
class Program
{
    void foo()
    {
        $$
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void StatementInAsyncMethod()
        {
            VerifyKeyword(@"
class Program
{
    async void foo()
    {
        $$
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void ExpressionInAsyncMethod()
        {
            VerifyKeyword(@"
class Program
{
    async void foo()
    {
        var z = $$
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInQuery()
        {
            VerifyAbsence(@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InFinally()
        {
            VerifyKeyword(@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InCatch()
        {
            VerifyKeyword(@"
class Program
{
    async void foo()
    {
        try { }
        catch { $$ } 
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInLock()
        {
            VerifyAbsence(@"
class Program
{
    async void foo()
    {
       lock(this) { $$ } 
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InAsyncLambdaInCatch()
        {
            VerifyKeyword(@"
class Program
{
    async void foo()
    {
        try { }
        catch { var z = async () => $$ } 
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AwaitInLock()
        {
            VerifyKeyword(@"
class Program
{
    async void foo()
    {
        lock($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InGlobalStatement()
        {
            const string text = @"$$";
            VerifyKeyword(SourceCodeKind.Script, text);
        } 
    }
}
