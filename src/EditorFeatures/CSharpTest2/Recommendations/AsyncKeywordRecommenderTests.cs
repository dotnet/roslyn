// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class AsyncKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclaration1()
        {
            await VerifyKeywordAsync(@"class C
{
    $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclaration2()
        {
            await VerifyKeywordAsync(@"class C
{
    public $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclaration3()
        {
            await VerifyKeywordAsync(@"class C
{
    $$ public void foo() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclarationInGlobalStatement1()
        {
            const string text = @"$$";
            await VerifyKeywordAsync(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclarationInGlobalStatement2()
        {
            const string text = @"public $$";
            await VerifyKeywordAsync(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExpressionContext()
        {
            await VerifyKeywordAsync(@"class C
{
    void foo()
    {
        foo($$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInParameter()
        {
            await VerifyAbsenceAsync(@"class C
{
    void foo($$)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeLambda()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z =  $$ () => 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotIfAlreadyAsync2()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z = async $$ () => 2;
    }
}");
        }

        [WorkItem(578061, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578061")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInNamespace()
        {
            await VerifyAbsenceAsync(@"
namespace Foo
{
    $$
}");
        }

        [WorkItem(578069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578069")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInNamespace()
        {
            await VerifyAbsenceAsync(@"
namespace Foo
{
    partial $$
}");
        }

        [WorkItem(578750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInClass()
        {
            await VerifyAbsenceAsync(@"
class Foo
{
    partial $$
}");
        }
    }
}
