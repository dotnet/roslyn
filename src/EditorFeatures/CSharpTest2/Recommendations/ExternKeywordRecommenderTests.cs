// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ExternKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass()
        {
            VerifyKeyword(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement()
        {
            VerifyKeyword(
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            VerifyKeyword(
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInEmptyStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterStaticInStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterAttributesInStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"[Attr] $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterAttributesInSwitchCase(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"switch (c)
{
    case 0:
         [Foo]
         $$
}", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterAttributesAndStaticInStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"[Attr] static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndReturnStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"[Attr]
$$
return x;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndLocalDeclarationStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"[Attr]
$$
x y = bar();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndAwaitExpression(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"[Attr]
$$
await bar;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndAssignmentStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"[Foo]
$$
y = bar();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestBetweenAttributesAndCallStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"[Foo]
$$
bar();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotAfterExternInStatement(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"extern $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExternKeyword()
            => VerifyAbsence(@"extern $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousExternAlias()
        {
            VerifyKeyword(
@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            VerifyKeyword(@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNamespace()
        {
            VerifyKeyword(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExternKeyword_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    extern $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousExternAlias_InsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
   extern alias Goo;
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsing_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    using Goo;
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespace_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    namespace N {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInClass()
        {
            VerifyKeyword(
@"class C {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInStruct()
        {
            VerifyKeyword(
@"struct S {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInterface()
        {
            VerifyKeyword(
@"interface I {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAbstract()
        {
            VerifyAbsence(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExtern()
        {
            VerifyAbsence(
@"class C {
    extern $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }
    }
}
