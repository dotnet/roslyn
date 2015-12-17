// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class TypeOfKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(543541)]
        public void TestOfferedInAttributeConstructorArgumentList()
        {
            VerifyKeyword("using System.ComponentModel; [DefaultValue($$ class C { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
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

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInConstMemberInitializer1()
        {
            VerifyAbsence(
@"class E {
    const int a = $$
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInConstLocalInitializer1()
        {
            VerifyAbsence(
@"class E {
  void Foo() {
    const int a = $$
  }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InMemberInitializer1()
        {
            VerifyKeyword(
@"class E {
    int a = $$
}");
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInTypeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInDefault()
        {
            VerifyAbsence(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInSizeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(544219)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInObjectInitializerMemberContext()
        {
            VerifyAbsence(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$");
        }
    }
}
