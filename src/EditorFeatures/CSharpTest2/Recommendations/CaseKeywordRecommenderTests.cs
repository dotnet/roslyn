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
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterExpr()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = goo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDottedName()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = goo.Current $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case 0:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDefault()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPatternCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case String s:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterOneStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterOneStatementPatternCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case String s:
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTwoStatements()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      Console.WriteLine();
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default: {
    }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterBlockPatternCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case String s: {
    }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIfElse()
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
        public void TestNotAfterIncompleteStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default:
       Console.WriteLine(
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInsideBlock()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default: {
      $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterIf()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      if (goo)
        Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterIf()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default:
      if (goo)
        $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterWhile()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      while (true) {
      }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGotoInSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      goto $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGotoOutsideSwitch()
        {
            VerifyAbsence(AddInsideMethod(
@"goto $$"));
        }
    }
}
