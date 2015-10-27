﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class DescendingKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
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
        public void NotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterOrderByExpr()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby x $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSecondExpr()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby x, y $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BetweenExprs()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby x, y $$, z"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDot()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          orderby x.$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterComma()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          orderby x, $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCloseParen()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby x.ToString() $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCloseBracket()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby x.ToString()[0] $$"));
        }
    }
}
