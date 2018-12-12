// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class BinaryWrappingTests : AbstractWrappingTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpWrappingCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSyntaxError()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        if ([||]i && j
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSelection()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        if ([|i|] && j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingBeforeExpr()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        [||]if (i && j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSingleExpr()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        if ([||]i) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithMultiLineExpression()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        if ([||]i && (j +
            k)) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithMultiLineExpr2()
        {
            await TestMissingAsync(
@"class C {
    void Bar() {
        if ([||]i && @""
        "") {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIf()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Bar() {
        if ([||]i && j) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i &&
            j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIf_IncludingOp()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Bar() {
        if ([||]i && j) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i
            && j) {
        }
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIf2()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Bar() {
        if (i[||] && j) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i &&
            j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIf3()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Bar() {
        if (i [||]&& j) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i &&
            j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIf4()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Bar() {
        if (i &&[||] j) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i &&
            j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIf5()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Bar() {
        if (i && [||]j) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i &&
            j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestTwoParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        a.Goobar([||]i, j);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
                 j);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i,
            j);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
            j);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i, j);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestThreeParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        a.Goobar([||]i, j, k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
                 j,
                 k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i,
            j,
            k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
            j,
            k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i, j, k);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_AllOptions_NoInitialMatches()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        a.Goobar(
            [||]i,
                j,
                    k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
                 j,
                 k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i,
            j,
            k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
            j,
            k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i, j, k);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i, j, k);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_ShortIds()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo() {
        this.Goobar([||]
            i, j, k, l, m, n, o, p,
            n);
    }
}",
GetIndentionColumn(30),
@"class C {
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
}",
@"class C {
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
}",
@"class C {
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
}",
@"class C {
    void Goo() {
        this.Goobar(i, j, k, l, m, n, o, p, n);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            i, j, k, l, m, n, o, p, n);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(i, j, k, l,
                    m, n, o, p,
                    n);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            i, j, k, l, m, n,
            o, p, n);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(i, j, k, l,
            m, n, o, p, n);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_VariadicLengthIds()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo() {
        this.Goobar([||]
            i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
GetIndentionColumn(30),
@"class C {
    void Goo() {
        this.Goobar(i,
                    jj,
                    kkkkk,
                    llllllll,
                    mmmmmmmmmmmmmmmmmm,
                    nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            i,
            jj,
            kkkkk,
            llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(i,
            jj,
            kkkkk,
            llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(i, jj,
                    kkkkk,
                    llllllll,
                    mmmmmmmmmmmmmmmmmm,
                    nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            i, jj, kkkkk,
            llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(i, jj,
            kkkkk, llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo() {
        this.Goobar([||]
            iiiii, jjjjj, kkkkk, lllll, mmmmm,
            nnnnn);
    }
}",
GetIndentionColumn(25),
@"class C {
    void Goo() {
        this.Goobar(iiiii,
                    jjjjj,
                    kkkkk,
                    lllll,
                    mmmmm,
                    nnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(
            iiiii, jjjjj,
            kkkkk, lllll,
            mmmmm, nnnnn);
    }
}",
@"class C {
    void Goo() {
        this.Goobar(iiiii,
            jjjjj, kkkkk,
            lllll, mmmmm,
            nnnnn);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        a.[||]Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm,
            nnnnn);
    }
}",
GetIndentionColumn(20),
@"class C {
    void Bar() {
        a.Goobar(iiiii,
                 jjjjj,
                 kkkkk,
                 lllll,
                 mmmmm,
                 nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_VariadicLengthIds2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        a.[||]Goobar(
            i, jj, kkkk, lll, mm,
            n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Bar() {
        a.Goobar(i,
                 jj,
                 kkkk,
                 lll,
                 mm,
                 n) {
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i,
            jj,
            kkkk,
            lll,
            mm,
            n) {
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
            jj,
            kkkk,
            lll,
            mm,
            n) {
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i, jj, kkkk, lll, mm, n) {
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i, jj, kkkk, lll, mm, n) {
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i, jj, kkkk,
                 lll, mm, n) {
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i, jj, kkkk, lll,
            mm, n) {
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i, jj, kkkk,
            lll, mm, n) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferExistingOption1()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        a.[||]Goobar(iiiii,
                 jjjjj,
                 kkkkk,
                 lllll,
                 mmmmm,
                 nnnnn);
    }
}",
GetIndentionColumn(30),
@"class C {
    void Bar() {
        a.Goobar(
            iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(iiiii, jjjjj,
                 kkkkk, lllll,
                 mmmmm, nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            iiiii, jjjjj,
            kkkkk, lllll,
            mmmmm, nnnnn);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(iiiii, jjjjj,
            kkkkk, lllll,
            mmmmm, nnnnn);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferExistingOption2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        a.Goobar([||]
            i,
            jj,
            kkkk,
            lll,
            mm,
            n);
    }
}",
GetIndentionColumn(30),
@"class C {
    void Bar() {
        a.Goobar(i,
                 jj,
                 kkkk,
                 lll,
                 mm,
                 n);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i,
            jj,
            kkkk,
            lll,
            mm,
            n);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i, jj, kkkk, lll, mm, n);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i, jj, kkkk, lll, mm, n);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i, jj, kkkk,
                 lll, mm, n);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(
            i, jj, kkkk, lll,
            mm, n);
    }
}",
@"class C {
    void Bar() {
        a.Goobar(i, jj, kkkk,
            lll, mm, n);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInElementAccess1()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo() {
        var v = this[[||]a, b, c];
    }
}",
@"class C {
    void Goo() {
        var v = this[a,
                     b,
                     c];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInElementAccess2()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo() {
        var v = [||]this[a, b, c];
    }
}",
@"class C {
    void Goo() {
        var v = this[a,
                     b,
                     c];
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInObjectCreation1()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo() {
        var v = [||]new Bar(a, b, c);
    }
}",
@"class C {
    void Goo() {
        var v = new Bar(a,
                        b,
                        c);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInObjectCreation2()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo() {
        var v = new Bar([||]a, b, c);
    }
}",
@"class C {
    void Goo() {
        var v = new Bar(a,
                        b,
                        c);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInConstructorInitializer1()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public C() : base([||]a, b, c) {
    }
}",
@"class C {
    public C() : base(a,
                      b,
                      c) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInConstructorInitializer2()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public C() : [||]base(a, b, c) {
    }
}",
@"class C {
    public C() : base(a,
                      b,
                      c) {
    }
}");
        }
    }
}
