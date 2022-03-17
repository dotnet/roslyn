// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class AsKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
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
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = goo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDottedName()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = goo.Current $$"));
        }

        [WorkItem(543041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543041")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVarInForLoop()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeFirstStringHole()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = ""\{0}$$\{1}\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBetweenStringHoles()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = ""\{0}\{1}$$\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStringHoles()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}$$"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterLastStringHole()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}"" $$"));
        }

        [WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotWithinNumericLiteral()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = .$$0;"));
        }

        [WorkItem(28586, "https://github.com/dotnet/roslyn/issues/28586")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAsync()
        {
            await VerifyAbsenceAsync(
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
            await VerifyAbsenceAsync(
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
            await VerifyAbsenceAsync(
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
            await VerifyAbsenceAsync(
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
            await VerifyAbsenceAsync(
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
            await VerifyAbsenceAsync(
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
            await VerifyAbsenceAsync(
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
            await VerifyAbsenceAsync(
@"
class C
{
    void M()
    {
        var x = 1. $$
    }
}");
        }

        [WorkItem(31367, "https://github.com/dotnet/roslyn/issues/31367")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingInCaseClause1()
        {
            await VerifyAbsenceAsync(
@"
class A
{

}

class C
{
    void M(object o)
    {
        switch (o)
        {
            case A $$
        }
    }
}
");
        }

        [WorkItem(31367, "https://github.com/dotnet/roslyn/issues/31367")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingInCaseClause2()
        {
            await VerifyAbsenceAsync(
@"
namespace N
{
    class A
    {

    }
}

class C
{
    void M(object o)
    {
        switch (o)
        {
            case N.A $$
        }
    }
}
");
        }
    }
}
