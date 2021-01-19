// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ParamsKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IGoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"interface IGoo<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"interface IGoo<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            VerifyAbsence(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"delegate void D<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"delegate void D<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotParamsBaseListAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInGenericMethod()
        {
            VerifyAbsence(
@"interface IGoo {
    void Goo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterParams()
        {
            VerifyAbsence(
@"class C {
    void Goo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOut()
        {
            VerifyAbsence(
@"class C {
    void Goo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterThis()
        {
            VerifyAbsence(
@"static class C {
    static void Goo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodOpenParen()
        {
            VerifyKeyword(
@"class C {
    void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodComma()
        {
            VerifyKeyword(
@"class C {
    void Goo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodAttribute()
        {
            VerifyKeyword(
@"class C {
    void Goo(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorOpenParen()
        {
            VerifyKeyword(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorComma()
        {
            VerifyKeyword(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorAttribute()
        {
            VerifyKeyword(
@"class C {
    public C(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateOpenParen()
        {
            VerifyKeyword(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateComma()
        {
            VerifyKeyword(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateAttribute()
        {
            VerifyKeyword(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOperator()
        {
            VerifyAbsence(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDestructor()
        {
            VerifyAbsence(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIndexer()
        {
            VerifyKeyword(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectCreationAfterOpenParen()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterParamsParam()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOutParam()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectCreationAfterComma()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectCreationAfterSecondComma()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectCreationAfterSecondNamedParam()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar(baz: 4, quux: $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInvocationExpression()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInvocationAfterComma()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInvocationAfterSecondComma()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInvocationAfterSecondNamedParam()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      Bar(baz: 4, quux: $$");
        }
    }
}
