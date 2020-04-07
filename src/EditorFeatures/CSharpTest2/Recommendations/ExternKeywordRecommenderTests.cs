// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ExternKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
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
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticInStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"static $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAttributesInStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Attr] $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAttributesInSwitchCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (c)
{
    case 0:
         [Foo]
         $$
}"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAttributesAndStaticInStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Attr] static $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenAttributesAndReturnStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Attr]
$$
return x;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenAttributesAndLocalDeclarationStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Attr]
$$
x y = bar();"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenAttributesAndAwaitExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Attr]
$$
await bar;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenAttributesAndAssignmentStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Foo]
$$
y = bar();"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenAttributesAndCallStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"[Foo]
$$
bar();"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExternInStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"extern $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExternKeyword()
            => await VerifyAbsenceAsync(@"extern $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousExternAlias()
        {
            await VerifyKeywordAsync(
@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeDeclaration()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExternKeyword_InsideNamespace()
        {
            await VerifyAbsenceAsync(@"namespace N {
    extern $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousExternAlias_InsideNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N {
   extern alias Goo;
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsing_InsideNamespace()
        {
            await VerifyAbsenceAsync(@"namespace N {
    using Goo;
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMember_InsideNamespace()
        {
            await VerifyAbsenceAsync(@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespace_InsideNamespace()
        {
            await VerifyAbsenceAsync(@"namespace N {
    namespace N {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInClass()
        {
            await VerifyKeywordAsync(
@"class C {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInStruct()
        {
            await VerifyKeywordAsync(
@"struct S {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInterface()
        {
            await VerifyKeywordAsync(
@"interface I {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAbstract()
        {
            await VerifyAbsenceAsync(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExtern()
        {
            await VerifyAbsenceAsync(
@"class C {
    extern $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic()
        {
            await VerifyKeywordAsync(
@"class C {
    public $$");
        }
    }
}
