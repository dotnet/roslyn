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
        if ([||]i && (j && )
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
        public async Task TestTwoExprWrappingCases()
        {
            await TestAllWrappingCasesAsync(
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
}",
@"class C {
    void Bar() {
        if (i
            && j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestThreeExprWrappingCases()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]i && j || k) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i &&
            j ||
            k) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i
            && j
            || k) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_AllOptions_NoInitialMatches()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if (
            [||]i   &&
                j
                 ||   k) {
        }
    }
}",
@"class C {
    void Bar() {
        if (
            i &&
            j ||
            k) {
        }
    }
}",
@"class C {
    void Bar() {
        if (
            i
            && j
            || k) {
        }
    }
}",
@"class C {
    void Bar() {
        if (
            i && j || k) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferExistingOption1()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]a &&
            b) {
        }
    }
}",
@"class C {
    void Bar() {
        if (a
            && b) {
        }
    }
}",
@"class C {
    void Bar() {
        if (a && b) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_DoNotOfferExistingOption2()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]a
            && b) {
        }
    }
}",
@"class C {
    void Bar() {
        if (a &&
            b) {
        }
    }
}",
@"class C {
    void Bar() {
        if (a && b) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInLocalInitializer()
        {
            await TestInRegularAndScript1Async(
@"class C {
    void Goo() {
        var v = [||]a && b && c;
    }
}",
@"class C {
    void Goo() {
        var v = a &&
                b &&
                c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInField()
        {
            await TestInRegularAndScript1Async(
@"class C {
    bool v = [||]a && b && c;
}",
@"class C {
    bool v = a &&
             b &&
             c;
}");
        }
    }
}
