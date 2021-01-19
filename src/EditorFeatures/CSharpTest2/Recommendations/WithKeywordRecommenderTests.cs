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
    public class WithKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWith()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = goo with $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterExpr()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = goo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDottedName()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = goo.Current $$"));
        }

        [WorkItem(543041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543041")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterVarInForLoop()
        {
            VerifyAbsence(AddInsideMethod(
@"for (var $$"));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeFirstStringHole()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = ""\{0}$$\{1}\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBetweenStringHoles()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = ""\{0}\{1}$$\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterStringHoles()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}$$"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterLastStringHole()
        {
            VerifyKeyword(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}"" $$"));
        }

        [WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotWithinNumericLiteral()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = .$$0;"));
        }

        [WorkItem(28586, "https://github.com/dotnet/roslyn/issues/28586")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfter()
        {
            VerifyAbsence(
@"
using System;

class C
{
    void Goo()
    {
        Bar(async $$
    }

    void Bar(Func<int, string> f)
    {
    }
}");
        }

        [WorkItem(8319, "https://github.com/dotnet/roslyn/issues/8319")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterMethodReference()
        {
            VerifyAbsence(
@"
using System;

class C {
    void M() {
        var v = Console.WriteLine $$");
        }

        [WorkItem(8319, "https://github.com/dotnet/roslyn/issues/8319")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterAnonymousMethod()
        {
            VerifyAbsence(
@"
using System;

class C {
    void M() {
        Action a = delegate { } $$");
        }

        [WorkItem(8319, "https://github.com/dotnet/roslyn/issues/8319")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterLambda1()
        {
            VerifyAbsence(
@"
using System;

class C {
    void M() {
        Action b = (() => 0) $$");
        }

        [WorkItem(8319, "https://github.com/dotnet/roslyn/issues/8319")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterLambda2()
        {
            VerifyAbsence(
@"
using System;

class C {
    void M() {
        Action b = () => {} $$");
        }

        [WorkItem(48573, "https://github.com/dotnet/roslyn/issues/48573")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterNumericLiteral()
        {
            VerifyAbsence(
@"
class C
{
    void M()
    {
        var x = 1$$
    }
}");
        }

        [WorkItem(48573, "https://github.com/dotnet/roslyn/issues/48573")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterNumericLiteralAndDot()
        {
            VerifyAbsence(
@"
class C
{
    void M()
    {
        var x = 1.$$
    }
}");
        }

        [WorkItem(48573, "https://github.com/dotnet/roslyn/issues/48573")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterNumericLiteralDotAndSpace()
        {
            VerifyAbsence(
@"
class C
{
    void M()
    {
        var x = 1. $$
    }
}");
        }
    }
}
