// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class IfKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPreprocessor1()
        {
            VerifyAbsence(AddInsideMethod(
"#if $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void EmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterHash()
        {
            VerifyKeyword(
@"#$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterHashFollowedBySkippedTokens()
        {
            VerifyKeyword(
@"#$$
aeu");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterHashAndSpace()
        {
            VerifyKeyword(
@"# $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideMethod()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BeforeStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$
return true;"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"return true;
$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIf()
        {
            VerifyAbsence(AddInsideMethod(
@"if $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (true) {
  case 0:
    $$
}"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InCaseBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (true) {
  case 0: {
    $$
  }
}"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InDefaultCase()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (true) {
  default:
    $$
}"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InDefaultCaseBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (true) {
  default: {
    $$
  }
}"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterLabel()
        {
            VerifyKeyword(AddInsideMethod(
@"label:
  $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDoBlock()
        {
            VerifyAbsence(AddInsideMethod(
@"do {
}
$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InActiveRegion1()
        {
            VerifyKeyword(AddInsideMethod(
@"#if true
$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InActiveRegion2()
        {
            VerifyKeyword(AddInsideMethod(
@"#if true

$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterElse()
        {
            VerifyKeyword(AddInsideMethod(
@"if (foo) {
} else $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatch()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatchDeclaration1()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception) $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatchDeclaration2()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception e) $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatchDeclarationEmpty()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch () $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterTryBlock()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} $$"));
        }
    }
}
