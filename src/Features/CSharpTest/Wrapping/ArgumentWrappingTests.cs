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
public class ArgumentWrappingTests : AbstractWrappingTests
{
    [Fact]
    public async Task TestMissingWithSyntaxError()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, j
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithSelection()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([|i|], j);
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingBeforeName()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    a.[||]b.Goobar(i, j);
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithSingleParameter()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i);
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMultiLineParameter()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, j +
                        k);
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingWithMultiLineParameter2()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, @"
                    ");
                }
            }
            """);
    }

    [Fact]
    public async Task TestInHeader1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInHeader2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInHeader4()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestTwoParamWrappingCases()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task TestThreeParamWrappingCases()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_AllOptions_NoInitialMatches()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_LongWrapping_ShortIds()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_LongWrapping_VariadicLengthIds()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_LongWrapping_VariadicLengthIds2()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_DoNotOfferExistingOption1()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task Test_DoNotOfferExistingOption2()
    {
        await TestAllWrappingCasesAsync(
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
    }

    [Fact]
    public async Task TestInElementAccess1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInElementAccess2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInObjectCreation1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInObjectCreation2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50104")]
    public async Task TestInImplicitObjectCreation()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInConstructorInitializer1()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestInConstructorInitializer2()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public async Task TestMissingStartToken1()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar [||])
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public async Task TestMissingStartToken2()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar [||]i, j)
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public async Task TestMissingEndToken1()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")]
    public async Task TestMissingEndToken2()
    {
        await TestMissingAsync(
            """
            class C {
                void Bar() {
                    Goobar([||]i, j
                }
            }
            """);
    }
}
