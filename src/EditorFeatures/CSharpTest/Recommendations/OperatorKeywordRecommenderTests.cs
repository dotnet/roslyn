// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class OperatorKeywordRecommenderTests : KeywordRecommenderTests
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
        public void AfterImplicit()
        {
            VerifyKeyword(
@"class Foo {
    public static implicit $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterExplicit()
        {
            VerifyKeyword(
@"class Foo {
    public static explicit $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterType()
        {
            VerifyAbsence(
@"class Foo {
    int $$");
        }

        [WorkItem(542271)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPublicStaticType()
        {
            VerifyAbsence(
@"class Foo {
    public static int $$");
        }

        [WorkItem(542271)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPublicStaticExternType()
        {
            VerifyAbsence(
@"class Foo {
    public static extern int $$");
        }

        [WorkItem(542271)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGenericType()
        {
            VerifyAbsence(
@"class Foo {
    public static IList<int> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInInterface()
        {
            VerifyAbsence(
@"interface Foo {
    public static int $$");
        }
    }
}
