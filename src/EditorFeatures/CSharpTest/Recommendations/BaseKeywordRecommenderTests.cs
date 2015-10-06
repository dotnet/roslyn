// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class BaseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInTopLevelMethod()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"void Foo()
{
    $$
}");
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
        public void InClassConstructorInitializer()
        {
            VerifyKeyword(
@"class C {
    public C() : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStaticClassConstructorInitializer()
        {
            VerifyAbsence(
@"class C {
    static C() : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInStructConstructorInitializer()
        {
            VerifyAbsence(
@"struct C {
    public C() : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCast()
        {
            VerifyKeyword(
@"struct C {
    new internal ErrorCode Code { get { return (ErrorCode)$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InEmptyMethod()
        {
            VerifyKeyword(
                SourceCodeKind.Regular,
                AddInsideMethod(
@"$$"));
        }

        [WorkItem(538264)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEnumMemberInitializer1()
        {
            VerifyAbsence(
@"enum E {
    a = $$
}");
        }

        [WorkItem(544219)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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
