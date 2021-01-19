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
            VerifyKeyword(@"class C
{
    $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclaration2()
        {
            VerifyKeyword(@"class C
{
    public $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclaration3()
        {
            VerifyKeyword(@"class C
{
    $$ public void goo() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            VerifyKeyword(@"interface C
{
    $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclarationInGlobalStatement1()
        {
            const string text = @"$$";
            VerifyKeyword(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMethodDeclarationInGlobalStatement2()
        {
            const string text = @"public $$";
            VerifyKeyword(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExpressionContext()
        {
            VerifyKeyword(@"class C
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
            VerifyAbsence(@"class C
{
    void goo($$)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeLambda()
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
        public async Task TestBeforeStaticLambda()
        {
            VerifyKeyword(@"
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
            VerifyKeyword(@"
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
            VerifyKeyword(@"
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
            VerifyKeyword(@"
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
            VerifyAbsence(@"
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
            VerifyAbsence(@"
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
            VerifyAbsence(@"
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
            VerifyAbsence(@"
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
            VerifyKeyword(@"
class Program
{
    static void Main(string[] args)
    {
        var z = $$ static
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotIfAlreadyAsync2()
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

        [WorkItem(578061, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578061")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInNamespace()
        {
            VerifyAbsence(@"
namespace Goo
{
    $$
}");
        }

        [WorkItem(578069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578069")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInNamespace()
        {
            VerifyAbsence(@"
namespace Goo
{
    partial $$
}");
        }

        [WorkItem(578750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartialInClass()
        {
            VerifyAbsence(@"
class Goo
{
    partial $$
}");
        }

        [WorkItem(578750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAttribute()
        {
            VerifyKeyword(@"
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
            VerifyKeyword(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction3(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe $$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction4(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction5()
        {
            VerifyKeyword(@"
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
            VerifyAbsence(AddInsideMethod(
@"int $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLocalFunction7(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
