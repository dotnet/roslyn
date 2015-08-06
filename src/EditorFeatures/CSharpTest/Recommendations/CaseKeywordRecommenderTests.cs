// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class CaseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterExpr()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = foo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDottedName()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = foo.Current $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    case 0:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDefault()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterOneStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterTwoStatements()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      Console.WriteLine();
      Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default: {
    }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIfElse()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      if (foo) {
      } else {
      }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIncompleteStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default:
       Console.WriteLine(
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInsideBlock()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default: {
      $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIf()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      if (foo)
        Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIf()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (expr) {
    default:
      if (foo)
        $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterWhile()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      while (true) {
      }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGotoInSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (expr) {
    default:
      goto $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGotoOutsideSwitch()
        {
            VerifyAbsence(AddInsideMethod(
@"goto $$"));
        }
    }
}
