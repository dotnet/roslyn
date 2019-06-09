// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
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
    $$ public void goo() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync(@"interface C
{
    $$
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
    void goo()
    {
        goo($$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInParameter()
        {
            await VerifyAbsenceAsync(@"class C
{
    void goo($$)
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
namespace Goo
{
    $$
}");
        }

        [WorkItem(578069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578069")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInNamespace()
        {
            await VerifyAbsenceAsync(@"
namespace Goo
{
    partial $$
}");
        }

        [WorkItem(578750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInClass()
        {
            await VerifyAbsenceAsync(@"
class Goo
{
    partial $$
}");
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction()
        {
            await VerifyKeywordAsync(@"
class Goo
{
    public void M()
    {
        $$
    }
}");
        }

        [Fact]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction2()
        {
            await VerifyKeywordAsync(@"
class Goo
{
    public void M()
    {
        unsafe $$
    }
}");
        }

        [Fact]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction3()
        {
            await VerifyKeywordAsync(@"
class Goo
{
    public void M()
    {
        unsafe $$ void L() { }
    }
}");
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction4()
        {
            await VerifyKeywordAsync(@"
class Goo
{
    public void M()
    {
        $$ void L() { }
    }
}");
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction5()
        {
            await VerifyKeywordAsync(@"
class Goo
{
    public void M(Action<int> a)
    {
        M(async () =>
        {
            $$
        });
    }
}");
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction6()
        {
            await VerifyAbsenceAsync(@"
class Goo
{
    public void M()
    {
        int $$
    }
}");
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction7()
        {
            await VerifyAbsenceAsync(@"
class Goo
{
    public void M()
    {
        static $$
    }
}");
        }
    }
}
