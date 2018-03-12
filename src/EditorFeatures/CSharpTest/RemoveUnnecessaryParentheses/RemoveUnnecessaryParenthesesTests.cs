// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryParentheses
{
    public partial class RemoveUnnecessaryParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryParenthesesCodeFixProvider());

        private async Task TestAsync(string initial, string expected, bool requireAllParenthesesForClarity)
        {
            await TestInRegularAndScriptAsync(initial, expected, options: RemoveAllUnnecessaryParentheses);

            if (requireAllParenthesesForClarity)
            {
                await TestInRegularAndScriptAsync(initial, expected, options: RequireAllParenthesesForClarity);
            }
            else
            {
                await TestMissingAsync(initial, parameters: new TestParameters(options: RequireAllParenthesesForClarity));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestVariableInitializer_TestWithAllOptionsSetToIgnore()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$(1);
    }
}", new TestParameters(options: IgnoreAllParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArithmeticRequiredForClarity1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 + $$(2 * 3);
    }
}", new TestParameters(options: RequireArithmeticParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArithmeticRequiredForClarity2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a || $$(b && c);
    }
}",
@"class C
{
    void M()
    {
        int x = a || b && c;
    }
}", parameters: new TestParameters(options: RequireArithmeticParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLogicalRequiredForClarity1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = a || $$(b && c);
    }
}", new TestParameters(options: RequireLogicalParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLogicalRequiredForClarity2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a + $$(b * c);
    }
}",
@"class C
{
    void M()
    {
        int x = a + b * c;
    }
}", parameters: new TestParameters(options: RequireLogicalParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Integral1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 1 + $$(2 + 3);
    }
}",
@"class C
{
    void M()
    {
        int x = 1 + 2 + 3;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Integral2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = $$(1 + 2) + 3;
    }
}",
@"class C
{
    void M()
    {
        int x = 1 + 2 + 3;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArithmeticRequiredForCorrectnessWhenPrecedenceStaysTheSameIfFloatingPoint()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1.0 + $$(2.0 + 3.0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Floating2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = $$(1.0 + 2.0) + 3.0;
    }
}",
@"class C
{
    void M()
    {
        int x = 1.0 + 2.0 + 3.0;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = a || $$(b || c);
    }
}",
@"class C
{
    void M()
    {
        int x = a || b || c;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = $$(a || b) || c;
    }
}",
@"class C
{
    void M()
    {
        int x = a || b || c;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestVariableInitializer_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = $$(1);
    }
}",
@"class C
{
    void M()
    {
        int x = 1;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestReturnStatement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        return $$(1 + 2);
    }
}",
@"class C
{
    void M()
    {
        return 1 + 2;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestExpressionBody_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    int M() => $$(1 + 2);
}",
@"class C
{
    int M() => 1 + 2;
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCheckedExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int i = checked($$(1 + 2));
    }
}",
@"class C
{
    void M()
    {
        int i = checked(1 + 2);
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAssignment_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        i = $$(1 + 2);
    }
}",
@"class C
{
    void M()
    {
        i = 1 + 2;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNestedParenthesizedExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int i = ( $$(1 + 2) );
    }
}",
@"class C
{
    void M()
    {
        int i = ( 1 + 2 );
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestIncrementExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int i = $$(x++);
    }
}",
@"class C
{
    void M()
    {
        int i = x++;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestLambdaBody_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        Func<int> i = () => $$(1);
    }
}",
@"class C
{
    void M()
    {
        Func<int> i = () => 1;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArrayElement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int[] i = new int[] { $$(1) };
    }
}",
@"class C
{
    void M()
    {
        int[] i = new int[] { 1 };
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestWhereClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = from c in customer
                where $$(c.Age > 21)
                select c;
    }
}",
@"class C
{
    void M()
    {
        var q = from c in customer
                where c.Age > 21
                select c;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int i = (int)$$(1);
    }
}",
@"class C
{
    void M()
    {
        int i = (int)1;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForConditionalAccess()
        {
            await TestMissingAsync(
@"class C
{
    void M(string s)
    {
        var v = $$(s?.Length).ToString();
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForConditionalIndex()
        {
            await TestMissingAsync(
@"class C
{
    void M(string s)
    {
        var v = $$(s?[0]).ToString();
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestBinaryInCastExpression()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int i = (int)$$(1 + 2);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAroundCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int i = $$((int)1);
    }
}",
@"class C
{
    void M()
    {
        int i = (int)1;
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalInInterpolation()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var s = $""{ $$(a ? b : c) }"";
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalInInterpolation_FixAll_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var s1 = $""{ {|FixAllInDocument:(|}(a ? b : c)) }"";
        var s2 = $""{ ((a ? b : c)) }"";
    }
}",
@"class C
{
    void M()
    {
        var s1 = $""{ (a ? b : c) }"";
        var s2 = $""{ (a ? b : c) }"";
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalInInterpolation_FixAll_2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var s1 = $""{ ({|FixAllInDocument:(|}a ? b : c)) }"";
        var s2 = $""{ ((a ? b : c)) }"";
    }
}",
@"class C
{
    void M()
    {
        var s1 = $""{ (a ? b : c) }"";
        var s2 = $""{ (a ? b : c) }"";
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNonConditionalInInterpolation_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var s = $""{ $$(true) }"";
    }
}",
@"class C
{
    void M()
    {
        var s = $""{ true }"";
    }
}", requireAllParenthesesForClarity: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = $$(a * b) + c;
    }
}",
@"class C
{
    void M()
    {
        var q = a * b + c;
    }
}", requireAllParenthesesForClarity: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = c + $$(a * b);
    }
}",
@"class C
{
    void M()
    {
        var q = c + a * b;
    }
}", requireAllParenthesesForClarity: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var q = $$(a * b) ? (1 + 2) : (3 + 4);
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var q = (a * b) ? $$(1 + 2) : (3 + 4);
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var q = (a * b) ? (1 + 2) : $$(3 + 4);
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestIsPattern_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if ( $$(a[0]) is string s) { }
    }
}",
@"class C
{
    void M()
    {
        if ( a[0] is string s) { }
    }
}", requireAllParenthesesForClarity: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestForOverloadedOperatorOnLeft()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(C c1, C c2, C c3)
    {
        var x = $$(c1 + c2) + c3;
    }

    public static C operator +(C c1, C c2) => null;
}",
@"class C
{
    void M(C c1, C c2, C c3)
    {
        var x = c1 + c2 + c3;
    }

    public static C operator +(C c1, C c2) => null;
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForOverloadedOperatorOnRight()
        {
            await TestMissingAsync(
@"class C
{
    void M(C c1, C c2, C c3)
    {
        var x = c1 + $$(c2 + c3);
    }

    public static C operator +(C c1, C c2) => null;
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestShiftRequiredForClarity1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x =  $$(1 + 2) << 3;
    }
}", parameters: new TestParameters(options: RequireShiftParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestShiftRequiredForClarity2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$(1 + 2) << 3;
    }
}", parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestRemoveShiftIfNotNecessary1()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = $$(1 + 2) << 3;
    }
}",
@"class C
{
    void M()
    {
        int x = 1 + 2 << 3;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestRemoveCoalesceIfNotNecessary1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$(a ?? b) ?? c;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestRemoveCoalesceIfNotNecessary2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = a ?? $$(b ?? c);
    }
}",
@"class C
{
    void M()
    {
        int x = a ?? b ?? c;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestBitwiseExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = $$(a + b) & c;
    }
}",
@"class C
{
    void M()
    {
        var q = a + b & c;
    }
}", requireAllParenthesesForClarity: false);
        }
    }
}
