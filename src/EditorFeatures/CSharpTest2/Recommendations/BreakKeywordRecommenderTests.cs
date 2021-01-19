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
        public void TestNotAtRoot()
        {
            VerifyAbsence(
@"$$", options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement()
        {
            VerifyAbsence(
@"System.Console.WriteLine();
$$", options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration()
        {
            VerifyAbsence(
@"int i = 0;
$$", options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestEmptyStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestBeforeStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"$$
return true;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"return true;
$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterBlock(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"if (true) {
}
$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterIf(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"if (true) 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterDo(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"do 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterWhile(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"while (true) 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterFor(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"for (int i = 0; i < 10; i++) 
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterForeach(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v in bar)
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestNotInsideLambda(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in bar) {
   var d = () => {
     $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestNotInsideAnonymousMethod(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in bar) {
   var d = delegate {
     $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestInsideSwitch(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"switch (a) {
    case 0:
       $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestNotInsideSwitchWithLambda(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"switch (a) {
    case 0:
      var d = () => {
        $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestInsideSwitchOutsideLambda(bool topLevelStatement)
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
        public void TestNotAfterBreak(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"break $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInClass()
        {
            VerifyAbsence(@"class C
{
  $$
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterYield(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"yield $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public void TestAfterSwitchInSwitch(bool topLevelStatement)
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
        public void TestAfterBlockInSwitch(bool topLevelStatement)
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
