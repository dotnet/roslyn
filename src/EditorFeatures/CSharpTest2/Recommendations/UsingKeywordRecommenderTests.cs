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
    public class UsingKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass()
        {
            VerifyKeyword(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInEmptyStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAwait()
        {
            VerifyKeyword(
@"class C
{
    async void M()
    {
        await $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAwaitInAssignment()
        {
            VerifyAbsence(
@"class C
{
    async void M()
    {
        _ = await $$
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsingKeyword()
            => VerifyAbsence(@"using $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousUsing()
        {
            VerifyKeyword(
@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            VerifyKeyword(
@"extern alias goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeUsing()
        {
            VerifyKeyword(
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsingAlias()
        {
            VerifyKeyword(
@"using Goo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedTypeDeclaration()
        {
            VerifyAbsence(@"class A {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsingKeyword_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    using $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousUsing_InsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
   using Goo;
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeUsing_InsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
    $$
    using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class A {
      class C {}
      $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeExtern()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
extern alias Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeExtern_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
extern alias Goo;");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestBeforeStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$
return true;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"return true;
$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBlock(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) {
}
$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterIf(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterDo(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"do 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterWhile(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"while (true) 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterFor(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"for (int i = 0; i < 10; i++) 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterForeach(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v in bar)
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsing()
        {
            VerifyAbsence(
@"using $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            VerifyAbsence(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenUsings()
        {
            VerifyKeyword(
@"using Goo;
$$
using Bar;");
        }
    }
}
