// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class SwitchKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
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
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSwitchBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (E) {
  case 0:
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSwitch1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"switch $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExpression()
            => await VerifyKeywordAsync(AddInsideMethod(@"_ = expr $$"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExpression_InMethodWithArrowBody()
        {
            await VerifyKeywordAsync(@"
class C
{
    bool M() => this $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterForeachVar()
            => await VerifyAbsenceAsync(AddInsideMethod(@"foreach (var $$)"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTuple()
            => await VerifyKeywordAsync(AddInsideMethod(@"_ = (expr, expr) $$"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSwitch2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"switch ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            await VerifyAbsenceAsync(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (expr) {
   default:
}
$$"));
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
