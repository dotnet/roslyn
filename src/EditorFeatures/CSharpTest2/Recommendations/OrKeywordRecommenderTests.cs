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
    public class OrKeywordRecommenderTests : KeywordRecommenderTests
    {
        private const string InitializeObjectE = @"var e = new object();
";

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterConstant()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is 1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMultipleConstants()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or 2 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterType()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is int $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRelationalOperator()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"if (e is >= 0 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGenericType()
        {
            VerifyKeyword(
@"class C<T>
{
    void M()
    {
        var e = new object();
        if (e is T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterArrayType()
        {
            VerifyKeyword(
@"class C
{
    void M()
    {
        var e = new object();
        if (e is int[] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterListType()
        {
            VerifyKeyword(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var e = new object();
        if (e is List<int> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterListType_FullyQualified()
        {
            VerifyKeyword(
@"class C
{
    void M()
    {
        var e = new object();
        if (e is System.Collections.Generic.List<int> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRecursivePattern()
        {
            VerifyKeyword(
@"class C
{
    int P { get; }

    void M(C test)
    {
        if (test is { P: 1 } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern()
        {
            VerifyKeyword(
@"class C
{
    public int P { get; }

    void M(C test)
    {
        if (test is { P: 1 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_ComplexConstant()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        int Prop { get; }

        void M(C test)
        {
            if (test is { Prop: N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterOpenParen()
        {
            VerifyKeyword(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (1 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterOpenParen_ComplexConstant()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        int Prop { get; }

        void M(C test)
        {
            if (test is { Prop: (N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterMultipleOpenParens()
        {
            VerifyKeyword(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (((1 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterMultipleOpenParens_ComplexConstant()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        int Prop { get; }

        void M(C test)
        {
            if (test is { Prop: (((N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens_MemberAccessExpression()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        void M()
        {
            var e = new object();
            switch (e)
            {
                case (((N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens_MemberAccessExpression2()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        void M()
        {
            var e = new object();
            switch (e)
            {
                case (((N.C $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterParenPair()
        {
            VerifyKeyword(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (1) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterParenPair_ComplexConstant()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        int Prop { get; }

        void M(C test)
        {
            if (test is { Prop: (N.C.P + 1) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterMultipleParenPairs()
        {
            VerifyKeyword(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (((1))) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideSubpattern_AfterMultipleParenPairs_ComplexConstant()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        int Prop { get; }

        void M(C test)
        {
            if (test is { Prop: (((N.C.P))) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterQualifiedName()
        {
            VerifyKeyword(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        var e = new object();
        if (e is C2.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterQualifiedName2()
        {
            VerifyKeyword(
@"
namespace N
{
    class C
    {
        const int P = 1;

        void M()
        {
            var e = new object();
            if (e is N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression_Complex()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        void M()
        {
            var e = new object();
            var result = e switch
            {
                N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case 1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression_AfterOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    (1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression_AfterOpenParen_Complex()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        void M()
        {
            var e = new object();
            var result = e switch
            {
                (N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression_AfterMultipleOpenParens()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    (((1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchExpression_AfterMultipleOpenParens_Complex()
        {
            VerifyKeyword(
@"namespace N
{
    class C
    {
        const int P = 1;

        void M()
        {
            var e = new object();
            var result = e switch
            {
                (((N.C.P $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement_AfterOpenParen()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case (1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens()
        {
            VerifyKeyword(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case (((1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterIsKeyword()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterNotKeyword()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is not $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterVarKeyword()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterAndKeyword()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is 1 and $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterOrKeyword()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterOpenParen()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterOpenBracket()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is { $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAtBeginningOfSwitchExpression()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAtBeginningOfSwitchStatement()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterTypeAndOpenParen()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is int ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterTypeAndCloseParen()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is int)$$"));
        }

        [WorkItem(44396, "https://github.com/dotnet/roslyn/issues/44396")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterColonColonPatternSyntax()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"if (e is null or global::$$) { }"));
        }

        [WorkItem(44396, "https://github.com/dotnet/roslyn/issues/44396")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestMissingAfterColonColonPatternSyntax_SwitchExpression()
        {
            VerifyAbsence(AddInsideMethod(InitializeObjectE +
@"var x = false;
x = e switch
{
    global::$$"));
        }
    }
}
