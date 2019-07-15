// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class ParameterWrappingTests : AbstractWrappingTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpWrappingCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSyntaxError()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]int i, int j {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSelection()
        {
            await TestMissingAsync(
@"class C {
    void Goo([|int|] i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingInBody()
        {
            await TestMissingAsync(
@"class C {
    void Goo(int i, int j) {[||]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingInAttributes()
        {
            await TestMissingAsync(
@"class C {
    [||][Attr]
    void Goo(int i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithOpenTokenTrailingComment()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]/**/int i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithItemLeadingComment()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]
        /**/int i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithItemTrailingComment()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]
        int i/**/, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithCommaTrailingComment()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]
        int i,/**/int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithLastItemTrailingComment()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]
        int i, int j/**/
        ) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithCloseTokenLeadingComment()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]
        int i, int j
        /**/) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestWithOpenTokenLeadingComment()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo/**/([||]int i, int j) {
    }
}",

@"class C {
    void Goo/**/(int i,
                 int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestWithCloseTokenTrailingComment()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo([||]int i, int j)/**/ {
    }
}",

@"class C {
    void Goo(int i,
             int j)/**/ {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSingleParameter()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]int i) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithMultiLineParameter()
        {
            await TestMissingAsync(
@"class C {
    void Goo([||]int i, int j =
        initializer) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader1()
        {
            await TestInRegularAndScript1Async(
@"class C {
    [||]void Goo(int i, int j) {
    }
}",
@"class C {
    void Goo(int i,
             int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader2()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void [||]Goo(int i, int j) {
    }
}",
@"class C {
    void Goo(int i,
             int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader3()
        {
            await TestInRegularAndScript1Async(
@"class C {
    [||]public void Goo(int i, int j) {
    }
}",
@"class C {
    public void Goo(int i,
                    int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader4()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public void Goo(int i, int j)[||] {
    }
}",
@"class C {
    public void Goo(int i,
                    int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestTwoParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]int i, int j) {
    }
}",
@"class C {
    void Goo(int i,
             int j) {
    }
}",
@"class C {
    void Goo(
        int i,
        int j) {
    }
}",
@"class C {
    void Goo(int i,
        int j) {
    }
}",
@"class C {
    void Goo(
        int i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestThreeParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]int i, int j, int k) {
    }
}",
@"class C {
    void Goo(int i,
             int j,
             int k) {
    }
}",
@"class C {
    void Goo(
        int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Goo(int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Goo(
        int i, int j, int k) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_AllOptions_NoInitialMatches()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]
        int i,
            int j,
                int k) {
    }
}",
@"class C {
    void Goo(int i,
             int j,
             int k) {
    }
}",
@"class C {
    void Goo(
        int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Goo(int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Goo(int i, int j, int k) {
    }
}",
@"class C {
    void Goo(
        int i, int j, int k) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_ShortIds()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]
        int i, int j, int k, int l, int m,
        int n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Goo(int i,
             int j,
             int k,
             int l,
             int m,
             int n) {
    }
}",
@"class C {
    void Goo(
        int i,
        int j,
        int k,
        int l,
        int m,
        int n) {
    }
}",
@"class C {
    void Goo(int i,
        int j,
        int k,
        int l,
        int m,
        int n) {
    }
}",
@"class C {
    void Goo(int i, int j, int k, int l, int m, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int j, int k, int l, int m, int n) {
    }
}",
@"class C {
    void Goo(int i, int j,
             int k, int l,
             int m, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int j, int k,
        int l, int m, int n) {
    }
}",
@"class C {
    void Goo(int i, int j,
        int k, int l, int m,
        int n) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_VariadicLengthIds()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]
        int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Goo(int i,
             int jj,
             int kkkk,
             int llllllll,
             int mmmmmmmmmmmmmmmm,
             int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Goo(
        int i,
        int jj,
        int kkkk,
        int llllllll,
        int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Goo(int i,
        int jj,
        int kkkk,
        int llllllll,
        int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Goo(int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm, int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Goo(
        int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm, int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Goo(int i, int jj,
             int kkkk,
             int llllllll,
             int mmmmmmmmmmmmmmmm,
             int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Goo(
        int i, int jj,
        int kkkk, int llllllll,
        int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Goo(int i, int jj,
        int kkkk, int llllllll,
        int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm,
        int nnnnn) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Goo(int iiiii,
             int jjjjj,
             int kkkkk,
             int lllll,
             int mmmmm,
             int nnnnn) {
    }
}",
@"class C {
    void Goo(
        int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Goo(int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Goo(int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Goo(
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Goo(
        int iiiii, int jjjjj,
        int kkkkk, int lllll,
        int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Goo(int iiiii,
        int jjjjj, int kkkkk,
        int lllll, int mmmmm,
        int nnnnn) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm,
        int nnnnn) {
    }
}",
GetIndentionColumn(20),
@"class C {
    void Goo(int iiiii,
             int jjjjj,
             int kkkkk,
             int lllll,
             int mmmmm,
             int nnnnn) {
    }
}",
@"class C {
    void Goo(
        int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Goo(int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Goo(int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Goo(
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_VariadicLengthIds2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]
        int i, int jj, int kkkk, int lll, int mm,
        int n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Goo(int i,
             int jj,
             int kkkk,
             int lll,
             int mm,
             int n) {
    }
}",
@"class C {
    void Goo(
        int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Goo(int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Goo(int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Goo(int i, int jj,
             int kkkk, int lll,
             int mm, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}",
@"class C {
    void Goo(int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferExistingOption1()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]int i,
             int jj,
             int kkkk,
             int lll,
             int mm,
             int n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Goo(
        int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Goo(int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Goo(int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Goo(int i, int jj,
             int kkkk, int lll,
             int mm, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}",
@"class C {
    void Goo(int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferExistingOption2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo([||]
        int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Goo(int i,
             int jj,
             int kkkk,
             int lll,
             int mm,
             int n) {
    }
}",
@"class C {
    void Goo(int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Goo(int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Goo(int i, int jj,
             int kkkk, int lll,
             int mm, int n) {
    }
}",
@"class C {
    void Goo(
        int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}",
@"class C {
    void Goo(int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInConstructor()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public [||]C(int i, int j) {
    }
}",
@"class C {
    public C(int i,
             int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIndexer()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public int [||]this[int i, int j] => 0;
}",
@"class C {
    public int this[int i,
                    int j] => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInOperator()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public shared int operator [||]+(C c1, C c2) => 0;
}",
@"class C {
    public shared int operator +(C c1,
                                 C c2) => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInDelegate()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public delegate int [||]D(C c1, C c2);
}",
@"class C {
    public delegate int D(C c1,
                          C c2);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInParenthesizedLambda()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo()
    {
        var v = ([||]C c, C d) => {
        };
    }
}",
@"class C {
    void Goo()
    {
        var v = (C c,
                 C d) => {
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInParenthesizedLambda2()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo()
    {
        var v = ([||]c, d) => {
        };
    }
}",
@"class C {
    void Goo()
    {
        var v = (c,
                 d) => {
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestNotOnSimpleLambda()
        {
            await TestMissingAsync(
@"class C {
    void Goo()
    {
        var v = [||]c => {
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestLocalFunction()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo()
    {
        void Local([||]C c, C d) {
        }
    }
}",
@"class C {
    void Goo()
    {
        void Local(C c,
                   C d) {
        }
    }
}");
        }
    }
}
