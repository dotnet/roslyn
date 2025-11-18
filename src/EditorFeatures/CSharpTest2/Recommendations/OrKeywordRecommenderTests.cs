// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class OrKeywordRecommenderTests : KeywordRecommenderTests
{
    private const string InitializeObjectE = """
        var e = new object();
        """;

    [Fact]
    public Task TestAfterConstant()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 $$"));

    [Fact]
    public Task TestAfterMultipleConstants()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or 2 $$"));

    [Fact]
    public Task TestAfterType()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int $$"));

    [Fact]
    public Task TestAfterRelationalOperator()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
@"if (e is >= 0 $$"));

    [Fact]
    public Task TestAfterGenericType()
        => VerifyKeywordAsync(
            """
            class C<T>
            {
                void M()
                {
                    var e = new object();
                    if (e is T $$
            """);

    [Fact]
    public Task TestAfterArrayType()
        => VerifyKeywordAsync(
            """
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is int[] $$
            """);

    [Fact]
    public Task TestAfterListType()
        => VerifyKeywordAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is List<int> $$
            """);

    [Fact]
    public Task TestAfterListType_FullyQualified()
        => VerifyKeywordAsync(
            """
            class C
            {
                void M()
                {
                    var e = new object();
                    if (e is System.Collections.Generic.List<int> $$
            """);

    [Fact]
    public Task TestAfterRecursivePattern()
        => VerifyKeywordAsync(
            """
            class C
            {
                int P { get; }

                void M(C test)
                {
                    if (test is { P: 1 } $$
            """);

    [Fact]
    public Task TestInsideSubpattern()
        => VerifyKeywordAsync(
            """
            class C
            {
                public int P { get; }

                void M(C test)
                {
                    if (test is { P: 1 $$
            """);

    [Fact]
    public Task TestInsideSubpattern_ComplexConstant()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    int Prop { get; }

                    void M(C test)
                    {
                        if (test is { Prop: N.C.P $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterOpenParen()
        => VerifyKeywordAsync(
            """
            class C
            {
                int P { get; }

                void M()
                {
                    var C2 = new C();
                    if (C2 is { P: (1 $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterOpenParen_ComplexConstant()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    int Prop { get; }

                    void M(C test)
                    {
                        if (test is { Prop: (N.C.P $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterMultipleOpenParens()
        => VerifyKeywordAsync(
            """
            class C
            {
                int P { get; }

                void M()
                {
                    var C2 = new C();
                    if (C2 is { P: (((1 $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterMultipleOpenParens_ComplexConstant()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    int Prop { get; }

                    void M(C test)
                    {
                        if (test is { Prop: (((N.C.P $$
            """);

    [Fact]
    public Task TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens_MemberAccessExpression()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    void M()
                    {
                        var e = new object();
                        switch (e)
                        {
                            case (((N.C.P $$
            """);

    [Fact]
    public Task TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens_MemberAccessExpression2()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    void M()
                    {
                        var e = new object();
                        switch (e)
                        {
                            case (((N.C $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterParenPair()
        => VerifyKeywordAsync(
            """
            class C
            {
                int P { get; }

                void M()
                {
                    var C2 = new C();
                    if (C2 is { P: (1) $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterParenPair_ComplexConstant()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    int Prop { get; }

                    void M(C test)
                    {
                        if (test is { Prop: (N.C.P + 1) $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterMultipleParenPairs()
        => VerifyKeywordAsync(
            """
            class C
            {
                int P { get; }

                void M()
                {
                    var C2 = new C();
                    if (C2 is { P: (((1))) $$
            """);

    [Fact]
    public Task TestInsideSubpattern_AfterMultipleParenPairs_ComplexConstant()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    int Prop { get; }

                    void M(C test)
                    {
                        if (test is { Prop: (((N.C.P))) $$
            """);

    [Fact]
    public Task TestAfterQualifiedName()
        => VerifyKeywordAsync(
            """
            class C
            {
                int P { get; }

                void M()
                {
                    var C2 = new C();
                    var e = new object();
                    if (e is C2.P $$
            """);

    [Fact]
    public Task TestAfterQualifiedName2()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    void M()
                    {
                        var e = new object();
                        if (e is N.C.P $$
            """);

    [Fact]
    public Task TestAtBeginningOfSwitchExpression()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                1 $$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchExpression_Complex()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    void M()
                    {
                        var e = new object();
                        var result = e switch
                        {
                            N.C.P $$
            """);

    [Fact]
    public Task TestAtBeginningOfSwitchStatement()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case 1 $$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchExpression_AfterOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                (1 $$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchExpression_AfterOpenParen_Complex()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    void M()
                    {
                        var e = new object();
                        var result = e switch
                        {
                            (N.C.P $$
            """);

    [Fact]
    public Task TestAtBeginningOfSwitchExpression_AfterMultipleOpenParens()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                (((1 $$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchExpression_AfterMultipleOpenParens_Complex()
        => VerifyKeywordAsync(
            """
            namespace N
            {
                class C
                {
                    const int P = 1;

                    void M()
                    {
                        var e = new object();
                        var result = e switch
                        {
                            (((N.C.P $$
            """);

    [Fact]
    public Task TestAtBeginningOfSwitchStatement_AfterOpenParen()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case (1 $$
            """));

    [Fact]
    public Task TestAtBeginningOfSwitchStatement_AfterMultipleOpenParens()
        => VerifyKeywordAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case (((1 $$
            """));

    [Fact]
    public Task TestMissingAfterIsKeyword()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is $$"));

    [Fact]
    public Task TestMissingAfterNotKeyword()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is not $$"));

    [Fact]
    public Task TestMissingAfterVarKeyword()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is var $$"));

    [Fact]
    public Task TestMissingAfterAndKeyword()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 and $$"));

    [Fact]
    public Task TestMissingAfterOrKeyword()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is 1 or $$"));

    [Fact]
    public Task TestMissingAfterOpenParen()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is ($$"));

    [Fact]
    public Task TestMissingAfterOpenBracket()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is { $$"));

    [Fact]
    public Task TestMissingAtBeginningOfSwitchExpression()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
            """
            var result = e switch
            {
                $$
            """));

    [Fact]
    public Task TestMissingAtBeginningOfSwitchStatement()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
            """
            switch (e)
            {
                case $$
            """));

    [Fact]
    public Task TestMissingAfterTypeAndOpenParen()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int ($$"));

    [Fact]
    public Task TestMissingAfterTypeAndCloseParen()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is int)$$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44396")]
    public Task TestMissingAfterColonColonPatternSyntax()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
@"if (e is null or global::$$) { }"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44396")]
    public Task TestMissingAfterColonColonPatternSyntax_SwitchExpression()
        => VerifyAbsenceAsync(AddInsideMethod(InitializeObjectE +
            """
            var x = false;
            x = e switch
            {
                global::$$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70045")]
    public Task TestNotInMemberAccessInPattern1()
        => VerifyAbsenceAsync("""
            int v = 0;
            if (v is var a and a.$$)
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70045")]
    public Task TestNotInMemberAccessInPattern2()
        => VerifyAbsenceAsync("""
            int* v = null;
            if (v is var a and a->$$)
            """);
}
