// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ParamsKeywordRecommenderTests : KeywordRecommenderTests
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
        public void NotAfterAngle()
        {
            VerifyAbsence(
@"interface IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IFoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"interface IFoo<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InterfaceTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"interface IFoo<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceNotAfterAngle()
        {
            VerifyAbsence(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"delegate void D<Foo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DelegateTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"delegate void D<[Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotParamsBaseListAfterAngle()
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
        public void NotAfterParams()
        {
            VerifyAbsence(
@"class C {
    void Foo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOut()
        {
            VerifyAbsence(
@"class C {
    void Foo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterThis()
        {
            VerifyAbsence(
@"static class C {
    static void Foo(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethodOpenParen()
        {
            VerifyKeyword(
@"class C {
    void Foo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethodComma()
        {
            VerifyKeyword(
@"class C {
    void Foo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethodAttribute()
        {
            VerifyKeyword(
@"class C {
    void Foo(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterConstructorOpenParen()
        {
            VerifyKeyword(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterConstructorComma()
        {
            VerifyKeyword(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterConstructorAttribute()
        {
            VerifyKeyword(
@"class C {
    public C(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDelegateOpenParen()
        {
            VerifyKeyword(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDelegateComma()
        {
            VerifyKeyword(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDelegateAttribute()
        {
            VerifyKeyword(
@"delegate void D(int i, [Foo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOperator()
        {
            VerifyAbsence(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDestructor()
        {
            VerifyAbsence(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIndexer()
        {
            VerifyAbsence(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInObjectCreationAfterOpenParen()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      new Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterParamsParam()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      new Bar(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOutParam()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      new Bar(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInObjectCreationAfterComma()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      new Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInObjectCreationAfterSecondComma()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      new Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInObjectCreationAfterSecondNamedParam()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      new Bar(baz: 4, quux: $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInvocationExpression()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInvocationAfterComma()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInvocationAfterSecondComma()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInvocationAfterSecondNamedParam()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
      Bar(baz: 4, quux: $$");
        }
    }
}
