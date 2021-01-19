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
    public class NullKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInPPIf()
        {
            VerifyAbsence(
@"#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Or()
        {
            VerifyAbsence(
@"#if a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_And()
        {
            VerifyAbsence(
@"#if a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Not()
        {
            VerifyAbsence(
@"#if ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Paren()
        {
            VerifyAbsence(
@"#if ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Equals()
        {
            VerifyAbsence(
@"#if a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_NotEquals()
        {
            VerifyAbsence(
@"#if a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf()
        {
            VerifyAbsence(
@"#if true
#elif $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPelIf_Or()
        {
            VerifyAbsence(
@"#if true
#elif a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_And()
        {
            VerifyAbsence(
@"#if true
#elif a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_Not()
        {
            VerifyAbsence(
@"#if true
#elif ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_Paren()
        {
            VerifyAbsence(
@"#if true
#elif ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_Equals()
        {
            VerifyAbsence(
@"#if true
#elif a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_NotEquals()
        {
            VerifyAbsence(
@"#if true
#elif a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUnaryOperator()
        {
            VerifyAbsence(
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
        public async Task TestAfterCastInField()
        {
            VerifyKeyword(
@"class C {
   public static readonly ImmutableList<T> Empty = new ImmutableList<T>((Segment)$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInTernary()
        {
            VerifyKeyword(AddInsideMethod(
@"SyntaxKind kind = caseOrDefaultKeywordOpt == $$ ? SyntaxKind.GotoStatement :
                caseOrDefaultKeyword.Kind == SyntaxKind.CaseKeyword ? SyntaxKind.GotoCaseStatement : SyntaxKind.GotoDefaultStatement;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInForMiddle()
        {
            VerifyKeyword(AddInsideMethod(
@"for (int i = 0; $$"));
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

        [WorkItem(541670, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541670")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReferenceSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (""goo"")
        {
            case $$
        }"));
        }

        [WorkItem(543766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543766")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDefaultParameterValue()
        {
            VerifyKeyword(
@"class C { void Goo(string[] args = $$");
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

        [WorkItem(546938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCrefContext()
        {
            VerifyAbsence(@"
class Program
{
    /// <see cref=""$$"">
    static void Main(string[] args)
    {
        
    }
}");
        }

        [WorkItem(546955, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCrefContextNotAfterDot()
        {
            VerifyAbsence(@"
/// <see cref=""System.$$"" />
class C { }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIs()
            => VerifyKeyword(AddInsideMethod(@"if (x is $$"));

        [WorkItem(25293, "https://github.com/dotnet/roslyn/issues/25293")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIs_BeforeExpression()
        {
            VerifyKeyword(AddInsideMethod(@"
int x;
int y = x is $$ Method();
"));
        }
    }
}
