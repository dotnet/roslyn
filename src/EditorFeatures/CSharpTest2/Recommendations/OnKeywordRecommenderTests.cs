// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class OnKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestAfterJoinInExpr1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in e $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterJoinInExpr2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in e $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOn1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOn2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOn3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOn4()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 e$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOn5()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 equals $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOn6()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 equals o$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIn()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join a.b c in $$"));
        }
    }
}
