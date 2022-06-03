// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        public async Task TestMethodDeclarationAsyncAfterCursor()
        {
            await VerifyKeywordAsync(@"class C
{
    public $$ async void goo() { }
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
        public async Task TestBeforeStaticLambda()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z =  $$ static () => 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticInLambda()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z =  static $$ () => 2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticInExpression()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z = static $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDuplicateStaticInExpression()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z = static static $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticAsyncInExpression()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z = static async $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAsyncStaticInExpression()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z = async static $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttribute()
        {
            await VerifyAbsenceAsync(@"
class C
{
    [$$
    void M()
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttributeArgument()
        {
            await VerifyAbsenceAsync(@"
class C
{
    [Attr($$
    void M()
    {
    }
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeStaticInExpression()
        {
            await VerifyKeywordAsync(@"
class Program
{
    static void Main(string[] args)
    {
        var z = $$ static
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotIfAlreadyAsyncInLambda()
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

        [WorkItem(60340, "https://github.com/dotnet/roslyn/issues/60340")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotIfAlreadyAsyncBeforeOtherMember()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    async $$    

    public void M() {}
}");
        }

        [WorkItem(60340, "https://github.com/dotnet/roslyn/issues/60340")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotIfAlreadyAsyncAsLastMember()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    async $$
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

        [WorkItem(578750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAttribute()
        {
            await VerifyKeywordAsync(@"
class Goo
{
    [Attr] $$
}");
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"unsafe $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"unsafe $$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction4(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
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

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction6(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"int $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction7(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
