// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ParamKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(529127, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529127")]
        public async Task TestNotOfferedInsideArgumentList()
            => VerifyAbsence("class C { void M([$$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(529127, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529127")]
        public async Task TestNotOfferedInsideArgumentList2()
            => VerifyAbsence("delegate void M([$$");

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
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterAttributeInsideClass()
        {
            VerifyAbsence(
@"class C {
    [Goo]
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterMethod()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterProperty()
        {
            VerifyAbsence(
@"class C {
    int Goo {
        get;
    }
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterField()
        {
            VerifyAbsence(
@"class C {
    int Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInAttributeAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event Action<int> Goo;
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInOuterAttribute()
        {
            VerifyAbsence(
@"[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInParameterAttribute()
        {
            VerifyAbsence(
@"class C {
    void Goo([$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPropertyAttribute1()
        {
            VerifyKeyword(
@"class C {
    int Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPropertyAttribute2()
        {
            VerifyKeyword(
@"class C {
    int Goo { get { } [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEventAttribute1()
        {
            VerifyKeyword(
@"class C {
    event Action<int> Goo { [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEventAttribute2()
        {
            VerifyKeyword(
@"class C {
    event Action<int> Goo { add { } [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInTypeParameters()
        {
            VerifyAbsence(
@"class C<[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInInterface()
        {
            VerifyAbsence(
@"interface I {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInStruct()
        {
            VerifyAbsence(
@"struct S {
    [$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEnum()
        {
            VerifyAbsence(
@"enum E {
    [$$");
        }
    }
}
