// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class DescendingKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOrderByExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSecondExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby x, y $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenExprs()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby x, y $$, z"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDot()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          orderby x.$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterComma()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          orderby x, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCloseParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby x.ToString() $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCloseBracket()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby x.ToString()[0] $$"));
        }
    }
}
