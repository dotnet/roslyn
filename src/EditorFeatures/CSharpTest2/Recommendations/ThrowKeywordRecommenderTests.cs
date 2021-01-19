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
    public class ThrowKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
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
        public async Task TestEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$
return true;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"return true;
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBlock()
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) {
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIf()
        {
            VerifyKeyword(AddInsideMethod(
@"if (true) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDo()
        {
            VerifyKeyword(AddInsideMethod(
@"do 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterWhile()
        {
            VerifyKeyword(AddInsideMethod(
@"while (true) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFor()
        {
            VerifyKeyword(AddInsideMethod(
@"for (int i = 0; i < 10; i++) 
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterForeach()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v in bar)
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterThrow()
        {
            VerifyAbsence(AddInsideMethod(
@"throw $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInClass()
        {
            VerifyAbsence(@"class C
{
  $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInNestedIf()
        {
            VerifyKeyword(AddInsideMethod(
@"if (caseOrDefaultKeywordOpt != null) {
    if (caseOrDefaultKeyword.Kind != SyntaxKind.CaseKeyword && caseOrDefaultKeyword.Kind != SyntaxKind.DefaultKeyword) 
      $$"));
        }

        [WorkItem(9099, "https://github.com/dotnet/roslyn/issues/9099")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterArrow()
        {
            VerifyKeyword(
@"class C
{
    void Goo() => $$
");
        }

        [WorkItem(9099, "https://github.com/dotnet/roslyn/issues/9099")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterQuestionQuestion()
        {
            VerifyKeyword(
@"class C
{
    public C(object o)
    {
        _o = o ?? $$
");
        }

        [WorkItem(9099, "https://github.com/dotnet/roslyn/issues/9099")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConditional1()
        {
            VerifyKeyword(
@"class C
{
    public C(object o)
    {
        var v= true ? $$
");
        }

        [WorkItem(9099, "https://github.com/dotnet/roslyn/issues/9099")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConditional2()
        {
            VerifyKeyword(
@"class C
{
    public C(object o)
    {
        var v= true ? 0 : $$
");
        }
    }
}
