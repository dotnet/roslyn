// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class OnKeywordRecommenderTests : KeywordRecommenderTests
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
        public void AfterJoinInExpr1()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in e $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterJoinInExpr2()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a.b c in e $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOn1()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOn2()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOn3()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOn4()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 e$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOn5()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 equals $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterOn6()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in e on o1 equals o$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterIn()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          join a.b c in $$"));
        }
    }
}
