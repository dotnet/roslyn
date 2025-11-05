// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessaryParentheses;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryParentheses)]
public sealed class RemoveUnnecessaryExpressionParenthesesTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpRemoveUnnecessaryExpressionParenthesesDiagnosticAnalyzer(), new CSharpRemoveUnnecessaryParenthesesCodeFixProvider());

    private async Task TestAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initial,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected,
        bool offeredWhenRequireForClarityIsEnabled, int index = 0)
    {
        await TestInRegularAndScriptAsync(initial, expected, index: index, new(options: RemoveAllUnnecessaryParentheses));

        if (offeredWhenRequireForClarityIsEnabled)
        {
            await TestInRegularAndScriptAsync(initial, expected, index: index, new(options: RequireAllParenthesesForClarity));
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
    public Task TestVariableInitializer_TestWithAllOptionsSetToIgnore()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = $$(1);
                }
            }
            """, new TestParameters(options: IgnoreAllParentheses));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29736")]
    public Task TestVariableInitializer_TestMissingParenthesis()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = $$(1;
                }
            }
            """);

    [Fact]
    public Task TestArithmeticRequiredForClarity1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + $$(2 * 3);
                }
            }
            """, new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44629")]
    public Task TestStackAlloc()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var span = $$(stackalloc byte[8]);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47365")]
    public Task TestDynamic()
        => TestMissingAsync(
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

    [Fact]
    public Task TestArithmeticRequiredForClarity2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestLogicalRequiredForClarity1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a || $$(b && c);
                }
            }
            """, new TestParameters(options: RequireOtherBinaryParenthesesForClarity));

    [Fact]
    public Task TestLogicalRequiredForClarity2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Integral1()
        => TestAsync(
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

    [Fact]
    public Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Integral2()
        => TestAsync(
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

    [Fact]
    public Task TestArithmeticRequiredForCorrectnessWhenPrecedenceStaysTheSameIfFloatingPoint()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1.0 + $$(2.0 + 3.0);
                }
            }
            """);

    [Fact]
    public Task TestArithmeticNotRequiredForClarityWhenPrecedenceStaysTheSame_Floating2()
        => TestAsync(
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

    [Fact]
    public Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame1()
        => TestAsync(
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

    [Fact]
    public Task TestLogicalNotRequiredForClarityWhenPrecedenceStaysTheSame2()
        => TestAsync(
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

    [Fact]
    public Task TestVariableInitializer_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestReturnStatement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestExpressionBody_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestCheckedExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestCompoundAssignment_TestAvailableWithAlwaysRemove_And_TestNotAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestPimaryAssignment_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestNestedParenthesizedExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestIncrementExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestLambdaBody_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestArrayElement_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestWhereClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestMissingForConditionalAccess1()
        => TestMissingAsync(
            """
            class C
            {
                void M(string s)
                {
                    var v = $$(s?.Length).ToString();
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/37046")]
    public Task TestMissingForConditionalAccess2()
        => TestMissingAsync(
            """
            class C
            {
                void M(string s)
                {
                    var v = $$(s?.Length)?.ToString();
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestForConditionalAccessNotInExpression()
        => TestInRegularAndScriptAsync(
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
            """, new(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestMissingForConditionalIndex()
        => TestMissingAsync(
            """
            class C
            {
                void M(string s)
                {
                    var v = $$(s?[0]).ToString();
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestBinaryInCastExpression()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int i = (int)$$(1 + 2);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestAroundCastExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestConditionalInInterpolation()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var s = $"{ $$(a ? b : c) }";
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestConditionalInInterpolation_FixAll_1()
        => TestAsync(
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

    [Fact]
    public Task TestConditionalInInterpolation_FixAll_2()
        => TestAsync(
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

    [Fact]
    public Task TestNonConditionalInInterpolation_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact]
    public Task TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        => TestAsync(
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

    [Fact]
    public Task TestBinaryExpression_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_2()
        => TestAsync(
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

    [Fact]
    public Task TestConditionalExpression_TestNotAvailableForComplexChildren1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var q = $$(a * b) ? (1 + 2) : (3 + 4);
                }
            }
            """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestConditionalExpression_TestNotAvailableForComplexChildren2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var q = (a * b) ? $$(1 + 2) : (3 + 4);
                }
            }
            """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestConditionalExpression_TestNotAvailableForComplexChildren3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var q = (a * b) ? (1 + 2) : $$(3 + 4);
                }
            }
            """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestConditionalExpression_TestAvailableForPrimaryChildren1()
        => TestAsync(
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

    [Fact]
    public Task TestConditionalExpression_TestAvailableForPrimaryChildren2()
        => TestAsync(
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

    [Fact]
    public Task TestConditionalExpression_TestAvailableForPrimaryChildren3()
        => TestAsync(
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

    [Fact]
    public Task TestIsPattern_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_1()
        => TestAsync(
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

    [Fact]
    public Task TestIsPattern_TestAvailableWithAlwaysRemove_And_NotAvailableWhenRequiredForClarity_2()
        => TestAsync(
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

    [Fact]
    public Task TestForOverloadedOperatorOnLeft()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingForOverloadedOperatorOnRight()
        => TestMissingAsync(
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

    [Fact]
    public Task TestShiftRequiredForClarity1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x =  $$(1 + 2) << 3;
                }
            }
            """, parameters: new TestParameters(options: RequireArithmeticBinaryParenthesesForClarity));

    [Fact]
    public Task TestShiftRequiredForClarity2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = $$(1 + 2) << 3;
                }
            }
            """, parameters: new TestParameters(options: RequireAllParenthesesForClarity));

    [Fact]
    public Task TestDoNotRemoveShiftAcrossPrecedence()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = $$(1 + 2) << 3;
                }
            }
            """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestRemoveShiftIfNotNecessary2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestDoNotRemoveShiftAcrossSamePrecedenceIfValueWouldChange()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 << $$(2 << 3);
                }
            }
            """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestDoNotRemoveShiftIfShiftKindDiffers()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = $$(1 >> 2) << 3;
                }
            }
            """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestRemoveCoalesceIfNotNecessary1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = $$(a ?? b) ?? c;
                }
            }
            """, parameters: new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestRemoveCoalesceIfNotNecessary2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestBitwiseExpression_TestMissingWithDifferencePrecedence1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var q = $$(a + b) & c;
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestBitwiseExpression_TestMissingWithDifferencePrecedence2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    var q = $$(a | b) & c;
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithSamePrecedenceMissingWithDifferencePrecedence2()
        => TestAsync(
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

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithSimpleLiteralOperand()
        => TestAsync(
            """
            class C
            {
                public const int A = 1 | $$(2);
            }
            """,
            """
            class C
            {
                public const int A = 1 | 2;
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithSimpleLiteralOperandAnd()
        => TestAsync(
            """
            class C
            {
                public const int A = 1 & $$(2);
            }
            """,
            """
            class C
            {
                public const int A = 1 & 2;
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithSimpleIdentifierOperand()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var a = 1;
                    var q = 1 | $$(a);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var a = 1;
                    var q = 1 | a;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithMemberAccessOperand()
        => TestAsync(
            """
            class C
            {
                int Field = 1;
                void M()
                {
                    var q = 1 | $$(Field);
                }
            }
            """,
            """
            class C
            {
                int Field = 1;
                void M()
                {
                    var q = 1 | Field;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithInvocationOperand()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var q = 1 | $$(GetValue());
                }
                int GetValue() => 2;
            }
            """,
            """
            class C
            {
                void M()
                {
                    var q = 1 | GetValue();
                }
                int GetValue() => 2;
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithElementAccessOperand()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var array = new[] { 1, 2, 3 };
                    var q = 1 | $$(array[0]);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var array = new[] { 1, 2, 3 };
                    var q = 1 | array[0];
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestBitwiseExpression_TestAvailableWithThisExpressionOperand()
        => TestAsync(
            """
            class C
            {
                public static implicit operator int(C c) => 1;
                void M()
                {
                    var q = 1 | $$(this);
                }
            }
            """,
            """
            class C
            {
                public static implicit operator int(C c) => 1;
                void M()
                {
                    var q = 1 | this;
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
    public Task TestSwitchCase_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
    public Task TestSwitchCase_WithWhenClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
    public Task TestWhenClause_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
    public Task TestConstantPatternExpression_TestAvailableWithAlwaysRemove_And_TestAvailableWhenRequiredForClarity()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25554")]
    public Task TestConstantPatternExpression_RequiredForPrecedence()
        => TestMissingAsync(
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

    [Fact]
    public Task TestWhenClauseWithNullableIndexing_RequiredForParsing()
        => TestMissingAsync(
            """
            class C
            {
                public void M(C[] x, bool a)
                {
                    switch ("")
                    {
                        case "" when $$(a || x?[0]):
                        {
                            break;
                        }
                    }
                }

                public static implicit operator bool(C? c) => true;
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestWhenClauseWithNullableIndexing_AndOperator()
        => TestMissingAsync(
            """
            class C
            {
                public void M(C[] x, bool a)
                {
                    switch ("")
                    {
                        case "" when $$(a && x?[0]):
                        {
                            break;
                        }
                    }
                }

                public static implicit operator bool(C? c) => true;
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestWhenClauseWithNullableIndexing_NestedBinary()
        => TestMissingAsync(
            """
            class C
            {
                public void M(C[] x, bool a, bool b)
                {
                    switch ("")
                    {
                        case "" when $$((a || b) && x?[0]):
                        {
                            break;
                        }
                    }
                }

                public static implicit operator bool(C? c) => true;
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestWhenClauseWithNullableIndexing_NoAmbiguityOnLeft()
        => TestAsync(
            """
            class C
            {
                public void M(C[] x, bool a)
                {
                    switch ("")
                    {
                        case "" when $$(x?[0]) || a:
                        {
                            break;
                        }
                    }
                }

                public static implicit operator bool(C? c) => true;
            }
            """,
            """
            class C
            {
                public void M(C[] x, bool a)
                {
                    switch ("")
                    {
                        case "" when x?[0] || a:
                        {
                            break;
                        }
                    }
                }

                public static implicit operator bool(C? c) => true;
            }
            """, offeredWhenRequireForClarityIsEnabled: false);

    [Fact]
    public Task TestWhenClauseWithoutNullableIndexing_CanRemove()
        => TestAsync(
            """
            class C
            {
                public void M(bool a, bool b)
                {
                    switch ("")
                    {
                        case "" when $$(a || b):
                        {
                            break;
                        }
                    }
                }
            }
            """,
            """
            class C
            {
                public void M(bool a, bool b)
                {
                    switch ("")
                    {
                        case "" when a || b:
                        {
                            break;
                        }
                    }
                }
            }
            """, offeredWhenRequireForClarityIsEnabled: true);

    [Fact]
    public Task TestCastAmbiguity1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (X)$$(-1);
                }
            }
            """);

    [Fact]
    public Task TestCastAmbiguity2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (X)$$(+1);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestCastAmbiguity3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (X)$$(&1);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestCastAmbiguity4()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (X)$$(*1);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestPrimitiveCastNoAmbiguity1()
        => TestAsync(
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

    [Fact]
    public Task TestPrimitiveCastNoAmbiguity2()
        => TestAsync(
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

    [Fact]
    public Task TestPrimitiveCastNoAmbiguity3()
        => TestAsync(
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

    [Fact]
    public Task TestPrimitiveCastNoAmbiguity4()
        => TestAsync(
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

    [Fact]
    public Task TestArrayCastNoAmbiguity1()
        => TestAsync(
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

    [Fact]
    public Task TestArrayCastNoAmbiguity2()
        => TestAsync(
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

    [Fact]
    public Task TestArrayCastNoAmbiguity3()
        => TestAsync(
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

    [Fact]
    public Task TestArrayCastNoAmbiguity4()
        => TestAsync(
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

    [Fact]
    public Task TestPointerCastNoAmbiguity1()
        => TestAsync(
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

    [Fact]
    public Task TestPointerCastNoAmbiguity2()
        => TestAsync(
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

    [Fact]
    public Task TestPointerCastNoAmbiguity3()
        => TestAsync(
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

    [Fact]
    public Task TestPointerCastNoAmbiguity4()
        => TestAsync(
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

    [Fact]
    public Task TestNullableCastNoAmbiguity1()
        => TestAsync(
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

    [Fact]
    public Task TestNullableCastNoAmbiguity2()
        => TestAsync(
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

    [Fact]
    public Task TestNullableCastNoAmbiguity3()
        => TestAsync(
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

    [Fact]
    public Task TestNullableCastNoAmbiguity4()
        => TestAsync(
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

    [Fact]
    public Task TestAliasCastNoAmbiguity1()
        => TestAsync(
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

    [Fact]
    public Task TestAliasCastNoAmbiguity2()
        => TestAsync(
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

    [Fact]
    public Task TestAliasCastNoAmbiguity3()
        => TestAsync(
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

    [Fact]
    public Task TestAliasCastNoAmbiguity4()
        => TestAsync(
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

    [Fact]
    public Task TestCastOfPrimary()
        => TestAsync(
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

    [Fact]
    public Task TestCastOfMemberAccess()
        => TestAsync(
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

    [Fact]
    public Task TestCastOfNonAmbiguousUnary()
        => TestAsync(
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

    [Fact]
    public Task TestCastOfCast()
        => TestAsync(
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

    [Fact]
    public Task TestIsPatternAndLogical_TestWithAllOptionsSetToIgnore()
        => TestAsync(
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

    [Fact]
    public Task TestGuardPatternMissing()
        => TestMissingAsync(
            """
            class C
            {
                void M(object expression)
                {
                    if (!$$(expression is bool b)) { }
                }
            }
            """);

    [Fact]
    public Task TestParensAroundLValueMemberAccess()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundMultiplicationInAddEquals()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundAddInMultipleEquals()
        => TestAsync(
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

    [Fact]
    public Task TestNecessaryCast()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    $$((short)3).ToString();
                }
            }
            """);

    [Fact]
    public Task TestParensAroundChecked()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundUnchecked()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundNameof()
        => TestAsync(
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

    [Fact]
    public Task TestParensIsCheck()
        => TestAsync(
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

    [Fact]
    public Task TestNecessaryParensAroundIs()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    string x = $$("" is string).ToString();
                }
            }
            """);

    [Fact]
    public Task TestParensAroundAssignmentInInitialization()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundLambda1()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundLambda2()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundCastedLambda1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    string y = ((Func<string, string>)$$((v) => v))("text");
                }
            }
            """);

    [Fact]
    public Task TestParensAroundCastedLambda2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    string y = ($$(Func<string, string>)((v) => v))("text");
                }
            }
            """);

    [Fact]
    public Task TestParensAroundCastedLambda3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    string y = $$((Func<string, string>)((v) => v))("text");
                }
            }
            """);

    [Fact]
    public Task TestParensAroundReturnValue1()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundReturnValue2()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundPPDirective1()
        => TestAsync(
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

    [Fact]
    public Task TestParensAroundPPDirective2()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57768")]
    public Task TestParensAroundPPDirective3()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestMissingForPreIncrement()
        => TestMissingAsync(
            """
            class C
            {
                void M(int x)
                {
                    var v = (byte)$$(++x);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestMissingForPreDecrement()
        => TestMissingAsync(
            """
            class C
            {
                void M(int x)
                {
                    var v = (byte)$$(--x);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestForPostIncrement()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestForPostDecrement()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestForPreIncrementInLocalDeclaration()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestForPreIncrementInSimpleAssignment()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestForPreIncrementInArgument()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestMissingForPreIncrementAfterAdd()
        => TestMissingAsync(
            """
            class C
            {
                void M(int x)
                {
                    var v = x+$$(++x);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29454")]
    public Task TestMissingForUnaryPlusAfterAdd()
        => TestMissingAsync(
            """
            class C
            {
                void M(int x)
                {
                    var v = x+$$(+x);
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31103")]
    public Task TestMissingForConditionalRefAsLeftHandSideValue()
        => TestMissingAsync(
            """
            class Bar
            {
                void Foo(bool cond, double a, double b)
                {
                    [||](cond ? ref a : ref b) = 6.67e-11;
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31103")]
    public Task TestConditionalExpressionAsRightHandSideValue()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32085")]
    public Task TestMissingForNestedConditionalExpressionInLambda()
        => TestMissingAsync(
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
    public Task TestUnnecessaryParenthesesInSwitchExpression()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26311")]
    public Task TestUnnecessaryParenthesesAroundDefaultLiteral()
        => TestAsync(
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

    [Fact]
    public Task TestRangeWithConstantExpression()
        => TestAsync(
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

    [Fact]
    public Task TestRangeWithMemberAccessExpression()
        => TestAsync(
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

    [Fact]
    public Task TestRangeWithElementAccessExpression()
        => TestAsync(
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

    [Fact]
    public Task TestRangeWithBinaryExpression()
        => TestMissingAsync(
            """
            class C
            {
                void M(string s)
                {
                    _ = s[$$(s.Length - 5)..];
                }
            }
            """, new TestParameters(options: RemoveAllUnnecessaryParentheses));

    [Fact]
    public Task TestAlwaysUnnecessaryForPrimaryPattern1()
        => TestAsync(
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

    [Fact]
    public Task TestAlwaysUnnecessaryForPrimaryPattern2()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50025")]
    public Task TestDoNotRemoveWithConstantAndTypeAmbiguity()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50025")]
    public Task TestDoRemoveWithNoConstantAndTypeAmbiguity()
        => TestAsync(
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

    [Fact]
    public Task TestElementAccessOfSuppressedExpression1()
        => TestAsync(
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

    [Fact]
    public Task TestElementAccessOfSuppressedExpression2()
        => TestAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45100")]
    public Task TestArithmeticOverflow1()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45100")]
    public Task TestArithmeticOverflow1_CompilationOption()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/45100")]
    public Task TestArithmeticOverflow2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
    public Task TestTupleArgumentsBecomeGenericSyntax1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
    public Task TestTupleArgumentsBecomeGenericSyntax2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
    public Task TestTupleArgumentsBecomeGenericSyntax3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
    public Task TestTupleArgumentsBecomeGenericSyntax4()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
    public Task TestMethodArgumentsBecomeGenericSyntax1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
    public Task TestMethodArgumentsBecomeGenericSyntax2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43934")]
    public Task TestMethodArgumentsBecomeGenericSyntax3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestRemoveAroundCollectionExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool b)
                {
                    int[] a = b ? $$([1]) : []; 
                }
            }
            """,
            """
            class C
            {
                void M(bool b)
                {
                    int[] a = b ? [1] : []; 
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75203")]
    public Task TestConditionalExpressionInWhenClauseAmbiguity1()
        => TestMissingAsync(
            """
            class C
            {
                public void M(object o, object?[] c)
                {
                    switch(o)
                    {
                        case { } when $$(c?[0] is { }):
                            break;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75203")]
    public Task TestConditionalExpressionInWhenClauseAmbiguity2()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public void M(object o, object?[] c)
                {
                    switch(o)
                    {
                        case { } when $$(c ? [0] : [1]):
                            break;
                    }
                }
            }
            """, """
            class C
            {
                public void M(object o, object?[] c)
                {
                    switch(o)
                    {
                        case { } when c ? [0] : [1]:
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestCollectionExpressionSpread()
        => TestInRegularAndScriptAsync("""
            class C
            {
                public void M()
                {
                    var v = [.. $$(a ? b : c)];
                }
            }
            """, """
            class C
            {
                public void M()
                {
                    var v = [.. a ? b : c];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78331")]
    public Task TestCollectionExpressionInInitializer()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                public void M()
                {
                    var v = new List<int[]>() { $$([]) };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78331")]
    public Task TestAttributedLambdaExpressionInInitializer()
        => TestMissingInRegularAndScriptAsync("""
            class C
            {
                public void M()
                {
                    var v = new List<Action>() { $$([X] () => { }) };
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/80082")]
    [InlineData("int?")]
    [InlineData("int*")]
    [InlineData("int[]")]
    [InlineData("int")]
    [InlineData("A::B")]
    [InlineData("List<A>")]
    [InlineData("List<int>")]
    [InlineData("A.List<A>")]
    [InlineData("A.List<int>")]
    [InlineData("global::A.List<A>")]
    [InlineData("global::A.List<int>")]
    [InlineData("(A, B)")]
    public Task TestCollectionExpressionCast_NotEmpty_ShouldRemove(string type)
        => TestInRegularAndScriptAsync($$"""
            class C
            {
                public void M()
                {
                    var v = ({{type}})$$([a, b, c]);
                }
            }
            """,
            $$"""
            class C
            {
                public void M()
                {
                    var v = ({{type}})[a, b, c];
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/80082")]
    [InlineData("int?")]
    [InlineData("int*")]
    [InlineData("int[]")]
    [InlineData("int")]
    [InlineData("A::B")]
    [InlineData("List<A>")]
    [InlineData("List<int>")]
    [InlineData("A.List<A>")]
    [InlineData("A.List<int>")]
    [InlineData("global::A.List<A>")]
    [InlineData("global::A.List<int>")]
    [InlineData("(A, B)")]
    [InlineData("A")]
    [InlineData("A.B")]
    [InlineData("global::A.B")]
    public Task TestCollectionExpressionCast_Empty_ShouldRemove(string type)
        => TestInRegularAndScriptAsync($$"""
            class C
            {
                public void M()
                {
                    var v = ({{type}})$$([]);
                }
            }
            """,
            $$"""
            class C
            {
                public void M()
                {
                    var v = ({{type}})[];
                }
            }
            """);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/80082")]
    [InlineData("A")]
    [InlineData("A.B")]
    [InlineData("global::A.B")]
    public Task TestCollectionExpressionCast_NotEmpty_ShouldNotRemove(string type)
        => TestMissingInRegularAndScriptAsync($$"""
            class C
            {
                public void M()
                {
                    var v = ({{type}})$$([a, b, c]);
                }
            }
            """);
}
