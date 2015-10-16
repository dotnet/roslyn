// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ParamKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(529127)]
        public void TestNotOfferedInsideArgumentList()
        {
            VerifyAbsence("class C { void M([$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(529127)]
        public void TestNotOfferedInsideArgumentList2()
        {
            VerifyAbsence("delegate void M([$$");
        }

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
        public void NotInAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInAttributeAfterAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [Foo]
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInAttributeAfterMethod()
        {
            VerifyAbsence(
@"class C {
    void Foo() {
    }
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInAttributeAfterProperty()
        {
            VerifyAbsence(
@"class C {
    int Foo {
        get;
    }
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInAttributeAfterField()
        {
            VerifyAbsence(
@"class C {
    int Foo;
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInAttributeAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event Action<int> Foo;
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInOuterAttribute()
        {
            VerifyAbsence(
@"[$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInParameterAttribute()
        {
            VerifyAbsence(
@"class C {
    void Foo([$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InPropertyAttribute1()
        {
            VerifyKeyword(
@"class C {
    int Foo { [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InPropertyAttribute2()
        {
            VerifyKeyword(
@"class C {
    int Foo { get { } [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InEventAttribute1()
        {
            VerifyKeyword(
@"class C {
    event Action<int> Foo { [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InEventAttribute2()
        {
            VerifyKeyword(
@"class C {
    event Action<int> Foo { add { } [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInTypeParameters()
        {
            VerifyAbsence(
@"class C<[$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInterface()
        {
            VerifyAbsence(
@"interface I {
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStruct()
        {
            VerifyAbsence(
@"struct S {
    [$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEnum()
        {
            VerifyAbsence(
@"enum E {
    [$$");
        }
    }
}
