// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class WhenKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatch()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatchDeclaration1()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch (Exception) $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatchDeclaration2()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch (Exception e) $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCatchDeclarationEmpty()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch () $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterTryBlock()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterFilter1()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception e) when $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterFilter2()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception e) when ($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterFilter3()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception e) when (true) $$"));
        }
    }
}
