// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class UsingKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUsingKeyword()
        {
            VerifyAbsence(@"using $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousUsing()
        {
            VerifyKeyword(
@"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterExtern()
        {
            VerifyKeyword(
@"extern alias foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BeforeUsing()
        {
            VerifyKeyword(
@"$$
using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsingAlias()
        {
            VerifyKeyword(
@"using Foo = Bar;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterTypeDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedTypeDeclaration()
        {
            VerifyAbsence(@"class A {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUsingKeyword_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    using $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPreviousUsing_InsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
   using Foo;
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BeforeUsing_InsideNamespace()
        {
            VerifyKeyword(
@"namespace N {
    $$
    using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class C {}
    $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedMember_InsideNamespace()
        {
            VerifyAbsence(@"namespace N {
    class A {
      class C {}
      $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeExtern()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
extern alias Foo;");
        }

        [WpfFact(Skip = "528041"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeExtern_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
extern alias Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BeforeStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterIf()
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterDo()
        {
            VerifyKeyword(AddInsideMethod(
@"do 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterWhile()
        {
            VerifyKeyword(AddInsideMethod(
@"while (true) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterFor()
        {
            VerifyKeyword(AddInsideMethod(
@"for (int i = 0; i < 10; i++) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterForeach()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v in bar)
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUsing()
        {
            VerifyAbsence(AddInsideMethod(
@"using $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInClass()
        {
            VerifyAbsence(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BetweenUsings()
        {
            VerifyKeyword(AddInsideMethod(
@"using Foo;
$$
using Bar;"));
        }
    }
}
