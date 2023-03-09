// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryParentheses
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
    public partial class RemoveUnnecessaryExpressionParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public RemoveUnnecessaryExpressionParenthesesTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnnecessaryExpressionParenthesesDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryParenthesesCodeFixProvider());

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
            => descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.Unnecessary) && descriptor.DefaultSeverity == DiagnosticSeverity.Hidden;

        private static DiagnosticDescription GetRemoveUnnecessaryParenthesesDiagnostic(string text, int line, int column)
            => TestHelpers.Diagnostic(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId, text, startLocation: new LinePosition(line, column));

        [Fact]
        public async Task TestVariableInitializer_TestWithAllOptionsSetToIgnore()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1);
                    }
                }
                """, new TestParameters(options: IgnoreAllParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29736")]
        public async Task TestVariableInitializer_TestMissingParenthesis()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestArithmeticRequiredForClarity1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + $$(2 * 3);
                    }
                }
                """, new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44629")]
        public async Task TestStackAlloc()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var span = $$(stackalloc byte[8]);
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47365")]
        public async Task TestDynamic()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        dynamic i = 1;
                        dynamic s = "s";
                        Console.WriteLine(s + $$(1 + i));
                    }
                }
                """);
        }

        [Fact]
        public async Task TestArithmeticRequiredForClarity2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int x = a || $$(b && c);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = a || b && c;
                    }
                }
                """, parameters: new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));
        }

        [Fact]
        public async Task TestLogicalRequiredForClarity1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a || $$(b && c);
                    }
                }
                """, new TestParameters(options: RequireOtherBinaryParenthesesForClarity));
        }

        [Fact]
        public async Task TestLogicalRequiredForClarity2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int x = a + $$(b * c);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = a + b * c;
                    }
                }
                """, parameters: new TestParameters(options: RequireOtherBinaryParenthesesForClarity));
        }

        [Fact]
        public async Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Integral1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + $$(2 + 3);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + 2 + 3;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Integral2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1 + 2) + 3;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + 2 + 3;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestArithmeticRequiredForCorrectnessWhenPrecedenceStaysTheSameIfFloatingPoint()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1.0 + $$(2.0 + 3.0);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Floating2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1.0 + 2.0) + 3.0;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = 1.0 + 2.0 + 3.0;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a || $$(b || c);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = a || b || c;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(a || b) || c;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = a || b || c;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestVariableInitializer_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = 1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestReturnStatement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        return $$(1 + 2);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        return 1 + 2;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestExpressionBody_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    int M() => $$(1 + 2);
                }
                """,
                """
                class C
                {
                    int M() => 1 + 2;
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestCheckedExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int i = checked($$(1 + 2));
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = checked(1 + 2);
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        i = $$(1 + 2);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        i = 1 + 2;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestCompoundAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        i *= $$(1 + 2);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        i *= 1 + 2;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPimaryAssignment_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        i = $$(s.Length);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        i = s.Length;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNestedParenthesizedExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int i = ( $$(1 + 2) );
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = ( 1 + 2 );
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true, index: 1);
        }

        [Fact]
        public async Task TestIncrementExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int i = $$(x++);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = x++;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestLambdaBody_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        Func<int> i = () => $$(1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        Func<int> i = () => 1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestArrayElement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int[] i = new int[] { $$(1) };
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int[] i = new int[] { 1 };
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestWhereClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = from c in customer
                                where $$(c.Age > 21)
                                select c;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var q = from c in customer
                                where c.Age > 21
                                select c;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int i = (int)$$(1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = (int)1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestMissingForConditionalAccess1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        var v = $$(s?.Length).ToString();
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37046")]
        public async Task TestMissingForConditionalAccess2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        var v = $$(s?.Length)?.ToString();
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestForConditionalAccessNotInExpression()
        {
            await TestInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        var v = $$(s?.Length);
                    }
                }
                """,

                """
                class C
                {
                    void M(string s)
                    {
                        var v = s?.Length;
                    }
                }
                """, options: RemoveAllUnnecessaryParentheses);
        }

        [Fact]
        public async Task TestMissingForConditionalIndex()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        var v = $$(s?[0]).ToString();
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestBinaryInCastExpression()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int i = (int)$$(1 + 2);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestAroundCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int i = $$((int)1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int i = (int)1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestConditionalInInterpolation()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var s = $"{ $$(a ? b : c) }";
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestConditionalInInterpolation_FixAll_1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var s1 = $"{ {|FixAllInDocument:(|}(a ? b : c)) }";
                        var s2 = $"{ ((a ? b : c)) }";
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s1 = $"{ (a ? b : c) }";
                        var s2 = $"{ (a ? b : c) }";
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestConditionalInInterpolation_FixAll_2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var s1 = $"{ ({|FixAllInDocument:(|}a ? b : c)) }";
                        var s2 = $"{ ((a ? b : c)) }";
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s1 = $"{ (a ? b : c) }";
                        var s2 = $"{ (a ? b : c) }";
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNonConditionalInInterpolation_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var s = $"{ $$(true) }";
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var s = $"{ true }";
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = $$(a * b) + c;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var q = a * b + c;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: false);
        }

        [Fact]
        public async Task TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = c + $$(a * b);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var q = c + a * b;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: false);
        }

        [Fact]
        public async Task TestConditionalExpression_TestNotAvailableForComplexChildren1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = $$(a * b) ? (1 + 2) : (3 + 4);
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestConditionalExpression_TestNotAvailableForComplexChildren2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = (a * b) ? $$(1 + 2) : (3 + 4);
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestConditionalExpression_TestNotAvailableForComplexChildren3()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = (a * b) ? (1 + 2) : $$(3 + 4);
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestConditionalExpression_TestAvailableForPrimaryChildren1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = $$(a.X()) ? (1 + 2) : (3 + 4);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var q = a.X() ? (1 + 2) : (3 + 4);
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestConditionalExpression_TestAvailableForPrimaryChildren2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = (a.X()) ? $$(x.Length) : (3 + 4);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var q = (a.X()) ? x.Length : (3 + 4);
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestConditionalExpression_TestAvailableForPrimaryChildren3()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = (a.X()) ? (1 + 2) : $$(a[0]);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var q = (a.X()) ? (1 + 2) : a[0];
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestIsPattern_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        if ( $$(a[0]) is string s) { }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if ( a[0] is string s) { }
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestIsPattern_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        if ( $$(a * b) is int i) { }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if ( a * b is int i) { }
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: false);
        }

        [Fact]
        public async Task TestForOverloadedOperatorOnLeft()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(C c1, C c2, C c3)
                    {
                        var x = $$(c1 + c2) + c3;
                    }

                    public static C operator +(C c1, C c2) => null;
                }
                """,
                """
                class C
                {
                    void M(C c1, C c2, C c3)
                    {
                        var x = c1 + c2 + c3;
                    }

                    public static C operator +(C c1, C c2) => null;
                }
                """, parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact]
        public async Task TestMissingForOverloadedOperatorOnRight()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(C c1, C c2, C c3)
                    {
                        var x = c1 + $$(c2 + c3);
                    }

                    public static C operator +(C c1, C c2) => null;
                }
                """, parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact]
        public async Task TestShiftRequiredForClarity1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x =  $$(1 + 2) << 3;
                    }
                }
                """, parameters: new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));
        }

        [Fact]
        public async Task TestShiftRequiredForClarity2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1 + 2) << 3;
                    }
                }
                """, parameters: new TestParameters(options: RequireAllParenthesesForClarity));
        }

        [Fact]
        public async Task TestDoNotRemoveShiftAcrossPrecedence()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1 + 2) << 3;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestRemoveShiftIfNotNecessary2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1 << 2) << 3;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = 1 << 2 << 3;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestDoNotRemoveShiftAcrossSamePrecedenceIfValueWouldChange()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 << $$(2 << 3);
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestDoNotRemoveShiftIfShiftKindDiffers()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(1 >> 2) << 3;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestRemoveCoalesceIfNotNecessary1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = $$(a ?? b) ?? c;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestRemoveCoalesceIfNotNecessary2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M()
                    {
                        int x = a ?? $$(b ?? c);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = a ?? b ?? c;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestBitwiseExpression_TestMissingWithDifferencePrecedence1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = $$(a + b) & c;
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestBitwiseExpression_TestMissingWithDifferencePrecedence2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = $$(a | b) & c;
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestBitwiseExpression_TestAvailableWithSamePrecedenceMissingWithDifferencePrecedence2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        var q = $$(a & b) & c;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        var q = a & b & c;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
        public async Task TestSwitchCase_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case $$(default(bool)):
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case default(bool):
                        }
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
        public async Task TestSwitchCase_WithWhenClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case $$(default(bool)) when true:
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case default(bool) when true:
                        }
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
        public async Task TestWhenClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case true when $$(default(bool)):
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        switch (true)
                        {
                            case true when default(bool):
                        }
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
        public async Task TestConstantPatternExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        if (true is $$(default(bool)))
                        {
                        }
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        if (true is default(bool))
                        {
                        }
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
        public async Task TestConstantPatternExpression_RequiredForPrecedence()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        if (true is $$(true == true))
                        {
                        }
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestCastAmbiguity1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$(-1);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestCastAmbiguity2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$(+1);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestCastAmbiguity3()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$(&1);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestCastAmbiguity4()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$(*1);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestPrimitiveCastNoAmbiguity1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$(-1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (int)-1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPrimitiveCastNoAmbiguity2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$(+1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (int)+1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPrimitiveCastNoAmbiguity3()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$(&x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (int)&x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPrimitiveCastNoAmbiguity4()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$(*x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (int)*x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestArrayCastNoAmbiguity1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])$$(-1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])-1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestArrayCastNoAmbiguity2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])$$(+1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])+1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestArrayCastNoAmbiguity3()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])$$(&x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])&x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestArrayCastNoAmbiguity4()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])$$(*x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T[])*x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPointerCastNoAmbiguity1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)$$(-1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)-1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPointerCastNoAmbiguity2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)$$(+1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)+1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPointerCastNoAmbiguity3()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)$$(&x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)&x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestPointerCastNoAmbiguity4()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)$$(*x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T*)*x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNullableCastNoAmbiguity1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)$$(-1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)-1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNullableCastNoAmbiguity2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)$$(+1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)+1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNullableCastNoAmbiguity3()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)$$(&x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)&x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNullableCastNoAmbiguity4()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)$$(*x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (T?)*x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestAliasCastNoAmbiguity1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)$$(-1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)-1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestAliasCastNoAmbiguity2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)$$(+1);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)+1;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestAliasCastNoAmbiguity3()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)$$(&x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)&x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestAliasCastNoAmbiguity4()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)$$(*x);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (e::N.T)*x;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestCastOfPrimary()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$(a);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (X)a;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestCastOfMemberAccess()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$(a.b);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (X)a.b;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestCastOfNonAmbiguousUnary()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$(!a);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (X)!a;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestCastOfCast()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (X)$$((Y)a);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = (X)(Y)a;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestIsPatternAndLogical_TestWithAllOptionsSetToIgnore()
        {
            await TestAsync(
                """
                class C
                {
                    void M(object expression)
                    {
                        if ($$(expression is bool b) && b) { }
                    }
                }
                """,
                """
                class C
                {
                    void M(object expression)
                    {
                        if (expression is bool b && b) { }
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: false);
        }

        [Fact]
        public async Task TestGuardPatternMissing()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(object expression)
                    {
                        if (!$$(expression is bool b)) { }
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParensAroundLValueMemberAccess()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        $$(this.Property) = Property;
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        this.Property = Property;
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundMultiplicationInAddEquals()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        x += $$(y * z)
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        x += y * z
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundAddInMultipleEquals()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        x *= $$(y + z)
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        x *= y + z
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNecessaryCast()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        $$((short)3).ToString();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParensAroundChecked()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 3 * $$(checked(5));
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = 3 * checked(5);
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundUnchecked()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 3 * $$(unchecked(5));
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        int x = 3 * unchecked(5);
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundNameof()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        string property = "My " + $$(nameof(property));
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        string property = "My " + nameof(property);
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensIsCheck()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        bool x = $$("" is string);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        bool x = "" is string;
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestNecessaryParensAroundIs()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        string x = $$("" is string).ToString();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParensAroundAssignmentInInitialization()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        string y;
                        string x = $$(y = "text");
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        string y;
                        string x = y = "text";
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundLambda1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        Func<string, string> y2 = $$(v => v);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        Func<string, string> y2 = v => v;
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundLambda2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        Func<string, string> y2 = $$((v) => v);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        Func<string, string> y2 = (v) => v;
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundCastedLambda1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        string y = ((Func<string, string>)$$((v) => v))("text");
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParensAroundCastedLambda2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        string y = ($$(Func<string, string>)((v) => v))("text");
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParensAroundCastedLambda3()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        string y = $$((Func<string, string>)((v) => v))("text");
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParensAroundReturnValue1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        return$$(value);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        return value;
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundReturnValue2()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        return $$(value);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        return value;
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundPPDirective1()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                #if$$(A || B)
                #endif
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                #if A || B
                #endif
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestParensAroundPPDirective2()
        {
            // Currently producing broken code.
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                #if( $$(A || B) || C)
                #endif
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                #if( A || B || C)
                #endif
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true, index: 1);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57768")]
        public async Task TestParensAroundPPDirective3()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                #if C
                #elif$$(A || B)
                #endif
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                #if C
                #elif A || B
                #endif
                    }
                }
                """,
offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestMissingForPreIncrement()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int x)
                    {
                        var v = (byte)$$(++x);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestMissingForPreDecrement()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int x)
                    {
                        var v = (byte)$$(--x);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestForPostIncrement()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int x)
                    {
                        var v = (byte)$$(x++);
                    }
                }
                """,

                """
                class C
                {
                    void M(int x)
                    {
                        var v = (byte)x++;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestForPostDecrement()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int x)
                    {
                        var v = (byte)$$(x--);
                    }
                }
                """,

                """
                class C
                {
                    void M(int x)
                    {
                        var v = (byte)x--;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestForPreIncrementInLocalDeclaration()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int x)
                    {
                        var v = $$(++x);
                    }
                }
                """,
                """
                class C
                {
                    void M(int x)
                    {
                        var v = ++x;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestForPreIncrementInSimpleAssignment()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int x, int v)
                    {
                        v = $$(++x);
                    }
                }
                """,
                """
                class C
                {
                    void M(int x, int v)
                    {
                        v = ++x;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestForPreIncrementInArgument()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int x)
                    {
                        M($$(++x));
                    }
                }
                """,
                """
                class C
                {
                    void M(int x)
                    {
                        M(++x);
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestMissingForPreIncrementAfterAdd()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int x)
                    {
                        var v = x+$$(++x);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
        public async Task TestMissingForUnaryPlusAfterAdd()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int x)
                    {
                        var v = x+$$(+x);
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31103")]
        public async Task TestMissingForConditionalRefAsLeftHandSideValue()
        {
            await TestMissingAsync(
                """
                class Bar
                {
                    void Foo(bool cond, double a, double b)
                    {
                        [||](cond ? ref a : ref b) = 6.67e-11;
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31103")]
        public async Task TestConditionalExpressionAsRightHandSideValue()
        {
            await TestInRegularAndScript1Async(
                """
                class Bar
                {
                    void Foo(bool cond, double a, double b)
                    {
                        double c = $$(cond ? a : b);
                    }
                }
                """,
                """
                class Bar
                {
                    void Foo(bool cond, double a, double b)
                    {
                        double c = cond ? a : b;
                    }
                }
                """,
parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32085")]
        public async Task TestMissingForNestedConditionalExpressionInLambda()
        {
            await TestMissingAsync(
                """
                class Bar
                {
                    void Test(bool a)
                    {
                        Func<int, string> lambda =
                            number => number + $"{ ($$a ? "foo" : "bar") }";
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27925")]
        public async Task TestUnnecessaryParenthesisDiagnosticSingleLineExpression()
        {
            var parentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + 2)", 4, 16);
            await TestDiagnosticsAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = [|(1 + 2)|];
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses), parentheticalExpressionDiagnostic);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27925")]
        public async Task TestUnnecessaryParenthesisDiagnosticInMultiLineExpression()
        {
            var firstLineParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 +", 4, 16);
            await TestDiagnosticsAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = [|(1 +
                            2)|];
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses), firstLineParentheticalExpressionDiagnostic);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27925")]
        public async Task TestUnnecessaryParenthesisDiagnosticInNestedExpression()
        {
            var outerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + (2 + 3) + 4)", 4, 16);
            var innerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(2 + 3)", 4, 21);
            var expectedDiagnostics = new DiagnosticDescription[] { outerParentheticalExpressionDiagnostic, innerParentheticalExpressionDiagnostic };
            await TestDiagnosticsAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = [|(1 + (2 + 3) + 4)|];
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses), expectedDiagnostics);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27925")]
        public async Task TestUnnecessaryParenthesisDiagnosticInNestedMultiLineExpression()
        {
            var outerFirstLineParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(1 + 2 +", 4, 16);
            var innerParentheticalExpressionDiagnostic = GetRemoveUnnecessaryParenthesesDiagnostic("(3 + 4)", 5, 12);
            var expectedDiagnostics = new DiagnosticDescription[] { outerFirstLineParentheticalExpressionDiagnostic, innerParentheticalExpressionDiagnostic };
            await TestDiagnosticsAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = [|(1 + 2 +
                            (3 + 4) +
                            5 + 6)|];
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses), expectedDiagnostics);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39529")]
        public async Task TestUnnecessaryParenthesisIncludesFadeLocations()
        {
            var input = """
                class C
                {
                    void M()
                    {
                        int x = [|{|expression:{|fade:(|}1 + 2{|fade:)|}|}|];
                    }
                }
                """;

            var parameters = new TestParameters(options: RemoveAllUnnecessaryParentheses);
            using var workspace = CreateWorkspaceFromOptions(input, parameters);
            var expectedSpans = workspace.Documents.First().AnnotatedSpans;

            var diagnostics = await GetDiagnosticsAsync(workspace, parameters).ConfigureAwait(false);
            var diagnostic = diagnostics.Single();

            Assert.Equal(3, diagnostic.AdditionalLocations.Count);
            Assert.Equal(expectedSpans["expression"].Single(), diagnostic.AdditionalLocations[0].SourceSpan);
            Assert.Equal(expectedSpans["fade"][0], diagnostic.AdditionalLocations[1].SourceSpan);
            Assert.Equal(expectedSpans["fade"][1], diagnostic.AdditionalLocations[2].SourceSpan);

            Assert.Equal("[1,2]", diagnostic.Properties[WellKnownDiagnosticTags.Unnecessary]);
        }

        [Fact, WorkItem(27925, "https://github.com/dotnet/roslyn/issues/39363")]
        public async Task TestUnnecessaryParenthesesInSwitchExpression()
        {
            await TestAsync(
                """
                class C
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
                }
                """,
                """
                class C
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
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26311")]
        public async Task TestUnnecessaryParenthesesAroundDefaultLiteral()
        {
            await TestAsync(
                """
                class C
                {
                    void M()
                    {
                        bool f = false;

                        string s2 = f ? "" : $$(default);
                    }
                }
                """,
                """
                class C
                {
                    void M()
                    {
                        bool f = false;

                        string s2 = f ? "" : default;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestRangeWithConstantExpression()
        {
            await TestAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        _ = s[$$(1)..];
                    }
                }
                """,
                """
                class C
                {
                    void M(string s)
                    {
                        _ = s[1..];
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestRangeWithMemberAccessExpression()
        {
            await TestAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        _ = s[$$(s.Length)..];
                    }
                }
                """,
                """
                class C
                {
                    void M(string s)
                    {
                        _ = s[s.Length..];
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestRangeWithElementAccessExpression()
        {
            await TestAsync(
                """
                class C
                {
                    void M(string s, int[] indices)
                    {
                        _ = s[$$(indices[0])..];
                    }
                }
                """,
                """
                class C
                {
                    void M(string s, int[] indices)
                    {
                        _ = s[indices[0]..];
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestRangeWithBinaryExpression()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        _ = s[$$(s.Length - 5)..];
                    }
                }
                """, new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact]
        public async Task TestAlwaysUnnecessaryForPrimaryPattern1()
        {
            await TestAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        bool x = o is 1 or $$(2);
                    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        bool x = o is 1 or 2;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestAlwaysUnnecessaryForPrimaryPattern2()
        {
            await TestAsync(
                """
                class C
                {
                    void M(object o)
                    {
                        bool x = o is $$(1) or 2;
                    }
                }
                """,
                """
                class C
                {
                    void M(object o)
                    {
                        bool x = o is 1 or 2;
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50025")]
        public async Task TestDoNotRemoveWithConstantAndTypeAmbiguity()
        {
            await TestMissingAsync(
                """
                public class C
                {    
                    public const int Goo = 1;  

                    public void M(Goo o)
                    {
                        if (o is $$(Goo)) M(1);
                    }
                }

                public class Goo { }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50025")]
        public async Task TestDoRemoveWithNoConstantAndTypeAmbiguity()
        {
            await TestAsync(
                """
                public class C
                {    
                    public const int Goo = 1;  

                    public void M(object o)
                    {
                        if (o is $$(Goo)) M(1);
                    }    
                }
                """,
                """
                public class C
                {    
                    public const int Goo = 1;  

                    public void M(object o)
                    {
                        if (o is Goo) M(1);
                    }    
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestElementAccessOfSuppressedExpression1()
        {
            await TestAsync(
                """
                public class C
                {
                    public void M(string[] Strings)
                    {
                        var v = $$(Strings!)[Strings.Count - 1];
                    }
                }
                """,
                """
                public class C
                {
                    public void M(string[] Strings)
                    {
                        var v = Strings![Strings.Count - 1];
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact]
        public async Task TestElementAccessOfSuppressedExpression2()
        {
            await TestAsync(
                """
                public class C
                {
                    string[] Strings;

                    public void M()
                    {
                        var v = $$(this.Strings!)[Strings.Count - 1];
                    }
                }
                """,
                """
                public class C
                {
                    string[] Strings;

                    public void M()
                    {
                        var v = this.Strings![Strings.Count - 1];
                    }
                }
                """, offeredWhenRequireForClarityIsEnabled: true);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45100")]
        public async Task TestArithmeticOverflow1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int a)
                    {
                        checked
                        {
                            return a + $$(int.MaxValue + -int.MaxValue);
                        }
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45100")]
        public async Task TestArithmeticOverflow1_CompilationOption()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int a)
                    {
                        return a + $$(int.MaxValue + -int.MaxValue);
                    }
                }
                """, parameters: new TestParameters(
    options: RemoveAllUnnecessaryParentheses,
    compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, checkOverflow: true)));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45100")]
        public async Task TestArithmeticOverflow2()
        {
            await TestInRegularAndScript1Async(
                """
                class C
                {
                    void M(int a)
                    {
                        return a + $$(int.MaxValue + -int.MaxValue);
                    }
                }
                """,
                """
                class C
                {
                    void M(int a)
                    {
                        return a + int.MaxValue + -int.MaxValue;
                    }
                }
                """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
        public async Task TestTupleArgumentsBecomeGenericSyntax1()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = ($$(N < T), (U > (5 + 0)));
                    }
                }
                """,
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = (N < T, (U > (5 + 0)));
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
        public async Task TestTupleArgumentsBecomeGenericSyntax2()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = ((N < T), (U > (5 + 0)$$));
                    }
                }
                """,
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = ((N < T), U > (5 + 0));
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
        public async Task TestTupleArgumentsBecomeGenericSyntax3()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = ({|FixAllInDocument:$$(N < T), (U > (5 + 0))|});
                    }
                }
                """,
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = (N < T, (U > (5 + 0)));
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
        public async Task TestTupleArgumentsBecomeGenericSyntax4()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = ({|FixAllInDocument:(N < T), (U > (5 + 0)$$)|});
                    }
                }
                """,
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = ((N < T), U > (5 + 0));
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
        public async Task TestMethodArgumentsBecomeGenericSyntax1()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = Goo($$(N < T), (U > (5 + 0)));
                    }
                }
                """,
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = Goo(N < T, (U > (5 + 0)));
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
        public async Task TestMethodArgumentsBecomeGenericSyntax2()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = Goo((N < T), (U > (5 + 0)$$));
                    }
                }
                """,
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = Goo((N < T), U > (5 + 0));
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
        public async Task TestMethodArgumentsBecomeGenericSyntax3()
        {
            await TestInRegularAndScriptAsync(
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = Goo({|FixAllInDocument:$$(N < T), (U > (5 + 0))|});
                    }
                }
                """,
                """
                using System;
                public class C {
                    public void M()
                    {
                        var T = 1;
                        var U = 8;
                        var N = 9;
                        var x = Goo(N < T, (U > (5 + 0)));
                    }
                }
                """);
        }
    }
}
