// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Wrapping;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public class BinaryExpressionWrappingTests : AbstractWrappingTests
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpWrappingCodeRefactoringProvider();

        private IDictionary<OptionKey, object> EndOfLine => Option(
            CodeStyleOptions.OperatorPlacementWhenWrapping,
            OperatorPlacementWhenWrappingPreference.EndOfLine);

        private IDictionary<OptionKey, object> BeginningOfLine => Option(
            CodeStyleOptions.OperatorPlacementWhenWrapping,
            OperatorPlacementWhenWrappingPreference.BeginningOfLine);

        private Task TestEndOfLine(string markup, string expected)
            => TestInRegularAndScript1Async(markup, expected, parameters: new TestParameters(
                options: EndOfLine));

        private Task TestBeginningOfLine(string markup, string expected)
            => TestInRegularAndScript1Async(markup, expected, parameters: new TestParameters(
                options: BeginningOfLine));

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
            await TestEndOfLine(
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
            await TestBeginningOfLine(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInIf2()
        {
            await TestEndOfLine(
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
            await TestEndOfLine(
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
            await TestEndOfLine(
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
            await TestEndOfLine(
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
        public async Task TestTwoExprWrappingCases_End()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]i && j) {
        }
    }
}",
EndOfLine,
@"class C {
    void Bar() {
        if (i &&
            j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestTwoExprWrappingCases_Beginning()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]i && j) {
        }
    }
}",
BeginningOfLine,
@"class C {
    void Bar() {
        if (i
            && j) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestThreeExprWrappingCases_End()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]i && j || k) {
        }
    }
}",
EndOfLine,
@"class C {
    void Bar() {
        if (i &&
            j ||
            k) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestThreeExprWrappingCases_Beginning()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]i && j || k) {
        }
    }
}",
BeginningOfLine,
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
        public async Task Test_AllOptions_NoInitialMatches_End()
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
EndOfLine,
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
            i && j || k) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task Test_AllOptions_NoInitialMatches_Beginning()
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
BeginningOfLine,
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
        public async Task Test_DoNotOfferExistingOption2_End()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]a
            && b) {
        }
    }
}",
EndOfLine,
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
        public async Task Test_DoNotOfferExistingOption2_Beginning()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        if ([||]a
            && b) {
        }
    }
}",
BeginningOfLine,
@"class C {
    void Bar() {
        if (a && b) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInLocalInitializer_Beginning()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo() {
        var v = [||]a && b && c;
    }
}",
BeginningOfLine,
@"class C {
    void Goo() {
        var v = a
            && b
            && c;
    }
}",
@"class C {
    void Goo() {
        var v = a
                && b
                && c;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInLocalInitializer_End()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Goo() {
        var v = [||]a && b && c;
    }
}",
EndOfLine,
@"class C {
    void Goo() {
        var v = a &&
            b &&
            c;
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
        public async Task TestInField_Beginning()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    bool v = [||]a && b && c;
}",
BeginningOfLine,
@"class C {
    bool v = a
        && b
        && c;
}",
@"class C {
    bool v = a
             && b
             && c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInField_End()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    bool v = [||]a && b && c;
}",
EndOfLine,
@"class C {
    bool v = a &&
        b &&
        c;
}",
@"class C {
    bool v = a &&
             b &&
             c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestAddition_End()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        var goo = [||]""now"" + ""is"" + ""the"" + ""time"";
    }
}",
EndOfLine,
@"class C {
    void Bar() {
        var goo = ""now"" +
            ""is"" +
            ""the"" +
            ""time"";
    }
}",
@"class C {
    void Bar() {
        var goo = ""now"" +
                  ""is"" +
                  ""the"" +
                  ""time"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestAddition_Beginning()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    void Bar() {
        var goo = [||]""now"" + ""is"" + ""the"" + ""time"";
    }
}",
BeginningOfLine,
@"class C {
    void Bar() {
        var goo = ""now""
            + ""is""
            + ""the""
            + ""time"";
    }
}",
@"class C {
    void Bar() {
        var goo = ""now""
                  + ""is""
                  + ""the""
                  + ""time"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestUnderscoreName_End()
        {
            await TestEndOfLine(
@"class C {
    void Bar() {
        if ([||]i is var _ && _ != null) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i is var _ &&
            _ != null) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestUnderscoreName_Beginning()
        {
            await TestBeginningOfLine(
@"class C {
    void Bar() {
        if ([||]i is var _ && _ != null) {
        }
    }
}",
@"class C {
    void Bar() {
        if (i is var _
            && _ != null) {
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInField_Already_Wrapped_Beginning()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    bool v =
        [||]a && b && c;
}",
BeginningOfLine,
@"class C {
    bool v =
        a
        && b
        && c;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)]
        public async Task TestInField_Already_Wrapped_End()
        {
            await TestAllWrappingCasesAsync(
@"class C {
    bool v =
        [||]a && b && c;
}",
EndOfLine,
@"class C {
    bool v =
        a &&
        b &&
        c;
}");
        }
    }
}
