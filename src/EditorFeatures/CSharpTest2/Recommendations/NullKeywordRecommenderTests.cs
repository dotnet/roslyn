// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NullKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
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
        public void InEmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPIf()
        {
            VerifyAbsence(
@"#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPIf_Or()
        {
            VerifyAbsence(
@"#if a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPIf_And()
        {
            VerifyAbsence(
@"#if a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPIf_Not()
        {
            VerifyAbsence(
@"#if ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPIf_Paren()
        {
            VerifyAbsence(
@"#if ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPIf_Equals()
        {
            VerifyAbsence(
@"#if a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPIf_NotEquals()
        {
            VerifyAbsence(
@"#if a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPElIf()
        {
            VerifyAbsence(
@"#if true
#elif $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPelIf_Or()
        {
            VerifyAbsence(
@"#if true
#elif a || $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPElIf_And()
        {
            VerifyAbsence(
@"#if true
#elif a && $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPElIf_Not()
        {
            VerifyAbsence(
@"#if true
#elif ! $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPElIf_Paren()
        {
            VerifyAbsence(
@"#if true
#elif ( $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPElIf_Equals()
        {
            VerifyAbsence(
@"#if true
#elif a == $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPPElIf_NotEquals()
        {
            VerifyAbsence(
@"#if true
#elif a != $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUnaryOperator()
        {
            VerifyAbsence(
@"class C {
   public static bool operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterImplicitOperator()
        {
            VerifyAbsence(
@"class C {
   public static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterExplicitOperator()
        {
            VerifyAbsence(
@"class C {
   public static implicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCastInField()
        {
            VerifyKeyword(
@"class C {
   public static readonly ImmutableList<T> Empty = new ImmutableList<T>((Segment)$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InTernary()
        {
            VerifyKeyword(AddInsideMethod(
@"SyntaxKind kind = caseOrDefaultKeywordOpt == $$ ? SyntaxKind.GotoStatement :
                caseOrDefaultKeyword.Kind == SyntaxKind.CaseKeyword ? SyntaxKind.GotoCaseStatement : SyntaxKind.GotoDefaultStatement;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForMiddle()
        {
            VerifyKeyword(AddInsideMethod(
@"for (int i = 0; $$"));
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInTypeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInDefault()
        {
            VerifyAbsence(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInSizeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(541670)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InReferenceSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch (""foo"")
        {
            case $$
        }"));
        }

#if false
        [WorkItem(8486)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInValueSwitch()
        {
            VerifyAbsence(AddInsideMethod(
@"switch (0)
        {
            case $$
        }"));
        }
#endif

        [WorkItem(543766)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInDefaultParameterValue()
        {
            VerifyKeyword(
@"class C { void Foo(string[] args = $$");
        }

        [WorkItem(544219)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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

        [WorkItem(546938)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCrefContext()
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

        [WorkItem(546955)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InCrefContextNotAfterDot()
        {
            VerifyAbsence(@"
/// <see cref=""System.$$"" />
class C { }
");
        }
    }
}
