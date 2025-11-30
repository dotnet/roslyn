// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
public sealed partial class AddRequiredExpressionParenthesesTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpAddRequiredExpressionParenthesesDiagnosticAnalyzer(), new AddRequiredParenthesesCodeFixProvider());

    private Task TestMissingAsync(string initialMarkup, OptionsCollection options)
        => TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options));

    private Task TestAsync(string initialMarkup, string expected, OptionsCollection options)
        => TestInRegularAndScriptAsync(initialMarkup, expected, parameters: new TestParameters(options: options));

    [Fact]
    public Task TestArithmeticPrecedence()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + 2 $$* 3;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = 1 + (2 * 3);
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNoArithmeticOnLowerPrecedence()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$+ 2 * 3;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotIfArithmeticPrecedenceStaysTheSame()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + 2 $$+ 3;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotIfArithmeticPrecedenceIsNotEnforced1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + 2 $$+ 3;
                }
            }
            """, RequireOtherBinaryParenthesesForClarity);

    [Fact]
    public Task TestNotIfArithmeticPrecedenceIsNotEnforced2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + 2 $$* 3;
                }
            }
            """, RequireOtherBinaryParenthesesForClarity);

    [Fact]
    public Task TestRelationalPrecedence()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = a $$> b == c;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = (a > b) == c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestLogicalPrecedence()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = a || b $$&& c;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = a || (b && c);
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNoLogicalOnLowerPrecedence()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a $$|| b && c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotIfLogicalPrecedenceStaysTheSame()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a || b $$|| c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotIfLogicalPrecedenceIsNotEnforced()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a || b $$|| c;
                }
            }
            """, RequireArithmeticBinaryParenthesesForClarity);

    [Fact]
    public Task TestMixedArithmeticAndLogical()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a == b $$&& c == d;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestLogicalPrecedenceMultipleEqualPrecedenceParts1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = a || b $$&& c && d;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = a || (b && c && d);
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestLogicalPrecedenceMultipleEqualPrecedenceParts2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = a || b && c $$&& d;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = a || (b && c && d);
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestShiftPrecedence1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$+ 2 << 3;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = (1 + 2) << 3;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestShiftPrecedence2()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$+ 2 << 3;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = (1 + 2) << 3;
                }
            }
            """, RequireArithmeticBinaryParenthesesForClarity);

    [Fact]
    public Task TestShiftPrecedence3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$+ 2 << 3;
                }
            }
            """, RequireOtherBinaryParenthesesForClarity);

    [Fact]
    public Task TestNotIfShiftPrecedenceStaysTheSame1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$<< 2 << 3;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotIfShiftPrecedenceStaysTheSame2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 << 2 $$<< 3;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestEqualityPrecedence1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$+ 2 == 2 + 3;
                }
            }
            """, RequireOtherBinaryParenthesesForClarity);

    [Fact]
    public Task TestEqualityPrecedence2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + 2 == 2 $$+ 3;
                }
            }
            """, RequireOtherBinaryParenthesesForClarity);

    [Fact]
    public Task TestEqualityPrecedence3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$+ 2 == 2 + 3;
                }
            }
            """, RequireRelationalBinaryParenthesesForClarity);

    [Fact]
    public Task TestEqualityPrecedence4()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + 2 == 2 $$+ 3;
                }
            }
            """, RequireRelationalBinaryParenthesesForClarity);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78841")]
    public Task TestCoalescePrecedence1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = a $$+ b ?? c;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = (a + b) ?? c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestCoalescePrecedence2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a $$?? b ?? c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestCoalescePrecedence3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a ?? b $$?? c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79286")]
    public Task TestCoalescePrecedence4()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a $$as b ?? c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79655")]
    public Task TestCoalescePrecedence_IsNull()
        => TestAsync(
            """
            class C
            {
                bool M(object x, object y) => x?.Equals(y) ?? y $$is null;
            }
            """,
            """
            class C
            {
                bool M(object x, object y) => x?.Equals(y) ?? (y is null);
            }
            """, RequireAllParenthesesForClarity);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79655")]
    public Task TestCoalescePrecedence_IsNotNull()
        => TestAsync(
            """
            class C
            {
                bool M(object x, object y) => x?.Equals(y) ?? y $$is not null;
            }
            """,
            """
            class C
            {
                bool M(object x, object y) => x?.Equals(y) ?? (y is not null);
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestBitwisePrecedence1()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$+ 2 & 3;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = (1 + 2) & 3;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestBitwisePrecedence2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a $$| b | c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestBitwisePrecedence3()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    int x = a | b $$& c;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = a | (b & c);
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestBitwisePrecedence4()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = a $$| b & c;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotForEqualityAfterEquals()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 $$== 2;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotForAssignmentEqualsAfterLocal()
        => TestMissingAsync(
            """
            class C
            {
                void M(int a)
                {
                    int x = a $$+= 2;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestForAssignmentAndEquality1()
        => TestMissingAsync(
            """
            class C
            {
                void M(bool x, bool y, bool z)
                {
                    x $$= y == z;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestMissingForAssignmentAndEquality2()
        => TestMissingAsync(
            """
            class C
            {
                void M(bool x, bool y, bool z)
                {
                    x = y $$== z;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestUnclearCast1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$-y;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestUnclearCast_NotOfferedWithIgnore()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$-y;
                }
            }
            """, IgnoreAllParentheses);

    [Fact]
    public Task TestUnclearCast_NotOfferedWithRemoveForClarity()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$-y;
                }
            }
            """, RemoveAllUnnecessaryParentheses);

    [Fact]
    public Task TestUnclearCast2()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$+y;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestUnclearCast3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$&y;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestUnclearCast4()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$*y;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotForPrimary()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$y;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotForMemberAccess()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$y.z;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotForCastOfCast()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$(y);
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestNotForNonAmbiguousUnary()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    int x = (int)$$!y;
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestFixAll1()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    if (0 {|FixAllInDocument:>=|} 3 * 2 + 4)
                    {
                    }
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestFixAll2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    if (3 * 2 + 4 >= 3 {|FixAllInDocument:*|} 2 + 4)
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
                    if ((3 * 2) + 4 >= (3 * 2) + 4)
                    {
                    }
                }
            }
            """, new(options: RequireAllParenthesesForClarity));

    [Fact]
    public Task TestFixAll3()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    if (3 * 2 + 4 >= 3 * 2 {|FixAllInDocument:+|} 4)
                    {
                    }
                }
            }
            """, RequireAllParenthesesForClarity);

    [Fact]
    public Task TestSeams1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int x = 1 + 2 {|FixAllInDocument:*|} 3 == 1 + 2 * 3;
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    int x = 1 + (2 * 3) == 1 + (2 * 3);
                }
            }
            """, new(options: RequireAllParenthesesForClarity));
}
