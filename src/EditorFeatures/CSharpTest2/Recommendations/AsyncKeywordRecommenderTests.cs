// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class AsyncKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void MethodDeclaration1()
        {
            VerifyKeyword(@"class C
{
    $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void MethodDeclaration2()
        {
            VerifyKeyword(@"class C
{
    public $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void MethodDeclaration3()
        {
            VerifyKeyword(@"class C
{
    $$ public void foo() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void MethodDeclarationInGlobalStatement1()
        {
            const string text = @"$$";
            VerifyKeyword(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void MethodDeclarationInGlobalStatement2()
        {
            const string text = @"public $$";
            VerifyKeyword(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void ExpressionContext()
        {
            VerifyKeyword(@"class C
{
    void foo()
    {
        foo($$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInParameter()
        {
            VerifyAbsence(@"class C
{
    void foo($$)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BeforeLambda()
        {
            VerifyKeyword(@"
class Program
{
    static void Main(string[] args)
    {
        var z =  $$ () => 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotIfAlreadyAsync2()
        {
            VerifyAbsence(@"
class Program
{
    static void Main(string[] args)
    {
        var z = async $$ () => 2;
    }
}");
        }

        [WorkItem(578061)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInNamespace()
        {
            VerifyAbsence(@"
namespace Foo
{
    $$
}");
        }

        [WorkItem(578069)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPartialInNamespace()
        {
            VerifyAbsence(@"
namespace Foo
{
    partial $$
}");
        }

        [WorkItem(578750)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPartialInClass()
        {
            VerifyAbsence(@"
class Foo
{
    partial $$
}");
        }
    }
}
