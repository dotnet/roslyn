// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class BreakKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot()
        {
            VerifyAbsence(
@"$$", options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement()
        {
            VerifyAbsence(
@"System.Console.WriteLine();
$$", options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration()
        {
            VerifyAbsence(
@"int i = 0;
$$", options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestBeforeStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"$$
return true;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"return true;
$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBlock(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"if (true) {
}
$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterIf(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
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

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInsideLambda(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in bar) {
   var d = () => {
     $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInsideAnonymousMethod(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in bar) {
   var d = delegate {
     $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInsideSwitch(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"switch (a) {
    case 0:
       $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInsideSwitchWithLambda(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"switch (a) {
    case 0:
      var d = () => {
        $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInsideSwitchOutsideLambda(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"switch (a) {
    case 0:
      var d = () => {
      };
      $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotAfterBreak(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"break $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            VerifyAbsence(@"class C
{
  $$
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterYield(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"yield $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterSwitchInSwitch(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      switch (expr) {
      }
      $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBlockInSwitch(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      {
      }
      $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
