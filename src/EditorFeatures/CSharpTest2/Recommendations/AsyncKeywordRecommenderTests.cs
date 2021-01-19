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
        public void TestMethodDeclaration1()
        {
            VerifyKeyword(@"class C
{
    $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMethodDeclaration2()
        {
            VerifyKeyword(@"class C
{
    public $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMethodDeclaration3()
        {
            VerifyKeyword(@"class C
{
    $$ public void goo() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideInterface()
        {
            VerifyKeyword(@"interface C
{
    $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMethodDeclarationInGlobalStatement1()
        {
            const string text = @"$$";
            VerifyKeyword(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMethodDeclarationInGlobalStatement2()
        {
            const string text = @"public $$";
            VerifyKeyword(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestExpressionContext()
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
        public void TestNotInParameter()
        {
            VerifyAbsence(@"class C
{
    void goo($$)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestBeforeLambda()
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
        public void TestBeforeStaticLambda()
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
        public void TestAfterStaticInLambda()
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
        public void TestAfterStaticInExpression()
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
        public void TestAfterDuplicateStaticInExpression()
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
        public void TestAfterStaticAsyncInExpression()
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
        public void TestAfterAsyncStaticInExpression()
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
        public void TestInAttribute()
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
        public void TestInAttributeArgument()
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
        public void TestBeforeStaticInExpression()
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
        public void TestNotIfAlreadyAsync2()
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
        public void TestNotInNamespace()
        {
            VerifyAbsence(@"
namespace Goo
{
    $$
}");
        }

        [WorkItem(578069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578069")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPartialInNamespace()
        {
            VerifyAbsence(@"
namespace Goo
{
    partial $$
}");
        }

        [WorkItem(578750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterPartialInClass()
        {
            VerifyAbsence(@"
class Goo
{
    partial $$
}");
        }

        [WorkItem(578750, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAttribute()
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
        public void TestLocalFunction(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(14525, "https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction3(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe $$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction4(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction5()
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
        public void TestLocalFunction6(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"int $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(8616, "https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestLocalFunction7(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
