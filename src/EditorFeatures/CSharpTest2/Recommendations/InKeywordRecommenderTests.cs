// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class InKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotAfterFrom()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFromIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFromAndTypeAndIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from int x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterJoin()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterJoinIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join z $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterJoinAndTypeAndIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join int z $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterJoinNotAfterIn()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          join z in $$"));
        }

        [WorkItem(544158)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterJoinPredefinedType()
        {
            await VerifyAbsenceAsync(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from x in y
                join int $$");
        }

        [WorkItem(544158)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterJoinType()
        {
            await VerifyAbsenceAsync(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from x in y
                join Int32 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInForEach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInForEach1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInForEach2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInForEach()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInForEach1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInForEach2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInForEach3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in c $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceAfterAngle()
        {
            await VerifyKeywordAsync(
@"interface IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            await VerifyAbsenceAsync(
@"interface IFoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceAfterComma()
        {
            await VerifyKeywordAsync(
@"interface IFoo<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceAfterAttribute()
        {
            await VerifyKeywordAsync(
@"interface IFoo<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceAfterAngle()
        {
            await VerifyKeywordAsync(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceAfterComma()
        {
            await VerifyKeywordAsync(
@"delegate void D<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceAfterAttribute()
        {
            await VerifyKeywordAsync(
@"delegate void D<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClassTypeVarianceAfterAngle()
        {
            await VerifyAbsenceAsync(
@"class IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStructTypeVarianceAfterAngle()
        {
            await VerifyAbsenceAsync(
@"struct IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInBaseListAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IFoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInGenericMethod()
        {
            await VerifyAbsenceAsync(
@"interface IFoo {
    void Foo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestFrom2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q2 = from int x $$ ((IEnumerable)src))"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestFrom3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q2 = from x $$ ((IEnumerable)src))"));
        }

        [WorkItem(544158)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterFromPredefinedType()
        {
            await VerifyAbsenceAsync(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from int $$");
        }

        [WorkItem(544158)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterFromType()
        {
            await VerifyAbsenceAsync(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from Int32 $$");
        }
    }
}
