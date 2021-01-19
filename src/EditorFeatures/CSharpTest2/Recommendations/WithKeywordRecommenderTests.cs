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
        public async Task TestNotAfterWith()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = goo with $$"));
        }

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
        public async Task TestAfterExpr()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = goo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDottedName()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = goo.Current $$"));
        }

        [WorkItem(543041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543041")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVarInForLoop()
        {
            VerifyAbsence(AddInsideMethod(
@"for (var $$"));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeFirstStringHole()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = ""\{0}$$\{1}\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBetweenStringHoles()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = ""\{0}\{1}$$\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStringHoles()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}$$"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterLastStringHole()
        {
            VerifyKeyword(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}"" $$"));
        }

        [WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotWithinNumericLiteral()
        {
            VerifyAbsence(AddInsideMethod(
@"var x = .$$0;"));
        }

        [WorkItem(28586, "https://github.com/dotnet/roslyn/issues/28586")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAsync()
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
        public async Task TestNotAfterMethodReference()
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
        public async Task TestNotAfterAnonymousMethod()
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
        public async Task TestNotAfterLambda1()
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
        public async Task TestNotAfterLambda2()
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
        public async Task TestMissingAfterNumericLiteral()
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
        public async Task TestMissingAfterNumericLiteralAndDot()
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
        public async Task TestMissingAfterNumericLiteralDotAndSpace()
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
