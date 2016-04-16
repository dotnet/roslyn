// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NullKeywordRecommenderTests : KeywordRecommenderTests
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
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf()
        {
            await VerifyAbsenceAsync(
@"#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Or()
        {
            await VerifyAbsenceAsync(
@"#if a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_And()
        {
            await VerifyAbsenceAsync(
@"#if a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Not()
        {
            await VerifyAbsenceAsync(
@"#if ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Paren()
        {
            await VerifyAbsenceAsync(
@"#if ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_Equals()
        {
            await VerifyAbsenceAsync(
@"#if a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPIf_NotEquals()
        {
            await VerifyAbsenceAsync(
@"#if a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf()
        {
            await VerifyAbsenceAsync(
@"#if true
#elif $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPelIf_Or()
        {
            await VerifyAbsenceAsync(
@"#if true
#elif a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_And()
        {
            await VerifyAbsenceAsync(
@"#if true
#elif a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_Not()
        {
            await VerifyAbsenceAsync(
@"#if true
#elif ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_Paren()
        {
            await VerifyAbsenceAsync(
@"#if true
#elif ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_Equals()
        {
            await VerifyAbsenceAsync(
@"#if true
#elif a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPPElIf_NotEquals()
        {
            await VerifyAbsenceAsync(
@"#if true
#elif a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUnaryOperator()
        {
            await VerifyAbsenceAsync(
@"class C {
   public static bool operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterImplicitOperator()
        {
            await VerifyAbsenceAsync(
@"class C {
   public static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExplicitOperator()
        {
            await VerifyAbsenceAsync(
@"class C {
   public static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCastInField()
        {
            await VerifyKeywordAsync(
@"class C {
   public static readonly ImmutableList<T> Empty = new ImmutableList<T>((Segment)$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInTernary()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"SyntaxKind kind = caseOrDefaultKeywordOpt == $$ ? SyntaxKind.GotoStatement :
                caseOrDefaultKeyword.Kind == SyntaxKind.CaseKeyword ? SyntaxKind.GotoCaseStatement : SyntaxKind.GotoDefaultStatement;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInForMiddle()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for (int i = 0; $$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDefault()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(541670, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541670")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReferenceSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch (""foo"")
        {
            case $$
        }"));
        }

#if false
        [WorkItem(8486)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInValueSwitch()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"switch (0)
        {
            case $$
        }"));
        }
#endif

        [WorkItem(543766, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543766")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDefaultParameterValue()
        {
            await VerifyKeywordAsync(
@"class C { void Foo(string[] args = $$");
        }

        [WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync(@"
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
            await VerifyAbsenceAsync(@"
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
            await VerifyAbsenceAsync(@"
/// <see cref=""System.$$"" />
class C { }
");
        }
    }
}
