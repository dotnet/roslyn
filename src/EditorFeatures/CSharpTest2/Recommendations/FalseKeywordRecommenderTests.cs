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
    public class FalseKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInPreprocessor1()
        {
            VerifyAbsence(
@"class C {
#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPreprocessor2()
        {
            VerifyAbsence(
@"class C {
#line $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPIf()
        {
            VerifyKeyword(
@"#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPIf_Or()
        {
            VerifyKeyword(
@"#if a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPIf_And()
        {
            VerifyKeyword(
@"#if a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPIf_Not()
        {
            VerifyKeyword(
@"#if ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPIf_Paren()
        {
            VerifyKeyword(
@"#if ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPIf_Equals()
        {
            VerifyKeyword(
@"#if a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPIf_NotEquals()
        {
            VerifyKeyword(
@"#if a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPElIf()
        {
            VerifyKeyword(
@"#if true
#elif $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPelIf_Or()
        {
            VerifyKeyword(
@"#if true
#elif a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPElIf_And()
        {
            VerifyKeyword(
@"#if true
#elif a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPElIf_Not()
        {
            VerifyKeyword(
@"#if true
#elif ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPElIf_Paren()
        {
            VerifyKeyword(
@"#if true
#elif ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPElIf_Equals()
        {
            VerifyKeyword(
@"#if true
#elif a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPPElIf_NotEquals()
        {
            VerifyKeyword(
@"#if true
#elif a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUnaryOperator()
        {
            VerifyKeyword(
@"class C {
   public static bool operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterImplicitOperator()
        {
            VerifyAbsence(
@"class C {
   public static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExplicitOperator()
        {
            VerifyAbsence(
@"class C {
   public static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInNamedParameter()
        {
            VerifyKeyword(AddInsideMethod(
@"return new SingleDeclaration(
                kind: GetKind(node.Kind),
                hasUsings: $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInAttribute()
        {
            VerifyKeyword(
@"[assembly: ComVisible($$");
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInTypeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDefault()
        {
            VerifyAbsence(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInSizeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            VerifyAbsence(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"ref int x = ref $$"));
        }
    }
}
