// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class BreakKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDo()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"do 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhile()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"while (true) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFor()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for (int i = 0; i < 10; i++) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterForeach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v in bar)
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideLambda()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in bar) {
   var d = () => {
     $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideAnonymousMethod()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in bar) {
   var d = delegate {
     $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (a) {
    case 0:
       $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideSwitchWithLambda()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"switch (a) {
    case 0:
      var d = () => {
        $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSwitchOutsideLambda()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (a) {
    case 0:
      var d = () => {
      };
      $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterBreak()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"break $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterYield()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"yield $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSwitchInSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (expr) {
    default:
      switch (expr) {
      }
      $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlockInSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (expr) {
    default:
      {
      }
      $$"));
        }
    }
}
