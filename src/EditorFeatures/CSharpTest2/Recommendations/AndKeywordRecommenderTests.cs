// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class AndKeywordRecommenderTests : KeywordRecommenderTests
    {
        private const string InitializeObjectE = @"var e = new object();
";

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstant()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMultipleConstants()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or 2 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterType()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRelationalOperator()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is >= 0 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericType()
        {
            await VerifyKeywordAsync(
@"class C<T>
{
    void M()
    {
        var e = new object();
        if (e is T $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterArrayType()
        {
            await VerifyKeywordAsync(
@"class C
{
    void M()
    {
        var e = new object();
        if (e is int[] $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterListType()
        {
            await VerifyKeywordAsync(
@"using System.Collections.Generic;

class C
{
    void M()
    {
        var e = new object();
        if (e is List<int> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterListType_FullyQualified()
        {
            await VerifyKeywordAsync(
@"class C
{
    void M()
    {
        var e = new object();
        if (e is System.Collections.Generic.List<int> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRecursivePattern()
        {
            await VerifyKeywordAsync(
@"class C
{
    int P { get; }

    void M(C test)
    {
        if (test is { P: 1 } $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSubpattern()
        {
            await VerifyKeywordAsync(
@"class C
{
    public int P { get; }

    void M(C test)
    {
        if (test is { P: 1 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSubpattern_ComplexConstant()
        {
            await VerifyKeywordAsync(
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
        public async Task TestInsideSubpattern_AfterOpenParen()
        {
            await VerifyKeywordAsync(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (1 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSubpattern_AfterOpenParen_ComplexConstant()
        {
            await VerifyKeywordAsync(
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
        public async Task TestInsideSubpattern_AfterMultipleOpenParens()
        {
            await VerifyKeywordAsync(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (((1 $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSubpattern_AfterMultipleOpenParens_ComplexConstant()
        {
            await VerifyKeywordAsync(
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
        public async Task TestInsideSubpattern_AfterParenPair()
        {
            await VerifyKeywordAsync(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (1) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSubpattern_AfterParenPair_ComplexConstant()
        {
            await VerifyKeywordAsync(
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
        public async Task TestInsideSubpattern_AfterMultipleParenPairs()
        {
            await VerifyKeywordAsync(
@"class C
{
    int P { get; }

    void M()
    {
        var C2 = new C();
        if (C2 is { P: (((1))) $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideSubpattern_AfterMultipleParenPairs_ComplexConstant()
        {
            await VerifyKeywordAsync(
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
        public async Task TestAfterQualifiedName()
        {
            await VerifyKeywordAsync(
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
        public async Task TestAfterQualifiedName2()
        {
            await VerifyKeywordAsync(
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
        public async Task TestAtBeginningOfSwitchExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtBeginningOfSwitchExpression_Complex()
        {
            await VerifyKeywordAsync(
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
        public async Task TestAtBeginningOfSwitchStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case 1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtBeginningOfSwitchExpression_AfterOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    (1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtBeginningOfSwitchExpression_AfterOpenParen_Complex()
        {
            await VerifyKeywordAsync(
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
        public async Task TestAtBeginningOfSwitchExpression_AfterMultipleOpenParens()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    (((1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtBeginningOfSwitchExpression_AfterMultipleOpenParens_Complex()
        {
            await VerifyKeywordAsync(
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
        public async Task TestAtBeginningOfSwitchStatement_AfterOpenParen()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case (1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens()
        {
            await VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case (((1 $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens_MemberAccessExpression()
        {
            await VerifyKeywordAsync(
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
        public async Task TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens_MemberAccessExpression2()
        {
            await VerifyKeywordAsync(
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
        public async Task TestMissingAfterIsKeyword()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterNotKeyword()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is not $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterVarKeyword()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterAndKeyword()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 and $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterOrKeyword()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterOpenParen()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterOpenBracket()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is { $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAtBeginningOfSwitchExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"var result = e switch
{
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAtBeginningOfSwitchStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"switch (e)
{
    case $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterTypeAndOpenParen()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterTypeAndCloseParen()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int)$$"));
        }

        [WorkItem(44396, "https://github.com/dotnet/roslyn/issues/44396")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterColonColonPatternSyntax()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is null or global::$$) { }"));
        }

        [WorkItem(44396, "https://github.com/dotnet/roslyn/issues/44396")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMissingAfterColonColonPatternSyntax_SwitchExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"var x = false;
x = e switch
{
    global::$$"));
        }
    }
}
