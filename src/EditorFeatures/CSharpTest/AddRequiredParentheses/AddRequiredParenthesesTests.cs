// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses
{
    public partial class AddRequiredParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAddRequiredParenthesesDiagnosticAnalyzer(), new AddRequiredParenthesesCodeFixProvider());
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestArithmeticPrecedence()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = 1 + 2 $$* 3;
    }
}",
@"class C
{
    void M()
    {
        int x = 1 + (2 * 3);
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNoArithmeticOnLowerPrecedence()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 * 3;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfArithmeticPrecedenceStaysTheSame()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 $$+ 3;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfArithmeticPrecedenceIsNotEnforced1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 $$+ 3;
    }
}", parameters: new TestParameters(options: RequireLogicalParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfArithmeticPrecedenceIsNotEnforced2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + 2 $$* 3;
    }
}", parameters: new TestParameters(options: RequireLogicalParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedence()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a || b $$&& c;
    }
}",
@"class C
{
    void M()
    {
        int x = a || (b && c);
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNoLogicalOnLowerPrecedence()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$|| b && c;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfLogicalPrecedenceStaysTheSame()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a || b $$|| c;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfLogicalPrecedenceIsNotEnforced()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a || b $$|| c;
    }
}", parameters: new TestParameters(options: RequireArithmeticParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestMixedArithmeticAndLogical()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a == b $$&& c == d;
    }
}", new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a || b $$&& c && d;
    }
}",
@"class C
{
    void M()
    {
        int x = a || (b && c && d);
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a || b && c $$&& d;
    }
}",
@"class C
{
    void M()
    {
        int x = a || (b && c && d);
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestShiftPrecedence1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 << 3;
    }
}",
@"class C
{
    void M()
    {
        int x = (1 + 2) << 3;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestShiftPrecedence2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 << 3;
    }
}",
@"class C
{
    void M()
    {
        int x = (1 + 2) << 3;
    }
}", parameters: new TestParameters(options: RequireShiftParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestShiftPrecedence3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 << 3;
    }
}", parameters: new TestParameters(options: RequireArithmeticParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfShiftPrecedenceStaysTheSame1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 $$<< 2 << 3;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestNotIfShiftPrecedenceStaysTheSame2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 << 2 $$<< 3;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestEqualityPrecedence1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 == 2 + 3;
    }
}",
@"class C
{
    void M()
    {
        int x = (1 + 2) == 2 + 3;
    }
}", parameters: new TestParameters(options: RequireEqualityParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestEqualityPrecedence2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = 1 + 2 == 2 $$+ 3;
    }
}",
@"class C
{
    void M()
    {
        int x = 1 + 2 == (2 + 3);
    }
}", parameters: new TestParameters(options: RequireEqualityParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a $$+ b ?? c;
    }
}",
@"class C
{
    void M()
    {
        int x = (a + b) ?? c;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$?? b ?? c;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestCoalescePrecedence3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a ?? b $$?? c;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = 1 $$+ 2 & 3;
    }
}",
@"class C
{
    void M()
    {
        int x = (1 + 2) & 3;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$| b | c;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence3()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a | b $$& c;
    }
}",
@"class C
{
    void M()
    {
        int x = a | (b & c);
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
        public async Task TestBitwisePrecedence4()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a $$| b & c;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }
    }
}
