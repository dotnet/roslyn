// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping;

[Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
public sealed class ArgumentWrappingTests : AbstractWrappingTests
{
    [Fact]
    public Task TestMissingWithSyntaxError()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, j
                }
            }
            """);

    [Fact]
    public Task TestMissingWithSelection()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([|i|], j);
                }
            }
            """);

    [Fact]
    public Task TestMissingBeforeName()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    a.[||]b.Goobar(i, j);
                }
            }
            """);

    [Fact]
    public Task TestMissingWithSingleParameter()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i);
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMultiLineParameter()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, j +
                        k);
                }
            }
            """);

    [Fact]
    public Task TestMissingWithMultiLineParameter2()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, @"
                    ");
                }
            }
            """);

    [Fact]
    public Task TestInHeader1()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Bar() {
                    [||]Goobar(i, j);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    Goobar(i,
                           j);
                }
            }
            """);

    [Fact]
    public Task TestInHeader2()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Bar() {
                    a.[||]Goobar(i, j);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                             j);
                }
            }
            """);

    [Fact]
    public Task TestInHeader4()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Bar() {
                    a.Goobar(i, j[||]);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                             j);
                }
            }
            """);

    [Fact]
    public Task TestTwoParamWrappingCases()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    a.Goobar([||]i, j);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                             j);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i,
                        j);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                        j);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i, j);
                }
            }
            """);

    [Fact]
    public Task TestThreeParamWrappingCases()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    a.Goobar([||]i, j, k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                             j,
                             k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i,
                        j,
                        k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                        j,
                        k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i, j, k);
                }
            }
            """);

    [Fact]
    public Task Test_AllOptions_NoInitialMatches()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    a.Goobar(
                        [||]i,
                            j,
                                k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                             j,
                             k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i,
                        j,
                        k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                        j,
                        k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i, j, k);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i, j, k);
                }
            }
            """);

    [Fact]
    public Task Test_LongWrapping_ShortIds()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo() {
                    this.Goobar([||]
                        i, j, k, l, m, n, o, p,
                        n);
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo() {
                    this.Goobar(i,
                                j,
                                k,
                                l,
                                m,
                                n,
                                o,
                                p,
                                n);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        i,
                        j,
                        k,
                        l,
                        m,
                        n,
                        o,
                        p,
                        n);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i,
                        j,
                        k,
                        l,
                        m,
                        n,
                        o,
                        p,
                        n);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i, j, k, l, m, n, o, p, n);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        i, j, k, l, m, n, o, p, n);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i, j, k, l,
                                m, n, o, p,
                                n);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        i, j, k, l, m, n,
                        o, p, n);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i, j, k, l,
                        m, n, o, p, n);
                }
            }
            """);

    [Fact]
    public Task Test_LongWrapping_VariadicLengthIds()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo() {
                    this.Goobar([||]
                        i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm,
                        nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Goo() {
                    this.Goobar(i,
                                jj,
                                kkkkk,
                                llllllll,
                                mmmmmmmmmmmmmmmmmm,
                                nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        i,
                        jj,
                        kkkkk,
                        llllllll,
                        mmmmmmmmmmmmmmmmmm,
                        nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i,
                        jj,
                        kkkkk,
                        llllllll,
                        mmmmmmmmmmmmmmmmmm,
                        nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i, jj,
                                kkkkk,
                                llllllll,
                                mmmmmmmmmmmmmmmmmm,
                                nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        i, jj, kkkkk,
                        llllllll,
                        mmmmmmmmmmmmmmmmmm,
                        nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(i, jj,
                        kkkkk, llllllll,
                        mmmmmmmmmmmmmmmmmm,
                        nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Goo() {
                    this.Goobar([||]
                        iiiii, jjjjj, kkkkk, lllll, mmmmm,
                        nnnnn);
                }
            }
            """,
            GetIndentionColumn(25),
            """
            class C {
                void Goo() {
                    this.Goobar(iiiii,
                                jjjjj,
                                kkkkk,
                                lllll,
                                mmmmm,
                                nnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        iiiii,
                        jjjjj,
                        kkkkk,
                        lllll,
                        mmmmm,
                        nnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(iiiii,
                        jjjjj,
                        kkkkk,
                        lllll,
                        mmmmm,
                        nnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(
                        iiiii, jjjjj,
                        kkkkk, lllll,
                        mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    this.Goobar(iiiii,
                        jjjjj, kkkkk,
                        lllll, mmmmm,
                        nnnnn);
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    a.[||]Goobar(
                        iiiii, jjjjj, kkkkk, lllll, mmmmm,
                        nnnnn);
                }
            }
            """,
            GetIndentionColumn(20),
            """
            class C {
                void Bar() {
                    a.Goobar(iiiii,
                             jjjjj,
                             kkkkk,
                             lllll,
                             mmmmm,
                             nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        iiiii,
                        jjjjj,
                        kkkkk,
                        lllll,
                        mmmmm,
                        nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(iiiii,
                        jjjjj,
                        kkkkk,
                        lllll,
                        mmmmm,
                        nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
                }
            }
            """);

    [Fact]
    public Task Test_LongWrapping_VariadicLengthIds2()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    a.[||]Goobar(
                        i, jj, kkkk, lll, mm,
                        n) {
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                             jj,
                             kkkk,
                             lll,
                             mm,
                             n) {
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i,
                        jj,
                        kkkk,
                        lll,
                        mm,
                        n) {
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                        jj,
                        kkkk,
                        lll,
                        mm,
                        n) {
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i, jj, kkkk, lll, mm, n) {
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i, jj, kkkk, lll, mm, n) {
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i, jj, kkkk,
                             lll, mm, n) {
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i, jj, kkkk, lll,
                        mm, n) {
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i, jj, kkkk,
                        lll, mm, n) {
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferExistingOption1()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    a.[||]Goobar(iiiii,
                             jjjjj,
                             kkkkk,
                             lllll,
                             mmmmm,
                             nnnnn);
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Bar() {
                    a.Goobar(
                        iiiii,
                        jjjjj,
                        kkkkk,
                        lllll,
                        mmmmm,
                        nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(iiiii,
                        jjjjj,
                        kkkkk,
                        lllll,
                        mmmmm,
                        nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(iiiii, jjjjj,
                             kkkkk, lllll,
                             mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        iiiii, jjjjj,
                        kkkkk, lllll,
                        mmmmm, nnnnn);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(iiiii, jjjjj,
                        kkkkk, lllll,
                        mmmmm, nnnnn);
                }
            }
            """);

    [Fact]
    public Task Test_DoNotOfferExistingOption2()
        => TestAllWrappingCasesAsync(
            """
            class C {
                void Bar() {
                    a.Goobar([||]
                        i,
                        jj,
                        kkkk,
                        lll,
                        mm,
                        n);
                }
            }
            """,
            GetIndentionColumn(30),
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                             jj,
                             kkkk,
                             lll,
                             mm,
                             n);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i,
                        jj,
                        kkkk,
                        lll,
                        mm,
                        n);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i, jj, kkkk, lll, mm, n);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i, jj, kkkk, lll, mm, n);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i, jj, kkkk,
                             lll, mm, n);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(
                        i, jj, kkkk, lll,
                        mm, n);
                }
            }
            """,
            """
            class C {
                void Bar() {
                    a.Goobar(i, jj, kkkk,
                        lll, mm, n);
                }
            }
            """);

    [Fact]
    public Task TestInElementAccess1()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo() {
                    var v = this[[||]a, b, c];
                }
            }
            """,
            """
            class C {
                void Goo() {
                    var v = this[a,
                                 b,
                                 c];
                }
            }
            """);

    [Fact]
    public Task TestInElementAccess2()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo() {
                    var v = [||]this[a, b, c];
                }
            }
            """,
            """
            class C {
                void Goo() {
                    var v = this[a,
                                 b,
                                 c];
                }
            }
            """);

    [Fact]
    public Task TestInObjectCreation1()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo() {
                    var v = [||]new Bar(a, b, c);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    var v = new Bar(a,
                                    b,
                                    c);
                }
            }
            """);

    [Fact]
    public Task TestInObjectCreation2()
        => TestInRegularAndScriptAsync(
            """
            class C {
                void Goo() {
                    var v = new Bar([||]a, b, c);
                }
            }
            """,
            """
            class C {
                void Goo() {
                    var v = new Bar(a,
                                    b,
                                    c);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50104")]
    public Task TestInImplicitObjectCreation()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Program p1 = new([||]1, 2);
                }

                public Program(object o1, object o2) { }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Program p1 = new(1,
                                     2);
                }

                public Program(object o1, object o2) { }
            }
            """);

    [Fact]
    public Task TestInConstructorInitializer1()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public C() : base([||]a, b, c) {
                }
            }
            """,
            """
            class C {
                public C() : base(a,
                                  b,
                                  c) {
                }
            }
            """);

    [Fact]
    public Task TestInConstructorInitializer2()
        => TestInRegularAndScriptAsync(
            """
            class C {
                public C() : [||]base(a, b, c) {
                }
            }
            """,
            """
            class C {
                public C() : base(a,
                                  b,
                                  c) {
                }
            }
            """);

    [Fact]
    public Task TestInPrimaryConstructorInitializer1()
        => TestAllWrappingCasesAsync(
            """
            class C() : B([||]a, b, c) {
            }
            """,
            """
            class C() : B(a,
                          b,
                          c) {
            }
            """,
            """
            class C() : B(
                a,
                b,
                c) {
            }
            """,
            """
            class C() : B(a,
                b,
                c) {
            }
            """,
            """
            class C() : B(
                a, b, c) {
            }
            """);

    [Fact]
    public Task TestInPrimaryConstructorInitializer2()
        => TestAllWrappingCasesAsync(
            """
            class C() : [||]B(a, b, c) {
            }
            """,
            """
            class C() : B(a,
                          b,
                          c) {
            }
            """,
            """
            class C() : B(
                a,
                b,
                c) {
            }
            """,
            """
            class C() : B(a,
                b,
                c) {
            }
            """,
            """
            class C() : B(
                a, b, c) {
            }
            """);

    [Fact]
    public Task TestInPrimaryConstructorInitializer3()
        => TestAllWrappingCasesAsync(
            """
            class C() 
                : [||]B(a, b, c) {
            }
            """,
            """
            class C() 
                : B(a,
                    b,
                    c) {
            }
            """,
            """
            class C() 
                : B(
                    a,
                    b,
                    c) {
            }
            """,
            """
            class C() 
                : B(
                    a, b, c) {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestMissingStartToken1()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar [||])
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestMissingStartToken2()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar [||]i, j)
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestMissingEndToken1()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public Task TestMissingEndToken2()
        => TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, j
                }
            }
            """);
}
