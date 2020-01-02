// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryParentheses
{
    public partial class RemoveUnnecessaryParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryParenthesesDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryParenthesesCodeFixProvider());

        private async Task TestAsync(string initial, string expected, bool offeredWhenRequireForClarityIsEnabled, int index = 0)
        {
            await TestInRegularAndScriptAsync(initial, expected, options: RemoveAllUnnecessaryParentheses, index: index);

            if (offeredWhenRequireForClarityIsEnabled)
            {
                await TestInRegularAndScriptAsync(initial, expected, options: RequireAllParenthesesForClarity, index: index);
            }
            else
            {
                await TestMissingAsync(initial, parameters: new TestParameters(options: RequireAllParenthesesForClarity));
            }
        }

        internal override bool ShouldSkipMessageDescriptionVerification(DiagnosticDescriptor descriptor)
        {
            return descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary) && descriptor.DefaultSeverity == DiagnosticSeverity.Hidden;
        }

        private DiagnosticDescription GetRemoveUnnecessaryParenthesesDiagnostic(string text, int line, int column)
        {
            var diagnosticId = IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId;
            return TestHelpers.Diagnostic(diagnosticId, text, startLocation: new LinePosition(line, column));
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
        [WorkItem(29736, "https://github.com/dotnet/roslyn/issues/29736")]
        public async Task TestVariableInitializer_TestMissingParenthesis()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$(1;
    }
}");
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
}", new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));
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
}", parameters: new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));
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
}", new TestParameters(options: RequireOtherBinaryParenthesesForClarity));
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
}", parameters: new TestParameters(options: RequireOtherBinaryParenthesesForClarity));
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity()
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
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCompoundAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        i *= $$(1 + 2);
    }
}",
@"class C
{
    void M()
    {
        i *= 1 + 2;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPimaryAssignment_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        i = $$(s.Length);
    }
}",
@"class C
{
    void M()
    {
        i = s.Length;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true, index: 1);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: false);
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
}", offeredWhenRequireForClarityIsEnabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalExpression_TestNotAvailableForComplexChildren1()
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
        public async Task TestConditionalExpression_TestNotAvailableForComplexChildren2()
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
        public async Task TestConditionalExpression_TestNotAvailableForComplexChildren3()
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
        public async Task TestConditionalExpression_TestAvailableForPrimaryChildren1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = $$(a.X()) ? (1 + 2) : (3 + 4);
    }
}",
@"class C
{
    void M()
    {
        var q = a.X() ? (1 + 2) : (3 + 4);
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalExpression_TestAvailableForPrimaryChildren2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = (a.X()) ? $$(x.Length) : (3 + 4);
    }
}",
@"class C
{
    void M()
    {
        var q = (a.X()) ? x.Length : (3 + 4);
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalExpression_TestAvailableForPrimaryChildren3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = (a.X()) ? (1 + 2) : $$(a[0]);
    }
}",
@"class C
{
    void M()
    {
        var q = (a.X()) ? (1 + 2) : a[0];
    }
}", offeredWhenRequireForClarityIsEnabled: true);
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
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestIsPattern_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if ( $$(a * b) is int i) { }
    }
}",
@"class C
{
    void M()
    {
        if ( a * b is int i) { }
    }
}", offeredWhenRequireForClarityIsEnabled: false);
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
}", parameters: new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));
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
        public async Task TestDoNotRemoveShiftAcrossPrecedence()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$(1 + 2) << 3;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestRemoveShiftIfNotNecessary2()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M()
    {
        int x = $$(1 << 2) << 3;
    }
}",
@"class C
{
    void M()
    {
        int x = 1 << 2 << 3;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestDoNotRemoveShiftAcrossSamePrecedenceIfValueWouldChange()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = 1 << $$(2 << 3);
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestDoNotRemoveShiftIfShiftKindDiffers()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = $$(1 >> 2) << 3;
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
        public async Task TestBitwiseExpression_TestMissingWithDifferencePrecedence1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var q = $$(a + b) & c;
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestBitwiseExpression_TestMissingWithDifferencePrecedence2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        var q = $$(a | b) & c;
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestBitwiseExpression_TestAvailableWithSamePrecedenceMissingWithDifferencePrecedence2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var q = $$(a & b) & c;
    }
}",
@"class C
{
    void M()
    {
        var q = a & b & c;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [WorkItem(25554, "https://github.com/dotnet/roslyn/issues/25554")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestSwitchCase_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        switch (true)
        {
            case $$(default(bool)):
        }
    }
}",
@"class C
{
    void M()
    {
        switch (true)
        {
            case default(bool):
        }
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [WorkItem(25554, "https://github.com/dotnet/roslyn/issues/25554")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestSwitchCase_WithWhenClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        switch (true)
        {
            case $$(default(bool)) when true:
        }
    }
}",
@"class C
{
    void M()
    {
        switch (true)
        {
            case default(bool) when true:
        }
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [WorkItem(25554, "https://github.com/dotnet/roslyn/issues/25554")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestWhenClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        switch (true)
        {
            case true when $$(default(bool)):
        }
    }
}",
@"class C
{
    void M()
    {
        switch (true)
        {
            case true when default(bool):
        }
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [WorkItem(25554, "https://github.com/dotnet/roslyn/issues/25554")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConstantPatternExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        if (true is $$(default(bool)))
        {
        }
    }
}",
@"class C
{
    void M()
    {
        if (true is default(bool))
        {
        }
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [WorkItem(25554, "https://github.com/dotnet/roslyn/issues/25554")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConstantPatternExpression_RequiredForPrecedence()
        {
            await TestMissingAsync(
@"class C
{
    void M(string s)
    {
        if (true is $$(true == true))
        {
        }
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastAmbiguity1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (X)$$(-1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastAmbiguity2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (X)$$(+1);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastAmbiguity3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (X)$$(&1);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastAmbiguity4()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        int x = (X)$$(*1);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPrimitiveCastNoAmbiguity1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$(-1);
    }
}",
@"class C
{
    void M()
    {
        int x = (int)-1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPrimitiveCastNoAmbiguity2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$(+1);
    }
}",
@"class C
{
    void M()
    {
        int x = (int)+1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPrimitiveCastNoAmbiguity3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$(&x);
    }
}",
@"class C
{
    void M()
    {
        int x = (int)&x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPrimitiveCastNoAmbiguity4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (int)$$(*x);
    }
}",
@"class C
{
    void M()
    {
        int x = (int)*x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArrayCastNoAmbiguity1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T[])$$(-1);
    }
}",
@"class C
{
    void M()
    {
        int x = (T[])-1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArrayCastNoAmbiguity2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T[])$$(+1);
    }
}",
@"class C
{
    void M()
    {
        int x = (T[])+1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArrayCastNoAmbiguity3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T[])$$(&x);
    }
}",
@"class C
{
    void M()
    {
        int x = (T[])&x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestArrayCastNoAmbiguity4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T[])$$(*x);
    }
}",
@"class C
{
    void M()
    {
        int x = (T[])*x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPointerCastNoAmbiguity1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T*)$$(-1);
    }
}",
@"class C
{
    void M()
    {
        int x = (T*)-1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPointerCastNoAmbiguity2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T*)$$(+1);
    }
}",
@"class C
{
    void M()
    {
        int x = (T*)+1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPointerCastNoAmbiguity3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T*)$$(&x);
    }
}",
@"class C
{
    void M()
    {
        int x = (T*)&x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestPointerCastNoAmbiguity4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T*)$$(*x);
    }
}",
@"class C
{
    void M()
    {
        int x = (T*)*x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNullableCastNoAmbiguity1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T?)$$(-1);
    }
}",
@"class C
{
    void M()
    {
        int x = (T?)-1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNullableCastNoAmbiguity2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T?)$$(+1);
    }
}",
@"class C
{
    void M()
    {
        int x = (T?)+1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNullableCastNoAmbiguity3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T?)$$(&x);
    }
}",
@"class C
{
    void M()
    {
        int x = (T?)&x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNullableCastNoAmbiguity4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (T?)$$(*x);
    }
}",
@"class C
{
    void M()
    {
        int x = (T?)*x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAliasCastNoAmbiguity1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (e::N.T)$$(-1);
    }
}",
@"class C
{
    void M()
    {
        int x = (e::N.T)-1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAliasCastNoAmbiguity2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (e::N.T)$$(+1);
    }
}",
@"class C
{
    void M()
    {
        int x = (e::N.T)+1;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAliasCastNoAmbiguity3()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (e::N.T)$$(&x);
    }
}",
@"class C
{
    void M()
    {
        int x = (e::N.T)&x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestAliasCastNoAmbiguity4()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (e::N.T)$$(*x);
    }
}",
@"class C
{
    void M()
    {
        int x = (e::N.T)*x;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastOfPrimary()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (X)$$(a);
    }
}",
@"class C
{
    void M()
    {
        int x = (X)a;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastOfMemberAccess()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (X)$$(a.b);
    }
}",
@"class C
{
    void M()
    {
        int x = (X)a.b;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastOfNonAmbiguousUnary()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (X)$$(!a);
    }
}",
@"class C
{
    void M()
    {
        int x = (X)!a;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestCastOfCast()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = (X)$$((Y)a);
    }
}",
@"class C
{
    void M()
    {
        int x = (X)(Y)a;
    }
}", offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestIsPatternAndLogical_TestWithAllOptionsSetToIgnore()
        {
            await TestAsync(
@"class C
{
    void M(object expression)
    {
        if ($$(expression is bool b) && b) { }
    }
}",
@"class C
{
    void M(object expression)
    {
        if (expression is bool b && b) { }
    }
}",
offeredWhenRequireForClarityIsEnabled: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestGuardPatternMissing()
        {
            await TestMissingAsync(
@"class C
{
    void M(object expression)
    {
        if (!$$(expression is bool b)) { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundLValueMemberAccess()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        $$(this.Property) = Property;
    }
}",
@"class C
{
    void M()
    {
        this.Property = Property;
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundMultiplicationInAddEquals()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        x += $$(y * z)
    }
}",
@"class C
{
    void M()
    {
        x += y * z
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundAddInMultipleEquals()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        x *= $$(y + z)
    }
}",
@"class C
{
    void M()
    {
        x *= y + z
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNecessaryCast()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        $$((short)3).ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundChecked()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 3 * $$(checked(5));
    }
}",
@"class C
{
    void M()
    {
        int x = 3 * checked(5);
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundUnchecked()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        int x = 3 * $$(unchecked(5));
    }
}",
@"class C
{
    void M()
    {
        int x = 3 * unchecked(5);
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundNameof()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        string property = ""My "" + $$(nameof(property));
    }
}",
@"class C
{
    void M()
    {
        string property = ""My "" + nameof(property);
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensIsCheck()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        bool x = $$("""" is string);
    }
}",
@"class C
{
    void M()
    {
        bool x = """" is string;
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestNecessaryParensAroundIs()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        string x = $$("""" is string).ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundAssignmentInInitialization()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        string y;
        string x = $$(y = ""text"");
    }
}",
@"class C
{
    void M()
    {
        string y;
        string x = y = ""text"";
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundLambda1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        Func<string, string> y2 = $$(v => v);
    }
}",
@"class C
{
    void M()
    {
        Func<string, string> y2 = v => v;
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundLambda2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        Func<string, string> y2 = $$((v) => v);
    }
}",
@"class C
{
    void M()
    {
        Func<string, string> y2 = (v) => v;
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundCastedLambda1()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        string y = ((Func<string, string>)$$((v) => v))(""text"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundCastedLambda2()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        string y = ($$(Func<string, string>)((v) => v))(""text"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundCastedLambda3()
        {
            await TestMissingAsync(
@"class C
{
    void M()
    {
        string y = $$((Func<string, string>)((v) => v))(""text"");
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundReturnValue1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        return$$(value);
    }
}",
@"class C
{
    void M()
    {
        return value;
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundReturnValue2()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        return $$(value);
    }
}",
@"class C
{
    void M()
    {
        return value;
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundPPDirective1()
        {
            await TestAsync(
@"class C
{
    void M()
    {
#if$$(A || B)
#endif
    }
}",
@"class C
{
    void M()
    {
#ifA || B
#endif
    }
}",
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestParensAroundPPDirective2()
        {
            // Currently producing broken code.
            await TestAsync(
@"class C
{
    void M()
    {
#if( $$(A || B) || C)
#endif
    }
}",
@"class C
{
    void M()
    {
#if( A || B || C)
#endif
    }
}",
offeredWhenRequireForClarityIsEnabled: true, index: 1);
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForPreIncrement()
        {
            await TestMissingAsync(
@"class C
{
    void M(int x)
    {
        var v = (byte)$$(++x);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForPreDecrement()
        {
            await TestMissingAsync(
@"class C
{
    void M(int x)
    {
        var v = (byte)$$(--x);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestForPostIncrement()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(int x)
    {
        var v = (byte)$$(x++);
    }
}",

@"class C
{
    void M(int x)
    {
        var v = (byte)x++;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestForPostDecrement()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(int x)
    {
        var v = (byte)$$(x--);
    }
}",

@"class C
{
    void M(int x)
    {
        var v = (byte)x--;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestForPreIncrementInLocalDeclaration()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(int x)
    {
        var v = $$(++x);
    }
}",
@"class C
{
    void M(int x)
    {
        var v = ++x;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestForPreIncrementInSimpleAssignment()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(int x, int v)
    {
        v = $$(++x);
    }
}",
@"class C
{
    void M(int x, int v)
    {
        v = ++x;
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestForPreIncrementInArgument()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(int x)
    {
        M($$(++x));
    }
}",
@"class C
{
    void M(int x)
    {
        M(++x);
    }
}", parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForPreIncrementAfterAdd()
        {
            await TestMissingAsync(
@"class C
{
    void M(int x)
    {
        var v = x+$$(++x);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(29454, "https://github.com/dotnet/roslyn/issues/29454")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForUnaryPlusAfterAdd()
        {
            await TestMissingAsync(
@"class C
{
    void M(int x)
    {
        var v = x+$$(+x);
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(31103, "https://github.com/dotnet/roslyn/issues/31103")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForConditionalRefAsLeftHandSideValue()
        {
            await TestMissingAsync(
@"class Bar
{
    void Foo(bool cond, double a, double b)
    {
        [||](cond ? ref a : ref b) = 6.67e-11;
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(31103, "https://github.com/dotnet/roslyn/issues/31103")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestConditionalExpressionAsRightHandSideValue()
        {
            await TestInRegularAndScript1Async(
@"class Bar
{
    void Foo(bool cond, double a, double b)
    {
        double c = $$(cond ? a : b);
    }
}",
@"class Bar
{
    void Foo(bool cond, double a, double b)
    {
        double c = cond ? a : b;
    }
}",
parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(32085, "https://github.com/dotnet/roslyn/issues/32085")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestMissingForNestedConditionalExpressionInLambda()
        {
            await TestMissingAsync(
@"class Bar
{
    void Test(bool a)
    {
        Func<int, string> lambda =
            number => number + $""{ ($$a ? ""foo"" : ""bar"") }"";
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestUnnecessaryParenthesisDiagnosticSingleLineExpression()
        {
            var openParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(", 4, 16);
            var parentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + 2)", 4, 16);
            var closeParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic(")", 4, 22);
            await TestDiagnosticsAsync(
@"class C
{
    void M()
    {
        int x = [|(1 + 2)|];
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses), parentheticalExpressionDiagnostic, openParenthesesDiagnostic, closeParenthesesDiagnostic);
        }

        [WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestUnnecessaryParenthesisDiagnosticInMultiLineExpression()
        {
            var openParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(", 4, 16);
            var firstLineParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 +", 4, 16);
            var closeParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic(")", 5, 13);
            await TestDiagnosticsAsync(
@"class C
{
    void M()
    {
        int x = [|(1 +
            2)|];
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses), firstLineParentheticalExpressionDiagnostic, openParenthesesDiagnostic, closeParenthesesDiagnostic);
        }

        [WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestUnnecessaryParenthesisDiagnosticInNestedExpression()
        {
            var outerOpenParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(", 4, 16);
            var outerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + (2 + 3) + 4)", 4, 16);
            var outerCloseParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic(")", 4, 32);
            var innerOpenParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(", 4, 21);
            var innerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(2 + 3)", 4, 21);
            var innerCloseParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic(")", 4, 27);
            var expectedDiagnostics = new DiagnosticDescription[] { outerParentheticalExpressionDiagnostic, outerOpenParenthesesDiagnostic,
                outerCloseParenthesesDiagnostic, innerParentheticalExpressionDiagnostic, innerOpenParenthesesDiagnostic, innerCloseParenthesesDiagnostic };
            await TestDiagnosticsAsync(
@"class C
{
    void M()
    {
        int x = [|(1 + (2 + 3) + 4)|];
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses), expectedDiagnostics);
        }

        [WorkItem(27925, "https://github.com/dotnet/roslyn/issues/27925")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestUnnecessaryParenthesisDiagnosticInNestedMultiLineExpression()
        {
            var outerOpenParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(", 4, 16);
            var outerFirstLineParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + 2 +", 4, 16);
            var outerCloseParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic(")", 6, 17);
            var innerOpenParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(", 5, 12);
            var innerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(3 + 4)", 5, 12);
            var innerCloseParenthesesDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic(")", 5, 18);
            var expectedDiagnostics = new DiagnosticDescription[] { outerFirstLineParentheticalExpressionDiagnostic, outerOpenParenthesesDiagnostic,
                outerCloseParenthesesDiagnostic, innerParentheticalExpressionDiagnostic, innerOpenParenthesesDiagnostic, innerCloseParenthesesDiagnostic };
            await TestDiagnosticsAsync(
@"class C
{
    void M()
    {
        int x = [|(1 + 2 +
            (3 + 4) +
            5 + 6)|];
    }
}", new TestParameters(options: RemoveAllUnnecessaryParentheses), expectedDiagnostics);
        }

        [WorkItem(27925, "https://github.com/dotnet/roslyn/issues/39363")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
        public async Task TestUnnecessaryParenthesesInSwitchExpression()
        {
            await TestAsync(
    @"class C
{
    void M(int x)
    {
            var result = x switch
            {
                1 => $$(5),
                2 => 10 + 5,
                _ => 100,
            }
    };
}",
    @"class C
{
    void M(int x)
    {
            var result = x switch
            {
                1 => 5,
                2 => 10 + 5,
                _ => 100,
            }
    };
}", offeredWhenRequireForClarityIsEnabled: true);
        }
    }
}
