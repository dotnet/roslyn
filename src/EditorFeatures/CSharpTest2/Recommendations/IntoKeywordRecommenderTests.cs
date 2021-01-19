// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class IntoKeywordRecommenderTests : KeywordRecommenderTests
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
        public void TestNotInSelectMemberExpressionOnlyADot()
        {
            VerifyAbsence(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.$$"));
        }
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInSelectMemberExpression()
        {
            VerifyAbsence(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.i$$"));
        }
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterJoinRightExpr()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in e on o1 equals o2 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterJoinRightExpr_NotAfterInto()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in o1 equals o2 into $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterEquals()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in o1 equals $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterSelectClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          select z
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterSelectClauseWithMemberExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          select z.i
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterSelectClause_NotAfterInto()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          select z
          into $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGroupClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          group z by w
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGroupClause_NotAfterInto()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          group z by w
          into $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterSelect()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          select $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGroupKeyword()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          group $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGroupExpression()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          group x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGroupBy()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          group x by $$"));
        }
    }
}
