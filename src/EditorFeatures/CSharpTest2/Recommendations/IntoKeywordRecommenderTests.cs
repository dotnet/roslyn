// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class IntoKeywordRecommenderTests : KeywordRecommenderTests
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
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInSelectMemberExpressionOnlyADot()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.$$"));
        }
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInSelectMemberExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var y = from x in new [] { 1,2,3 } select x.i$$"));
        }
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterJoinRightExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in e on o1 equals o2 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterJoinRightExpr_NotAfterInto()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in o1 equals o2 into $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEquals()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in o1 equals $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSelectClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          select z
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSelectClauseWithMemberExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          select z.i
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSelectClause_NotAfterInto()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          select z
          into $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGroupClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          group z by w
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGroupClause_NotAfterInto()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          group z by w
          into $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSelect()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          select $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGroupKeyword()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          group $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGroupExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          group x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGroupBy()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          group x by $$"));
        }
    }
}
