// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddRequiredParentheses;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddRequiredParentheses;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)]
    public partial class AddRequiredExpressionParenthesesTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public AddRequiredExpressionParenthesesTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpAddRequiredExpressionParenthesesDiagnosticAnalyzer(), new AddRequiredParenthesesCodeFixProvider());

        private Task TestMissingAsync(string initialMarkup, OptionsCollection options)
            => TestMissingInRegularAndScriptAsync(initialMarkup, new TestParameters(options: options));

        private Task TestAsync(string initialMarkup, string expected, OptionsCollection options)
            => TestInRegularAndScript1Async(initialMarkup, expected, parameters: new TestParameters(options: options));

        [Fact]
        public async Task TestArithmeticPrecedence()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestNoArithmeticOnLowerPrecedence()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 $$+ 2 * 3;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotIfArithmeticPrecedenceStaysTheSame()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + 2 $$+ 3;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotIfArithmeticPrecedenceIsNotEnforced1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + 2 $$+ 3;
                    }
                }
                """, RequireOtherBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotIfArithmeticPrecedenceIsNotEnforced2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + 2 $$* 3;
                    }
                }
                """, RequireOtherBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestRelationalPrecedence()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLogicalPrecedence()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestNoLogicalOnLowerPrecedence()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a $$|| b && c;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotIfLogicalPrecedenceStaysTheSame()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a || b $$|| c;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotIfLogicalPrecedenceIsNotEnforced()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a || b $$|| c;
                    }
                }
                """, RequireArithmeticBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestMixedArithmeticAndLogical()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a == b $$&& c == d;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts1()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestLogicalPrecedenceMultipleEqualPrecedenceParts2()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestShiftPrecedence1()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestShiftPrecedence2()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestShiftPrecedence3()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 $$+ 2 << 3;
                    }
                }
                """, RequireOtherBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotIfShiftPrecedenceStaysTheSame1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 $$<< 2 << 3;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotIfShiftPrecedenceStaysTheSame2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 << 2 $$<< 3;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestEqualityPrecedence1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 $$+ 2 == 2 + 3;
                    }
                }
                """, RequireOtherBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestEqualityPrecedence2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + 2 == 2 $$+ 3;
                    }
                }
                """, RequireOtherBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestEqualityPrecedence3()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 $$+ 2 == 2 + 3;
                    }
                }
                """, RequireRelationalBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestEqualityPrecedence4()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 + 2 == 2 $$+ 3;
                    }
                }
                """, RequireRelationalBinaryParenthesesForClarity);
        }

        [Fact]
        public async Task TestCoalescePrecedence1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a $$+ b ?? c;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestCoalescePrecedence2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a $$?? b ?? c;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestCoalescePrecedence3()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a ?? b $$?? c;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestBitwisePrecedence1()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestBitwisePrecedence2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a $$| b | c;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestBitwisePrecedence3()
        {
            await TestAsync(
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
        }

        [Fact]
        public async Task TestBitwisePrecedence4()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = a $$| b & c;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotForEqualityAfterEquals()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = 1 $$== 2;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotForAssignmentEqualsAfterLocal()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(int a)
                    {
                        int x = a $$+= 2;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestForAssignmentAndEquality1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(bool x, bool y, bool z)
                    {
                        x $$= y == z;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestMissingForAssignmentAndEquality2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M(bool x, bool y, bool z)
                    {
                        x = y $$== z;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestUnclearCast1()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$-y;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestUnclearCast_NotOfferedWithIgnore()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$-y;
                    }
                }
                """, IgnoreAllParentheses);
        }

        [Fact]
        public async Task TestUnclearCast_NotOfferedWithRemoveForClarity()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$-y;
                    }
                }
                """, RemoveAllUnnecessaryParentheses);
        }

        [Fact]
        public async Task TestUnclearCast2()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$+y;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestUnclearCast3()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$&y;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestUnclearCast4()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$*y;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotForPrimary()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$y;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotForMemberAccess()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$y.z;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotForCastOfCast()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$(y);
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestNotForNonAmbiguousUnary()
        {
            await TestMissingAsync(
                """
                class C
                {
                    void M()
                    {
                        int x = (int)$$!y;
                    }
                }
                """, RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await TestMissingAsync(
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
        }

        [Fact]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: RequireAllParenthesesForClarity);
        }

        [Fact]
        public async Task TestFixAll3()
        {
            await TestMissingAsync(
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
        }

        [Fact]
        public async Task TestSeams1()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: RequireAllParenthesesForClarity);
        }
    }
}
