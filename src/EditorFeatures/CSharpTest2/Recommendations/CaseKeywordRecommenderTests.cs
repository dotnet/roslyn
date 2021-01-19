// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class CaseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExpr()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = goo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDottedName()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = goo.Current $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case 0:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDefault()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPatternCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case String s:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOneStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOneStatementPatternCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case String s:
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTwoStatements()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      Console.WriteLine();
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default: {
    }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlockPatternCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case String s: {
    }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfElse()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      if (goo) {
      } else {
      }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIncompleteStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default:
       Console.WriteLine(
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideBlock()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default: {
      $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIf()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      if (goo)
        Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIf()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default:
      if (goo)
        $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhile()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      while (true) {
      }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGotoInSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      goto $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGotoOutsideSwitch()
        {
            VerifyAbsence(AddInsideMethod(
@"goto $$"));
        }
    }
}
