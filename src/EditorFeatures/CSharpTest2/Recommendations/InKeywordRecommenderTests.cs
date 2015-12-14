// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class InKeywordRecommenderTests : KeywordRecommenderTests
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
        public void NotAfterFrom()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterFromIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterFromAndTypeAndIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from int x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterJoin()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterJoinIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join z $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterJoinAndTypeAndIdentifier()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join int z $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterJoinNotAfterIn()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join z in $$"));
        }

        [WorkItem(544158)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterJoinPredefinedType()
        {
            VerifyAbsence(
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
        public void NotAfterJoinType()
        {
            VerifyAbsence(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from x in y
                join Int32 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForEach()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForEach1()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForEach2()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInForEach()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInForEach1()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInForEach2()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInForEach3()
        {
            VerifyAbsence(AddInsideMethod(
@"foreach (var v in c $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceAfterAngle()
        {
            VerifyKeyword(
@"interface IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IFoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceAfterComma()
        {
            VerifyKeyword(
@"interface IFoo<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceAfterAttribute()
        {
            VerifyKeyword(
@"interface IFoo<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceAfterAngle()
        {
            VerifyKeyword(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceAfterComma()
        {
            VerifyKeyword(
@"delegate void D<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceAfterAttribute()
        {
            VerifyKeyword(
@"delegate void D<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInClassTypeVarianceAfterAngle()
        {
            VerifyAbsence(
@"class IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStructTypeVarianceAfterAngle()
        {
            VerifyAbsence(
@"struct IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInBaseListAfterAngle()
        {
            VerifyAbsence(
@"interface IFoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInGenericMethod()
        {
            VerifyAbsence(
@"interface IFoo {
    void Foo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void From2()
        {
            VerifyKeyword(AddInsideMethod(
@"var q2 = from int x $$ ((IEnumerable)src))"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void From3()
        {
            VerifyKeyword(AddInsideMethod(
@"var q2 = from x $$ ((IEnumerable)src))"));
        }

        [WorkItem(544158)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterFromPredefinedType()
        {
            VerifyAbsence(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from int $$");
        }

        [WorkItem(544158)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterFromType()
        {
            VerifyAbsence(
@"using System;
using System.Linq;
class C {
    void M()
    {
        var q = from Int32 $$");
        }
    }
}
