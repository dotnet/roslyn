// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Editor.Wrapping;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class ParameterWrappingTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpParameterWrappingCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        private Dictionary<OptionKey, object> GetIndentionColumn(int column)
            => new Dictionary<OptionKey, object>
               {
                   { FormattingOptions.PreferredWrappingColumn, column }
               };

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSyntaxError()
        {
            await TestMissingAsync(
@"class C {
    void Foo([||]int i, int j {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSelection()
        {
            await TestMissingAsync(
@"class C {
    void Foo([|int|] i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingInBody()
        {
            await TestMissingAsync(
@"class C {
    void Foo(int i, int j) {[||]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingInAttributes()
        {
            await TestMissingAsync(
@"class C {
    [||][Attr]
    void Foo(int i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithSingleParameter()
        {
            await TestMissingAsync(
@"class C {
    void Foo([||]int i) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestMissingWithMultiLineParameter()
        {
            await TestMissingAsync(
@"class C {
    void Foo([||]int i, int j =
        initializer) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader1()
        {
            await TestInRegularAndScript1Async(
@"class C {
    [||]void Foo(int i, int j) {
    }
}",
@"class C {
    void Foo(int i,
             int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader2()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void [||]Foo(int i, int j) {
    }
}",
@"class C {
    void Foo(int i,
             int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader3()
        {
            await TestInRegularAndScript1Async(
@"class C {
    [||]public void Foo(int i, int j) {
    }
}",
@"class C {
    public void Foo(int i,
                    int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInHeader4()
        {
            await TestInRegularAndScript1Async(
@"class C {
    public void Foo(int i, int j)[||] {
    }
}",
@"class C {
    public void Foo(int i,
                    int j) {
    }
}");
        }

        private Task TestAllWrappingCasesAsync(
            string input,
            params string[] outputs)
        {
            return TestAllWrappingCasesAsync(input, options: null, outputs);
        }

        private async Task TestAllWrappingCasesAsync(
            string input,
            Dictionary<OptionKey, object> options,
            params string[] outputs)
        {
            var parameters = new TestParameters(options: options);

            for (int index = 0; index < outputs.Length; index++)
            {
                var output = outputs[index];
                await TestInRegularAndScript1Async(input, output, index, parameters: parameters);
            }

            await TestActionCountAsync(input, outputs.Length, parameters);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestTwoParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Foo([||]int i, int j) {
    }
}",
@"class C {
    void Foo(int i,
             int j) {
    }
}",
@"class C {
    void Foo(
        int i,
        int j) {
    }
}",
@"class C {
    void Foo(int i,
        int j) {
    }
}",
@"class C {
    void Foo(
        int i, int j) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestThreeParamWrappingCases()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Foo([||]int i, int j, int k) {
    }
}",
@"class C {
    void Foo(int i,
             int j,
             int k) {
    }
}",
@"class C {
    void Foo(
        int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Foo(int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Foo(
        int i, int j, int k) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_AllOptions_NoInitialMatches()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Foo([||]
        int i,
            int j,
                int k) {
    }
}",
@"class C {
    void Foo(int i,
             int j,
             int k) {
    }
}",
@"class C {
    void Foo(
        int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Foo(int i,
        int j,
        int k) {
    }
}",
@"class C {
    void Foo(int i, int j, int k) {
    }
}",
@"class C {
    void Foo(
        int i, int j, int k) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_ShortIds()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Foo([||]
        int i, int j, int k, int l, int m,
        int n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Foo(int i,
             int j,
             int k,
             int l,
             int m,
             int n) {
    }
}",
@"class C {
    void Foo(
        int i,
        int j,
        int k,
        int l,
        int m,
        int n) {
    }
}",
@"class C {
    void Foo(int i,
        int j,
        int k,
        int l,
        int m,
        int n) {
    }
}",
@"class C {
    void Foo(int i, int j, int k, int l, int m, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int j, int k, int l, int m, int n) {
    }
}",
@"class C {
    void Foo(int i, int j,
             int k, int l,
             int m, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int j, int k,
        int l, int m, int n) {
    }
}",
@"class C {
    void Foo(int i, int j, int k,
        int l, int m, int n) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_VariadicLengthIds()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Foo([||]
        int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Foo(int i,
             int jj,
             int kkkk,
             int llllllll,
             int mmmmmmmmmmmmmmmm,
             int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Foo(
        int i,
        int jj,
        int kkkk,
        int llllllll,
        int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Foo(int i,
        int jj,
        int kkkk,
        int llllllll,
        int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Foo(int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm, int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Foo(
        int i, int jj, int kkkk, int llllllll, int mmmmmmmmmmmmmmmm, int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Foo(int i, int jj,
             int kkkk,
             int llllllll,
             int mmmmmmmmmmmmmmmm,
             int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Foo(
        int i, int jj,
        int kkkk, int llllllll,
        int mmmmmmmmmmmmmmmm,
        int nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn) {
    }
}",
@"class C {
    void Foo(int i, int jj,
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
    void Foo([||]
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm,
        int nnnnn) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Foo(int iiiii,
             int jjjjj,
             int kkkkk,
             int lllll,
             int mmmmm,
             int nnnnn) {
    }
}",
@"class C {
    void Foo(
        int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Foo(int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Foo(int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Foo(
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Foo(
        int iiiii, int jjjjj,
        int kkkkk, int lllll,
        int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Foo(int iiiii, int jjjjj,
        int kkkkk, int lllll,
        int mmmmm, int nnnnn) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Foo([||]
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm,
        int nnnnn) {
    }
}",
GetIndentionColumn(20),
@"class C {
    void Foo(int iiiii,
             int jjjjj,
             int kkkkk,
             int lllll,
             int mmmmm,
             int nnnnn) {
    }
}",
@"class C {
    void Foo(
        int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Foo(int iiiii,
        int jjjjj,
        int kkkkk,
        int lllll,
        int mmmmm,
        int nnnnn) {
    }
}",
@"class C {
    void Foo(int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}",
@"class C {
    void Foo(
        int iiiii, int jjjjj, int kkkkk, int lllll, int mmmmm, int nnnnn) {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_LongWrapping_VariadicLengthIds2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Foo([||]
        int i, int jj, int kkkk, int lll, int mm,
        int n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Foo(int i,
             int jj,
             int kkkk,
             int lll,
             int mm,
             int n) {
    }
}",
@"class C {
    void Foo(
        int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Foo(int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Foo(int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Foo(int i, int jj,
             int kkkk, int lll,
             int mm, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}",
@"class C {
    void Foo(int i, int jj,
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
    void Foo([||]int i,
             int jj,
             int kkkk,
             int lll,
             int mm,
             int n) {
    }
}",
GetIndentionColumn(30),
@"class C {
    void Foo(
        int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Foo(int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Foo(int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Foo(int i, int jj,
             int kkkk, int lll,
             int mm, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}",
@"class C {
    void Foo(int i, int jj,
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
    void Foo([||]
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
    void Foo(int i,
             int jj,
             int kkkk,
             int lll,
             int mm,
             int n) {
    }
}",
@"class C {
    void Foo(int i,
        int jj,
        int kkkk,
        int lll,
        int mm,
        int n) {
    }
}",
@"class C {
    void Foo(int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int jj, int kkkk, int lll, int mm, int n) {
    }
}",
@"class C {
    void Foo(int i, int jj,
             int kkkk, int lll,
             int mm, int n) {
    }
}",
@"class C {
    void Foo(
        int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}",
@"class C {
    void Foo(int i, int jj,
        int kkkk, int lll,
        int mm, int n) {
    }
}");
        }
    }
}
